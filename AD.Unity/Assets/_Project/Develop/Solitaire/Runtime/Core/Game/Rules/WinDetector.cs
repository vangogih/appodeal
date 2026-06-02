using Appodeal.Solitaire.Runtime.Core.Domain;

namespace Appodeal.Solitaire.Runtime.Core.Game.Rules
{
    /// <summary>
    /// Worker: the win-check algorithm. A win occurs when all foundations together hold 52 cards.
    /// Pure predicate, no side effects.
    /// </summary>
    internal sealed class WinDetector
    {
        public bool IsWin(BoardState state)
        {
            int total = 0;
            for (int i = 0; i < state.Foundations.Count; i++)
                total += state.Foundations[i].Count;

            return total == RuntimeConstants.Game.Cards.DeckSize;
        }
    }
}
