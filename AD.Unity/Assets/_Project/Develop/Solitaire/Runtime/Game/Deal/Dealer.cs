using System.Collections.Generic;
using Appodeal.Solitaire.Runtime.Domain;

namespace Appodeal.Solitaire.Runtime.Game.Deal
{
    /// <summary>
    /// Worker: lays a shuffled deck out into a Klondike <see cref="BoardState"/>:
    /// 7 tableau columns sized 1..7 (top face-up), the remaining 24 cards to the stock,
    /// empty waste and foundations. Service logic only.
    /// </summary>
    internal sealed class Dealer
    {
        public BoardState Lay(IReadOnlyList<Card> deck)
        {
            var tableau = new List<Card>[RuntimeConstants.Game.Board.TableauColumns];
            for (int i = 0; i < tableau.Length; i++)
                tableau[i] = new List<Card>();

            int cursor = 0;

            for (int column = 0; column < RuntimeConstants.Game.Board.TableauColumns; column++)
            {
                int cardsInColumn = column + 1;
                for (int c = 0; c < cardsInColumn; c++)
                {
                    bool isTop = c == cardsInColumn - 1;
                    tableau[column].Add(deck[cursor++].AsFaceUp(isTop));
                }
            }

            var stock = new List<Card>();
            for (; cursor < deck.Count; cursor++)
                stock.Add(deck[cursor].AsFaceUp(false));

            var waste = new List<Card>();

            var foundations = new List<Card>[RuntimeConstants.Game.Board.FoundationCount];
            for (int i = 0; i < foundations.Length; i++)
                foundations[i] = new List<Card>();

            return new BoardState(stock, waste, foundations, tableau);
        }
    }
}
