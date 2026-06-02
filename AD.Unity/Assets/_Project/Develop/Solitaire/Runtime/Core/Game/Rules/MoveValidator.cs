using System.Collections.Generic;
using Appodeal.Solitaire.Runtime.Core.Domain;

namespace Appodeal.Solitaire.Runtime.Core.Game.Rules
{
    /// <summary>
    /// Worker: the move-validation algorithm (source + target checks) for Klondike Draw-1.
    /// Pure predicate over <see cref="BoardState"/>; never mutates the board.
    /// </summary>
    internal sealed class MoveValidator
    {
        public bool Validate(BoardState state, Move move)
        {
            // REQ-RULE-005.1: reject illegal source/target pile kinds.
            if (move.From.Kind == PileKind.Stock)
                return false;
            if (move.To.Kind is PileKind.Stock or PileKind.Waste)
                return false;
            if (move.Count < 1)
                return false;

            var source = state.GetPile(move.From);
            if (move.Count > source.Count)
                return false;

            var moved = GetMovedGroup(source, move.Count);

            if (!IsLiftable(move.From.Kind, moved, move.Count))
                return false;

            // The bottom card of the moved group lands on the target top.
            var landing = moved[0];

            return move.To.Kind switch
            {
                PileKind.Tableau => ValidateTableauTarget(state.GetPile(move.To), landing),
                PileKind.Foundation => ValidateFoundationTarget(state.GetPile(move.To), moved, move.Count),
                _ => false
            };
        }

        // The top `count` cards of a pile, ordered bottom-to-top (so index 0 is the card that lands).
        private static List<Card> GetMovedGroup(IReadOnlyList<Card> pile, int count)
        {
            var group = new List<Card>(count);
            for (int i = pile.Count - count; i < pile.Count; i++)
                group.Add(pile[i]);
            return group;
        }

        // REQ-RULE-003: the lifted group must be face-up and form a descending alternating-color run.
        private bool IsLiftable(PileKind sourceKind, IReadOnlyList<Card> group, int count)
        {
            if (sourceKind is PileKind.Waste or PileKind.Foundation)
                return count == 1 && group[0].FaceUp;

            // Tableau (group ordered bottom-to-top, i.e. high rank -> low rank).
            for (int i = 0; i < group.Count; i++)
            {
                if (!group[i].FaceUp)
                    return false;
                if (i > 0 && (group[i].Rank != group[i - 1].Rank - 1 || group[i].Color == group[i - 1].Color))
                    return false;
            }

            return true;
        }

        // REQ-RULE-001.
        private static bool ValidateTableauTarget(IReadOnlyList<Card> target, Card landing)
        {
            if (target.Count == 0)
                return landing.Rank == Rank.King;

            var top = target[^1];
            return landing.Rank == top.Rank - 1 && landing.Color != top.Color;
        }

        // REQ-RULE-002.
        private static bool ValidateFoundationTarget(IReadOnlyList<Card> target, IReadOnlyList<Card> moved, int count)
        {
            if (count != 1)
                return false;

            var card = moved[0];

            if (target.Count == 0)
                return card.Rank == Rank.Ace;

            var top = target[^1];
            return card.Suit == top.Suit && card.Rank == top.Rank + 1;
        }
    }
}
