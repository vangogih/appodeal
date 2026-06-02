using System;
using System.Collections.Generic;

namespace Appodeal.Solitaire.Runtime.Domain
{
    /// <summary>
    /// Immutable read-only snapshot of the board. Only <c>GameSystem</c> builds it from its
    /// private mutable state; consumers read it but cannot mutate the board.
    /// The top of any pile is its last element.
    /// </summary>
    public sealed class BoardState
    {
        public IReadOnlyList<Card> Stock { get; }
        public IReadOnlyList<Card> Waste { get; }
        public IReadOnlyList<IReadOnlyList<Card>> Foundations { get; } // 4 piles
        public IReadOnlyList<IReadOnlyList<Card>> Tableau { get; }     // 7 piles

        public BoardState(
            IReadOnlyList<Card> stock,
            IReadOnlyList<Card> waste,
            IReadOnlyList<IReadOnlyList<Card>> foundations,
            IReadOnlyList<IReadOnlyList<Card>> tableau)
        {
            Stock = stock;
            Waste = waste;
            Foundations = foundations;
            Tableau = tableau;
        }

        public IReadOnlyList<Card> GetPile(PileId id)
        {
            return id.Kind switch
            {
                PileKind.Stock => Stock,
                PileKind.Waste => Waste,
                PileKind.Foundation => Foundations[id.Index],
                PileKind.Tableau => Tableau[id.Index],
                _ => throw new ArgumentOutOfRangeException(nameof(id), id.Kind, "Unknown pile kind")
            };
        }
    }
}
