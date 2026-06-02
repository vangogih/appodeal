using System;
using System.Collections.Generic;
using Appodeal.Solitaire.Runtime.Domain;
using Appodeal.Solitaire.Runtime.Game.Deal;
using Appodeal.Solitaire.Runtime.Game.Rules;
using Appodeal.Solitaire.Runtime.Undo;

namespace Appodeal.Solitaire.Runtime.Game
{
    public interface IGameSystem
    {
        BoardState State { get; }
        bool CanUndo { get; }
        bool CanRedo { get; }

        event Action<BoardState> OnBoardChanged;
        event Action OnGameWon;

        void StartNewGame(int? seed = null);
        bool TryMove(PileId from, PileId to, int count);
        bool DrawStock();
        bool Undo();
        bool Redo();
    }

    /// <summary>
    /// Head system of the gameplay (pure logic, no Unity). Owns the single mutable board, runs the
    /// Klondike Draw-1 loop, applies/reverts moves and exposes a read-only snapshot plus events.
    /// Uses <see cref="DealSubSystem"/> and <see cref="RulesSubSystem"/> via direct composition and
    /// records reversible changes into the external <see cref="IUndoSystem"/>.
    /// </summary>
    public sealed partial class GameSystem : IGameSystem
    {
        private readonly IUndoSystem _undo;
        private readonly IDealSubSystem _deal = new DealSubSystem();
        private readonly IRulesSubSystem _rules = new RulesSubSystem();

        private readonly List<Card> _stock = new();
        private readonly List<Card> _waste = new();
        private readonly List<Card>[] _foundations;
        private readonly List<Card>[] _tableau;

        private BoardState _snapshot;
        private bool _snapshotDirty = true;
        private bool _wonRaised;

        public event Action<BoardState> OnBoardChanged;
        public event Action OnGameWon;

        public GameSystem(IUndoSystem undo)
        {
            _undo = undo;

            _foundations = new List<Card>[RuntimeConstants.Game.Board.FoundationCount];
            for (int i = 0; i < _foundations.Length; i++)
                _foundations[i] = new List<Card>();

            _tableau = new List<Card>[RuntimeConstants.Game.Board.TableauColumns];
            for (int i = 0; i < _tableau.Length; i++)
                _tableau[i] = new List<Card>();
        }

        public bool CanUndo => _undo.CanUndo;
        public bool CanRedo => _undo.CanRedo;

        public BoardState State
        {
            get
            {
                if (_snapshotDirty || _snapshot == null)
                {
                    _snapshot = BuildSnapshot();
                    _snapshotDirty = false;
                }

                return _snapshot;
            }
        }

        // REQ-GAME-002
        public void StartNewGame(int? seed = null)
        {
            var dealt = _deal.Deal(seed);
            LoadFrom(dealt);

            _undo.Clear();
            _wonRaised = false;

            RaiseBoardChanged();
        }

        // REQ-GAME-004
        public bool TryMove(PileId from, PileId to, int count)
        {
            var move = new Move(from, to, count);

            if (!_rules.Validate(State, move))
                return false;

            var change = new BoardChange
            {
                Kind = BoardChangeKind.Move,
                From = from,
                To = to,
                Count = count
            };

            MoveCards(from, to, count);
            change.FlippedSourceTop = AutoFlip(from);

            _undo.Record(change);
            RaiseBoardChanged();
            CheckWin();
            return true;
        }

        // REQ-GAME-003
        public bool DrawStock()
        {
            BoardChange change;

            if (_stock.Count > 0)
            {
                ApplyDraw();
                change = new BoardChange { Kind = BoardChangeKind.DrawStock, Count = RuntimeConstants.Game.Stock.DrawCount };
            }
            else if (_waste.Count > 0)
            {
                int moved = _waste.Count;
                ApplyRecycle();
                change = new BoardChange { Kind = BoardChangeKind.RecycleStock, Count = moved };
            }
            else
            {
                return false;
            }

            _undo.Record(change);
            RaiseBoardChanged();
            return true;
        }

        // REQ-GAME-007.1
        public bool Undo()
        {
            if (!_undo.CanUndo)
                return false;

            RevertChange(_undo.PopUndo());
            RaiseBoardChanged();
            return true;
        }

        // REQ-GAME-007.2
        public bool Redo()
        {
            if (!_undo.CanRedo)
                return false;

            ApplyChange(_undo.PopRedo());
            RaiseBoardChanged();
            CheckWin();
            return true;
        }

        // REQ-GAME-006
        private void CheckWin()
        {
            if (_wonRaised)
                return;

            if (_rules.IsWin(State))
            {
                _wonRaised = true;
                OnGameWon?.Invoke();
            }
        }

        private void RaiseBoardChanged()
        {
            _snapshotDirty = true;
            OnBoardChanged?.Invoke(State);
        }
    }
}
