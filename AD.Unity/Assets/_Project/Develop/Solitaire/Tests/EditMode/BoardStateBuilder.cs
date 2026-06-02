using System.Collections.Generic;
using System.Text;
using Appodeal.Solitaire.Runtime.Core;
using Appodeal.Solitaire.Runtime.Core.Domain;
using Appodeal.Solitaire.Runtime.Core.Game;

namespace Appodeal.Solitaire.Tests.EditMode
{
    /// <summary>Helper for assembling and comparing <see cref="BoardState"/> instances in tests.</summary>
    internal static class BoardStateBuilder
    {
        public static Card Up(Suit suit, Rank rank) => new(suit, rank, faceUp: true);
        public static Card Down(Suit suit, Rank rank) => new(suit, rank, faceUp: false);

        public static BoardState Build(
            List<Card> stock = null,
            List<Card> waste = null,
            List<Card>[] foundations = null,
            List<Card>[] tableau = null)
        {
            stock ??= new List<Card>();
            waste ??= new List<Card>();

            foundations ??= EmptyPiles(RuntimeConstants.Game.Board.FoundationCount);
            tableau ??= EmptyPiles(RuntimeConstants.Game.Board.TableauColumns);

            return new BoardState(stock, waste, ToReadOnly(foundations), ToReadOnly(tableau));
        }

        private static List<Card>[] EmptyPiles(int count)
        {
            var piles = new List<Card>[count];
            for (int i = 0; i < count; i++)
                piles[i] = new List<Card>();
            return piles;
        }

        private static IReadOnlyList<Card>[] ToReadOnly(List<Card>[] piles)
        {
            var result = new IReadOnlyList<Card>[piles.Length];
            for (int i = 0; i < piles.Length; i++)
                result[i] = piles[i];
            return result;
        }

        public static string Signature(BoardState state)
        {
            var sb = new StringBuilder();
            AppendPile(sb, "S", state.Stock);
            AppendPile(sb, "W", state.Waste);
            for (int i = 0; i < state.Foundations.Count; i++)
                AppendPile(sb, $"F{i}", state.Foundations[i]);
            for (int i = 0; i < state.Tableau.Count; i++)
                AppendPile(sb, $"T{i}", state.Tableau[i]);
            return sb.ToString();
        }

        private static void AppendPile(StringBuilder sb, string label, IReadOnlyList<Card> pile)
        {
            sb.Append(label).Append(':');
            foreach (var card in pile)
                sb.Append((int)card.Suit).Append((int)card.Rank).Append(card.FaceUp ? 'u' : 'd').Append(',');
            sb.Append('|');
        }
    }
}
