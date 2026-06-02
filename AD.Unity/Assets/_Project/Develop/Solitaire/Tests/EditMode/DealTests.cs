using System.Collections.Generic;
using Appodeal.Solitaire.Runtime;
using Appodeal.Solitaire.Runtime.Domain;
using Appodeal.Solitaire.Runtime.Game.Deal;
using NUnit.Framework;

namespace Appodeal.Solitaire.Tests.EditMode
{
    public sealed class DealTests
    {
        [Test]
        public void Deal_ProducesFiftyTwoUniqueCards()
        {
            var board = new DealSubSystem().Deal(seed: 1);

            var seen = new HashSet<(Suit, Rank)>();
            int total = 0;

            foreach (var card in AllCards(board))
            {
                total++;
                Assert.IsTrue(seen.Add((card.Suit, card.Rank)), $"Duplicate card {card}");
            }

            Assert.AreEqual(RuntimeConstants.Game.Cards.DeckSize, total);
            Assert.AreEqual(RuntimeConstants.Game.Cards.DeckSize, seen.Count);
        }

        [Test]
        public void Deal_TableauColumnsAreSizedOneToSeven_WithTopFaceUp()
        {
            var board = new DealSubSystem().Deal(seed: 42);

            Assert.AreEqual(RuntimeConstants.Game.Board.TableauColumns, board.Tableau.Count);

            for (int i = 0; i < board.Tableau.Count; i++)
            {
                var column = board.Tableau[i];
                Assert.AreEqual(i + 1, column.Count, $"Column {i} size");

                for (int c = 0; c < column.Count; c++)
                {
                    bool shouldBeFaceUp = c == column.Count - 1;
                    Assert.AreEqual(shouldBeFaceUp, column[c].FaceUp, $"Column {i} card {c} face state");
                }
            }
        }

        [Test]
        public void Deal_StockHasTwentyFourFaceDownCards_WasteAndFoundationsEmpty()
        {
            var board = new DealSubSystem().Deal(seed: 7);

            Assert.AreEqual(24, board.Stock.Count);
            foreach (var card in board.Stock)
                Assert.IsFalse(card.FaceUp, "Stock cards must be face-down");

            Assert.AreEqual(0, board.Waste.Count);

            Assert.AreEqual(RuntimeConstants.Game.Board.FoundationCount, board.Foundations.Count);
            foreach (var foundation in board.Foundations)
                Assert.AreEqual(0, foundation.Count);
        }

        [Test]
        public void Deal_WithSameSeed_IsReproducible()
        {
            var a = new DealSubSystem().Deal(seed: 123);
            var b = new DealSubSystem().Deal(seed: 123);

            Assert.AreEqual(BoardStateBuilder.Signature(a), BoardStateBuilder.Signature(b));
        }

        [Test]
        public void Deal_WithDifferentSeeds_ProducesDifferentLayouts()
        {
            var a = new DealSubSystem().Deal(seed: 1);
            var b = new DealSubSystem().Deal(seed: 2);

            Assert.AreNotEqual(BoardStateBuilder.Signature(a), BoardStateBuilder.Signature(b));
        }

        private static IEnumerable<Card> AllCards(BoardState board)
        {
            foreach (var c in board.Stock) yield return c;
            foreach (var c in board.Waste) yield return c;
            foreach (var pile in board.Foundations)
                foreach (var c in pile) yield return c;
            foreach (var pile in board.Tableau)
                foreach (var c in pile) yield return c;
        }
    }
}
