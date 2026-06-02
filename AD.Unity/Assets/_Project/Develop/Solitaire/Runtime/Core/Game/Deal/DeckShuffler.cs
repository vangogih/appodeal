using System;
using System.Collections.Generic;

namespace Appodeal.Solitaire.Runtime.Core.Game.Deal
{
    /// <summary>
    /// Worker: shuffles a card list in place using the Fisher-Yates algorithm.
    /// Service logic only (the algorithm); the subsystem decides when to use it.
    /// </summary>
    internal sealed class DeckShuffler
    {
        public void Shuffle<T>(IList<T> deck, int? seed)
        {
            var random = seed.HasValue ? new Random(seed.Value) : new Random();

            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }
        }
    }
}
