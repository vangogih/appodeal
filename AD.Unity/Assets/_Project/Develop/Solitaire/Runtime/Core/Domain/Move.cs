namespace Appodeal.Solitaire.Runtime.Core.Domain
{
    public readonly struct Move
    {
        public readonly PileId From;
        public readonly PileId To;
        public readonly int Count; // 1 for most moves; >1 for tableau sequences

        public Move(PileId from, PileId to, int count)
        {
            From = from;
            To = to;
            Count = count;
        }
    }
}
