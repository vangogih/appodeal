namespace Appodeal.Solitaire.Runtime
{
    public enum Suit
    {
        Clubs,
        Diamonds,
        Hearts,
        Spades
    }

    public enum CardColor
    {
        Black,
        Red
    }

    public enum Rank
    {
        Ace = 1,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King
    }

    public enum PileKind
    {
        Stock,
        Waste,
        Foundation,
        Tableau
    }
}
