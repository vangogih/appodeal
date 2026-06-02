using System.Collections.Generic;
using Appodeal.Solitaire.Runtime.Core.Domain;

namespace Appodeal.Solitaire.Runtime.Core.Game.Deal
{
    public interface IDealSubSystem
    {
        BoardState Deal(int? seed = null);
    }

    /// <summary>
    /// Subsystem of <c>GameSystem</c>: builds a 52-card deck, shuffles it and lays it out into
    /// the initial Klondike board. Orchestrates the <see cref="DeckShuffler"/> and <see cref="Dealer"/>
    /// workers via direct composition; invisible outside its parent system.
    /// </summary>
    public sealed class DealSubSystem : IDealSubSystem
    {
        private readonly DeckShuffler _shuffler = new();
        private readonly Dealer _dealer = new();

        public BoardState Deal(int? seed = null)
        {
            var deck = BuildDeck();
            _shuffler.Shuffle(deck, seed);
            return _dealer.Lay(deck);
        }

        private static List<Card> BuildDeck()
        {
            var deck = new List<Card>(RuntimeConstants.Game.Cards.DeckSize);

            for (int suit = 0; suit < RuntimeConstants.Game.Cards.SuitCount; suit++)
            {
                for (int rank = 1; rank <= RuntimeConstants.Game.Cards.RankCount; rank++)
                    deck.Add(new Card((Suit)suit, (Rank)rank, faceUp: false));
            }

            return deck;
        }
    }
}
