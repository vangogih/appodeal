using System.Collections.Generic;
using Appodeal.Solitaire.Runtime;
using Appodeal.Solitaire.Runtime.Domain;
using Appodeal.Solitaire.Runtime.Game.Rules;
using NUnit.Framework;
using static Appodeal.Solitaire.Tests.EditMode.BoardStateBuilder;

namespace Appodeal.Solitaire.Tests.EditMode
{
    public sealed class RulesTests
    {
        private RulesSubSystem _rules;

        [SetUp]
        public void SetUp() => _rules = new RulesSubSystem();

        [Test]
        public void Tableau_EmptyColumn_AcceptsKingOnly()
        {
            var withKing = Build(tableau: Columns(
                Col(Up(Suit.Spades, Rank.King)),
                Col()));
            Assert.IsTrue(_rules.Validate(withKing, new Move(PileId.Tableau(0), PileId.Tableau(1), 1)));

            var withQueen = Build(tableau: Columns(
                Col(Up(Suit.Spades, Rank.Queen)),
                Col()));
            Assert.IsFalse(_rules.Validate(withQueen, new Move(PileId.Tableau(0), PileId.Tableau(1), 1)));
        }

        [Test]
        public void Tableau_NonEmptyColumn_RequiresDescendingAlternatingColor()
        {
            // red 6 onto black 7 -> valid
            var valid = Build(tableau: Columns(
                Col(Up(Suit.Hearts, Rank.Six)),
                Col(Up(Suit.Spades, Rank.Seven))));
            Assert.IsTrue(_rules.Validate(valid, new Move(PileId.Tableau(0), PileId.Tableau(1), 1)));

            // red 6 onto red 7 -> invalid (same color)
            var sameColor = Build(tableau: Columns(
                Col(Up(Suit.Hearts, Rank.Six)),
                Col(Up(Suit.Diamonds, Rank.Seven))));
            Assert.IsFalse(_rules.Validate(sameColor, new Move(PileId.Tableau(0), PileId.Tableau(1), 1)));

            // red 5 onto black 7 -> invalid (wrong rank)
            var wrongRank = Build(tableau: Columns(
                Col(Up(Suit.Hearts, Rank.Five)),
                Col(Up(Suit.Spades, Rank.Seven))));
            Assert.IsFalse(_rules.Validate(wrongRank, new Move(PileId.Tableau(0), PileId.Tableau(1), 1)));
        }

        [Test]
        public void Foundation_EmptyAcceptsAceOnly_ThenAscendingSameSuit()
        {
            var ace = Build(tableau: Columns(Col(Up(Suit.Clubs, Rank.Ace))));
            Assert.IsTrue(_rules.Validate(ace, new Move(PileId.Tableau(0), PileId.Foundation(0), 1)));

            var two = Build(tableau: Columns(Col(Up(Suit.Clubs, Rank.Two))));
            Assert.IsFalse(_rules.Validate(two, new Move(PileId.Tableau(0), PileId.Foundation(0), 1)));

            // 2 of clubs onto a foundation holding ace of clubs -> valid
            var ascending = Build(
                tableau: Columns(Col(Up(Suit.Clubs, Rank.Two))),
                foundations: FoundationsWith(0, Up(Suit.Clubs, Rank.Ace)));
            Assert.IsTrue(_rules.Validate(ascending, new Move(PileId.Tableau(0), PileId.Foundation(0), 1)));

            // 2 of spades onto ace of clubs -> invalid (suit mismatch)
            var wrongSuit = Build(
                tableau: Columns(Col(Up(Suit.Spades, Rank.Two))),
                foundations: FoundationsWith(0, Up(Suit.Clubs, Rank.Ace)));
            Assert.IsFalse(_rules.Validate(wrongSuit, new Move(PileId.Tableau(0), PileId.Foundation(0), 1)));
        }

        [Test]
        public void Foundation_RejectsMultiCardMove()
        {
            var state = Build(tableau: Columns(
                Col(Up(Suit.Clubs, Rank.Two), Up(Suit.Hearts, Rank.Ace))));
            Assert.IsFalse(_rules.Validate(state, new Move(PileId.Tableau(0), PileId.Foundation(0), 2)));
        }

