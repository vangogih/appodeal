using Appodeal.Solitaire.Runtime.Core.Game;

namespace Appodeal.Solitaire.Runtime.Core.Domain
{
    public readonly struct Card
    {
        public readonly Suit Suit;
        public readonly Rank Rank;
        public readonly bool FaceUp;

        public Card(Suit suit, Rank rank, bool faceUp = false)
        {
            Suit = suit;
            Rank = rank;
            FaceUp = faceUp;
        }

        public CardColor Color => Suit is Suit.Hearts or Suit.Diamonds ? CardColor.Red : CardColor.Black;

        public Card AsFaceUp(bool faceUp) => new(Suit, Rank, faceUp);

        public override string ToString() => $"{Rank} of {Suit} ({(FaceUp ? "up" : "down")})";
    }
}
