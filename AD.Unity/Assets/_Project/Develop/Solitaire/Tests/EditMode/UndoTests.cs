using Appodeal.Solitaire.Runtime.Domain;
using Appodeal.Solitaire.Runtime.Undo;
using NUnit.Framework;

namespace Appodeal.Solitaire.Tests.EditMode
{
    public sealed class UndoTests
    {
        private static BoardChange Change(BoardChangeKind kind = BoardChangeKind.DrawStock) =>
            new() { Kind = kind, Count = 1 };

        [Test]
        public void NewSystem_HasEmptyStacks()
        {
            var undo = new UndoSystem();
            Assert.IsFalse(undo.CanUndo);
            Assert.IsFalse(undo.CanRedo);
        }

        [Test]
        public void Record_PushesToUndo_AndClearsRedo()
        {
            var undo = new UndoSystem();
            undo.Record(Change());
            undo.PopUndo(); // now redo has one entry
            Assert.IsTrue(undo.CanRedo);

            undo.Record(Change()); // a new move must clear redo
            Assert.IsTrue(undo.CanUndo);
            Assert.IsFalse(undo.CanRedo);
        }

        [Test]
        public void PopUndo_MovesRecordToRedo()
        {
            var undo = new UndoSystem();
            var change = Change(BoardChangeKind.Move);
            undo.Record(change);

            var popped = undo.PopUndo();

            Assert.AreSame(change, popped);
            Assert.IsFalse(undo.CanUndo);
            Assert.IsTrue(undo.CanRedo);
        }

        [Test]
        public void PopRedo_MovesRecordBackToUndo()
        {
            var undo = new UndoSystem();
            var change = Change();
            undo.Record(change);
            undo.PopUndo();

            var popped = undo.PopRedo();

            Assert.AreSame(change, popped);
            Assert.IsTrue(undo.CanUndo);
            Assert.IsFalse(undo.CanRedo);
        }

        [Test]
        public void Clear_EmptiesBothStacks()
        {
            var undo = new UndoSystem();
            undo.Record(Change());
            undo.Record(Change());
            undo.PopUndo();

            undo.Clear();

            Assert.IsFalse(undo.CanUndo);
            Assert.IsFalse(undo.CanRedo);
        }
    }
}