        [Test]
        public void MovableRun_ValidDescendingAlternatingGroup_CanMove()
        {
            // red 7, black 6, red 5 (bottom-to-top) onto black 8 -> valid group of 3
            var valid = Build(tableau: Columns(
                Col(Up(Suit.Hearts, Rank.Seven), Up(Suit.Spades, Rank.Six), Up(Suit.Diamonds, Rank.Five)),
                Col(Up(Suit.Clubs, Rank.Eight))));
            Assert.IsTrue(_rules.Validate(valid, new Move(PileId.Tableau(0), PileId.Tableau(1), 3)));

            // broken run (same color in the middle) -> invalid
            var broken = Build(tableau: Columns(
                Col(Up(Suit.Hearts, Rank.Seven), Up(Suit.Diamonds, Rank.Six), Up(Suit.Spades, Rank.Five)),
                Col(Up(Suit.Clubs, Rank.Eight))));
            Assert.IsFalse(_rules.Validate(broken, new Move(PileId.Tableau(0), PileId.Tableau(1), 3)));
        }

        [Test]
        public void MovableRun_FaceDownCardInGroup_CannotMove()
        {
            var state = Build(tableau: Columns(
                Col(Down(Suit.Hearts, Rank.Seven), Up(Suit.Spades, Rank.Six)),
                Col(Up(Suit.Clubs, Rank.Eight))));
            Assert.IsFalse(_rules.Validate(state, new Move(PileId.Tableau(0), PileId.Tableau(1), 2)));
        }

        [Test]
        public void Validate_RejectsIllegalSourceAndTarget()
        {
            var state = Build(
                stock: new List<Card> { Down(Suit.Clubs, Rank.Three) },
                waste: new List<Card> { Up(Suit.Hearts, Rank.Five) },
                tableau: Columns(Col(Up(Suit.Spades, Rank.Six))));

            // Stock is never a TryMove source.
            Assert.IsFalse(_rules.Validate(state, new Move(PileId.Stock, PileId.Tableau(0), 1)));
            // Stock / Waste are never targets.
            Assert.IsFalse(_rules.Validate(state, new Move(PileId.Tableau(0), PileId.Stock, 1)));
            Assert.IsFalse(_rules.Validate(state, new Move(PileId.Waste, PileId.Stock, 1)));
        }

        [Test]
        public void Waste_TopCard_OntoTableau_IsValid()
        {
            // red 6 from waste onto black 7
            var state = Build(
                waste: new List<Card> { Up(Suit.Hearts, Rank.Six) },
                tableau: Columns(Col(Up(Suit.Spades, Rank.Seven))));
            Assert.IsTrue(_rules.Validate(state, new Move(PileId.Waste, PileId.Tableau(0), 1)));
        }

        [Test]
        public void IsWin_TrueWhenAllFiftyTwoCardsInFoundations()
        {
            var foundations = new List<Card>[RuntimeConstants.Game.Board.FoundationCount];
            for (int suit = 0; suit < foundations.Length; suit++)
            {
                foundations[suit] = new List<Card>();
                for (var rank = Rank.Ace; rank <= Rank.King; rank++)
                    foundations[suit].Add(Up((Suit)suit, rank));
            }

            var won = Build(foundations: foundations);
            Assert.IsTrue(_rules.IsWin(won));

            var notWon = Build();
            Assert.IsFalse(_rules.IsWin(notWon));
        }

        private static List<Card> Col(params Card[] cards) => new(cards);

        private static List<Card>[] Columns(params List<Card>[] columns)
        {
            var result = new List<Card>[RuntimeConstants.Game.Board.TableauColumns];
            for (int i = 0; i < result.Length; i++)
                result[i] = i < columns.Length ? columns[i] : new List<Card>();
            return result;
        }

        private static List<Card>[] FoundationsWith(int index, params Card[] cards)
        {
            var result = new List<Card>[RuntimeConstants.Game.Board.FoundationCount];
            for (int i = 0; i < result.Length; i++)
                result[i] = new List<Card>();
            result[index].AddRange(cards);
            return result;
        }
    }
}
