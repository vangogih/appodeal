using System;
using System.Threading;
using Appodeal.Solitaire.Runtime.Core.Assets;
using Appodeal.Solitaire.Runtime.Core.Domain;
using Appodeal.Solitaire.Runtime.Core.Game;
using Appodeal.Solitaire.Runtime.Core.Input;
using Appodeal.Solitaire.Runtime.Core.Layout;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Appodeal.Solitaire.Runtime.Core.Presentation
{
    public interface IPresentationSystem
    {
        UniTask InitializeAsync(CancellationToken ct = default);
        UniTask DisposeAsync();
    }

    /// <summary>
    /// Visuals-only head system. Builds the board, renders <see cref="BoardState"/> reactively from
    /// <see cref="IGameSystem"/> events, shows dragging from <see cref="IInputSystem"/> events and
    /// translates completed gestures into <see cref="IGameSystem"/> commands. Contains no game rules,
    /// position math, asset loading or history. Built with the Model/View pattern.
    /// </summary>
    public sealed class PresentationSystem : IPresentationSystem, IDisposable
    {
        private readonly IGameSystem _game;
        private readonly IInputSystem _input;

        private readonly BoardModel _board;
        private readonly HitTester _hitTester;

        // Stored delegates so subscriptions can be removed (see event-subscriptions.md).
        private readonly Action<BoardState> _onBoardChanged;
        private readonly Action _onGameWon;
        private readonly Action<Vector2> _onPointerDown;
        private readonly Action<Vector2> _onPointerMove;
        private readonly Action<Vector2> _onPointerUp;
        private readonly Action<Vector2> _onTap;
        private readonly Action _onUndoClicked;
        private readonly Action _onRedoClicked;
        private readonly Action _onNewGameClicked;

        private bool _initialized;
        private bool _dragging;
        private PileId _dragSource;
        private int _dragCount;

        public PresentationSystem(
            IGameSystem game,
            IInputSystem input,
            IGameAssetsSystem assets,
            ILayoutSystem layout)
        {
            _game = game;
            _input = input;

            var cardFactory = new CardViewFactory(assets);
            _board = new BoardModel(game, assets, layout, cardFactory);
            _hitTester = new HitTester(Camera.main);

            _onBoardChanged = HandleBoardChanged;
            _onGameWon = HandleGameWon;
            _onPointerDown = HandlePointerDown;
            _onPointerMove = HandlePointerMove;
            _onPointerUp = HandlePointerUp;
            _onTap = HandleTap;
            _onUndoClicked = HandleUndoClicked;
            _onRedoClicked = HandleRedoClicked;
            _onNewGameClicked = HandleNewGameClicked;
        }

        // REQ-PRES-001 / REQ-PRES-008
        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            await _board.BuildAsync(ct);

            _game.OnBoardChanged += _onBoardChanged;
            _game.OnGameWon += _onGameWon;

            _input.OnPointerDown += _onPointerDown;
            _input.OnPointerMove += _onPointerMove;
            _input.OnPointerUp += _onPointerUp;
            _input.OnTap += _onTap;

            if (_board.BoardView != null)
            {
                _board.BoardView.OnUndoClicked += _onUndoClicked;
                _board.BoardView.OnRedoClicked += _onRedoClicked;
                _board.BoardView.OnNewGameClicked += _onNewGameClicked;
            }

            _initialized = true;

            await _board.SyncAsync(_game.State, animate: false, ct);
        }

        // REQ-PRES-008
        public UniTask DisposeAsync()
        {
            if (!_initialized)
                return UniTask.CompletedTask;

            _game.OnBoardChanged -= _onBoardChanged;
            _game.OnGameWon -= _onGameWon;

            _input.OnPointerDown -= _onPointerDown;
            _input.OnPointerMove -= _onPointerMove;
            _input.OnPointerUp -= _onPointerUp;
            _input.OnTap -= _onTap;

            if (_board.BoardView != null)
            {
                _board.BoardView.OnUndoClicked -= _onUndoClicked;
                _board.BoardView.OnRedoClicked -= _onRedoClicked;
                _board.BoardView.OnNewGameClicked -= _onNewGameClicked;
            }

            _board.Dispose();
            _initialized = false;
            return UniTask.CompletedTask;
        }

        // Bridge for VContainer scope teardown -> async unsubscription.
        public void Dispose() => DisposeAsync().Forget();

        // REQ-PRES-003
        private void HandleBoardChanged(BoardState state)
        {
            _board.SyncAsync(state, animate: true).Forget();
        }

        // REQ-PRES-007
        private void HandleGameWon() => _board.ShowWin();

        // REQ-PRES-004.1
        private void HandlePointerDown(Vector2 screen)
        {
            if (!_initialized)
                return;

            var card = _hitTester.TopCardAt(screen);
            if (card == null)
                return;

            if (_board.TryLift(card, _hitTester.ToWorld(screen), out _dragSource, out _dragCount))
                _dragging = true;
        }

        // REQ-PRES-004.2
        private void HandlePointerMove(Vector2 screen)
        {
            if (_dragging)
                _board.MoveLifted(_hitTester.ToWorld(screen));
        }

        // REQ-PRES-004.3 / .4
        private void HandlePointerUp(Vector2 screen)
        {
            if (!_dragging)
                return;

            _dragging = false;

            var targetPile = _hitTester.PileAt(screen);
            if (targetPile != null && _game.TryMove(_dragSource, targetPile.PileId, _dragCount))
            {
                // Success: OnBoardChanged already triggered a re-sync; just drop the lift bookkeeping.
                _board.ClearLift();
                return;
            }

            _board.ReturnLiftedAsync().Forget();
        }

        // REQ-PRES-005
        private void HandleTap(Vector2 screen)
        {
            if (!_initialized)
                return;

            var pile = _hitTester.PileAt(screen);
            if (pile != null && pile.PileId.Kind == PileKind.Stock)
                _game.DrawStock();
        }

        // REQ-PRES-006
        private void HandleUndoClicked() => _game.Undo();
        private void HandleRedoClicked() => _game.Redo();

        private void HandleNewGameClicked()
        {
            _board.HideWin();
            _game.StartNewGame();
        }
    }
}
