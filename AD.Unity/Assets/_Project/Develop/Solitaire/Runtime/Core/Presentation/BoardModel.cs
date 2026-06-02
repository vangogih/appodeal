using System.Collections.Generic;
using System.Threading;
using Appodeal.Solitaire.Runtime.Core.Assets;
using Appodeal.Solitaire.Runtime.Core.Domain;
using Appodeal.Solitaire.Runtime.Core.Game;
using Appodeal.Solitaire.Runtime.Core.Layout;
using Appodeal.Solitaire.Runtime.Utilities.Logging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Appodeal.Solitaire.Runtime.Core.Presentation
{
    /// <summary>
    /// Model that owns the board views: the <see cref="BoardView"/> container, the 13
    /// <see cref="PileView"/>s and one <see cref="CardModel"/> per card. Syncs the views to a
    /// <see cref="BoardState"/>, animates transitions and handles lifting/returning of dragged cards.
    /// Depends only on external systems (<see cref="IGameSystem"/>, <see cref="IGameAssetsSystem"/>,
    /// <see cref="ILayoutSystem"/>).
    /// </summary>
    public sealed class BoardModel
    {
        private const string LogTag = "PRESENTATION";

        private readonly IGameSystem _game;
        private readonly IGameAssetsSystem _assets;
        private readonly ILayoutSystem _layout;
        private readonly CardViewFactory _cardFactory;

        private readonly Dictionary<(Suit, Rank), CardModel> _cards = new();
        private readonly List<PileView> _piles = new();

        private BoardView _boardView;
        private BoardState _state;

        // Drag bookkeeping.
        private readonly List<CardModel> _lifted = new();
        private readonly List<Vector3> _liftGrabOffsets = new();

        public BoardModel(IGameSystem game, IGameAssetsSystem assets, ILayoutSystem layout, CardViewFactory cardFactory)
        {
            _game = game;
            _assets = assets;
            _layout = layout;
            _cardFactory = cardFactory;
        }

        public BoardView BoardView => _boardView;

        // REQ-PRES-001.1/.2: build the board skeleton (board container + 13 pile slots).
        public async UniTask BuildAsync(CancellationToken ct = default)
        {
            var prefab = _assets.BoardViewPrefab;
            if (prefab == null)
            {
                Log.Default.E(LogTag, "BoardView prefab is not loaded; cannot build the board.");
                return;
            }

            var instance = Object.Instantiate(prefab);
            _boardView = instance.GetComponent<BoardView>();
            if (_boardView == null)
            {
                Log.Default.E(LogTag, "BoardView prefab has no BoardView component.");
                return;
            }

            await _boardView.InitializeAsync(ct);

            CreatePile(PileId.Stock);
            CreatePile(PileId.Waste);
            for (int i = 0; i < RuntimeConstants.Game.Board.FoundationCount; i++)
                CreatePile(PileId.Foundation(i));
            for (int i = 0; i < RuntimeConstants.Game.Board.TableauColumns; i++)
                CreatePile(PileId.Tableau(i));
        }

        private void CreatePile(PileId id)
        {
            var prefab = _assets.PileViewPrefab;
            if (prefab == null)
            {
                Log.Default.E(LogTag, "PileView prefab is not loaded; cannot create a pile slot.");
                return;
            }

            var pile = PileView.Create(prefab.GetComponent<PileView>(), _boardView.PilesRoot);
            if (pile == null)
                return;

            pile.Setup(id, _layout.GetPilePosition(id));
            _piles.Add(pile);
        }

        // REQ-PRES-002 / REQ-PRES-003: bring the set of CardViews into agreement with the board state.
        public async UniTask SyncAsync(BoardState state, bool animate, CancellationToken ct = default)
        {
            _state = state;
            if (_boardView == null)
                return;

            await SyncPileAsync(PileId.Stock, state.Stock, animate, ct);
            await SyncPileAsync(PileId.Waste, state.Waste, animate, ct);

            for (int i = 0; i < state.Foundations.Count; i++)
                await SyncPileAsync(PileId.Foundation(i), state.Foundations[i], animate, ct);

            for (int i = 0; i < state.Tableau.Count; i++)
                await SyncPileAsync(PileId.Tableau(i), state.Tableau[i], animate, ct);

            UpdateButtons();
        }

        private async UniTask SyncPileAsync(PileId pile, IReadOnlyList<Card> cards, bool animate, CancellationToken ct)
        {
            var anchor = _layout.GetPilePosition(pile);
            var offset = Vector3.zero;

            for (int index = 0; index < cards.Count; index++)
            {
                var card = cards[index];
                var model = await GetOrCreateCardAsync(card, ct);
                if (model == null)
                    continue;

                int sorting = RuntimeConstants.Game.Presentation.CardSortingBase + index;
                model.Apply(card, pile, index, anchor + offset, sorting, animate);

                offset += GetFanStep(pile.Kind, card.FaceUp);
            }
        }

        // Caller-side cumulative fan summation using the layout steps (allowed by REQ-LAY-002).
        private Vector3 GetFanStep(PileKind kind, bool faceUp)
        {
            if (kind != PileKind.Tableau)
                return Vector3.zero;

            float step = faceUp
                ? RuntimeConstants.Game.Layout.Tableau.FaceUpFanY
                : RuntimeConstants.Game.Layout.Tableau.FaceDownFanY;

            return new Vector3(0f, -step, 0f);
        }

        private async UniTask<CardModel> GetOrCreateCardAsync(Card card, CancellationToken ct)
        {
            var key = (card.Suit, card.Rank);
            if (_cards.TryGetValue(key, out var existing))
                return existing;

            var model = new CardModel(_assets);
            await model.InitializeAsync(_cardFactory, _boardView.CardsRoot, ct);
            _cards[key] = model;
            return model;
        }

        // REQ-PRES-004.1: lift a face-up card (plus the tableau tail above it).
        public bool TryLift(CardView card, Vector3 pointerWorld, out PileId source, out int count)
        {
            source = default;
            count = 0;

            if (card == null || !card.FaceUp || _state == null)
                return false;

            source = card.Pile;
            int grabIndex = card.IndexInPile;
            int pileCount = _state.GetPile(source).Count;

            count = source.Kind == PileKind.Tableau ? pileCount - grabIndex : 1;
            if (count < 1)
                return false;

            _lifted.Clear();
            _liftGrabOffsets.Clear();

            foreach (var model in _cards.Values)
            {
                var view = model.View;
                if (view == null || view.Pile != source || view.IndexInPile < grabIndex)
                    continue;

                _lifted.Add(model);
                _liftGrabOffsets.Add(view.transform.position - pointerWorld);
            }

            for (int i = 0; i < _lifted.Count; i++)
                _lifted[i].Lift(pointerWorld + _liftGrabOffsets[i], RuntimeConstants.Game.Presentation.DragSortingBoost + i);

            return true;
        }

        // REQ-PRES-004.2: drag the lifted cards with the pointer.
        public void MoveLifted(Vector3 pointerWorld)
        {
            for (int i = 0; i < _lifted.Count; i++)
                _lifted[i].MoveLiftedTo(pointerWorld + _liftGrabOffsets[i]);
        }

        public void ClearLift()
        {
            _lifted.Clear();
            _liftGrabOffsets.Clear();
        }

        // REQ-PRES-004.4: return cards to their board positions (used when a move is rejected).
        public async UniTask ReturnLiftedAsync(CancellationToken ct = default)
        {
            ClearLift();
            if (_state != null)
                await SyncAsync(_state, animate: true, ct);
        }

        public void ShowWin()
        {
            if (_boardView != null)
                _boardView.ShowWin();
        }

        public void HideWin()
        {
            if (_boardView != null)
                _boardView.HideWin();
        }

        private void UpdateButtons()
        {
            if (_boardView == null)
                return;

            _boardView.SetUndoInteractable(_game.CanUndo);
            _boardView.SetRedoInteractable(_game.CanRedo);
        }

        public void Dispose()
        {
            foreach (var model in _cards.Values)
                model.Dispose();
            _cards.Clear();

            _piles.Clear();
            _lifted.Clear();
            _liftGrabOffsets.Clear();

            if (_boardView != null)
            {
                _boardView.Teardown();
                Object.Destroy(_boardView.gameObject);
                _boardView = null;
            }
        }
    }
}
