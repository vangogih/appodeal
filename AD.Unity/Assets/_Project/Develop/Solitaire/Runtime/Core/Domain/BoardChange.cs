namespace Appodeal.Solitaire.Runtime.Core.Domain
{
    public enum BoardChangeKind
    {
        Move,
        DrawStock,
        RecycleStock
    }

    /// <summary>
    /// A reversible record for Undo/Redo. Stores the minimum required to revert a change:
    /// kind, source/target, card count and the auto-flip flag.
    /// </summary>
    public sealed class BoardChange
    {
        public BoardChangeKind Kind;
        public PileId From;
        public PileId To;
        public int Count;
        public bool FlippedSourceTop; // whether the source pile's top card was auto-flipped
    }
}
