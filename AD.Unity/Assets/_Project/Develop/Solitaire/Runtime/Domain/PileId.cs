using System;

namespace Appodeal.Solitaire.Runtime.Domain
{
    public readonly struct PileId : IEquatable<PileId>
    {
        public readonly PileKind Kind;
        public readonly int Index; // Stock/Waste: 0; Foundation: 0..3; Tableau: 0..6

        public PileId(PileKind kind, int index)
        {
            Kind = kind;
            Index = index;
        }

        public static PileId Stock => new(PileKind.Stock, 0);
        public static PileId Waste => new(PileKind.Waste, 0);
        public static PileId Foundation(int index) => new(PileKind.Foundation, index);
        public static PileId Tableau(int index) => new(PileKind.Tableau, index);

        public bool Equals(PileId other) => Kind == other.Kind && Index == other.Index;
        public override bool Equals(object obj) => obj is PileId other && Equals(other);
        public override int GetHashCode() => ((int)Kind * 397) ^ Index;

        public static bool operator ==(PileId a, PileId b) => a.Equals(b);
        public static bool operator !=(PileId a, PileId b) => !a.Equals(b);

        public override string ToString() => $"{Kind}[{Index}]";
    }
}
