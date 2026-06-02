using System.Collections.Generic;
using Appodeal.Solitaire.Runtime.Domain;

namespace Appodeal.Solitaire.Runtime.Undo
{
    public interface IUndoSystem
    {
        bool CanUndo { get; }
        bool CanRedo { get; }

        void Record(BoardChange change);
        BoardChange PopUndo(); // call only when CanUndo
        BoardChange PopRedo(); // call only when CanRedo
        void Clear();
    }

    /// <summary>
    /// Stores the move history as two LIFO stacks of reversible <see cref="BoardChange"/> records.
    /// Intentionally "dumb": reverting/replaying the board state is done by <c>GameSystem</c>.
    /// </summary>
    public sealed class UndoSystem : IUndoSystem
    {
        private readonly Stack<BoardChange> _undo = new();
        private readonly Stack<BoardChange> _redo = new();

        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;

        public void Record(BoardChange change)
        {
            _undo.Push(change);
            _redo.Clear();
        }

        public BoardChange PopUndo()
        {
            var change = _undo.Pop();
            _redo.Push(change);
            return change;
        }

        public BoardChange PopRedo()
        {
            var change = _redo.Pop();
            _undo.Push(change);
            return change;
        }

        public void Clear()
        {
            _undo.Clear();
            _redo.Clear();
        }
    }
}
