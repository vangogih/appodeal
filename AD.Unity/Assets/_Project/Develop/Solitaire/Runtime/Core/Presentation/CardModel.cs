using System.Threading;
using Appodeal.Solitaire.Runtime.Core.Assets;
using Appodeal.Solitaire.Runtime.Core.Domain;
using Appodeal.Solitaire.Runtime.Core.Game;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Appodeal.Solitaire.Runtime.Core.Presentation
{
    /// <summary>
    /// Model of a single card: owns its <see cref="CardView"/> and depends only on the external
    /// <see cref="IGameAssetsSystem"/> (for the face/back sprites). Translates the card's
    /// <see cref="Card"/> data into the view's sprite, sorting and position. Manages the view lifecycle.
    /// </summary>
    public sealed class CardModel
    {
        private readonly IGameAssetsSystem _assets;

        private CardView _view;
        private bool _hasCard;
        private bool _lastFaceUp;

        public CardModel(IGameAssetsSystem assets)
        {
            _assets = assets;
        }

        public CardView View => _view;

        public async UniTask InitializeAsync(CardViewFactory factory, Transform parent, CancellationToken ct = default)
        {
            _view = await factory.CreateAsync(this, parent, ct);
        }

        public void Apply(Card card, PileId pile, int indexInPile, Vector3 localPosition, int sortingOrder, bool animate)
        {
            if (_view == null)
                return;

            _view.ResetScale();
            _view.SetSlot(pile, indexInPile, card.FaceUp);
            _view.SetSortingOrder(sortingOrder);

            bool faceChanged = _hasCard && _lastFaceUp != card.FaceUp;
            var visual = ResolveVisual(card);

            // REQ-PRES-003.2: animate the flip when a card's face state changed.
            if (animate && faceChanged)
            {
                _view.AnimateFlip(visual.Sprite, visual.LabelText, visual.LabelColor, visual.ShowLabel);
            }
            else
            {
                _view.SetSprite(visual.Sprite);
                _view.SetLabel(visual.LabelText, visual.LabelColor, visual.ShowLabel);
            }

            _view.SetLocalPosition(localPosition, animate);
            _view.SetColliderEnabled(true);

            _hasCard = true;
            _lastFaceUp = card.FaceUp;
        }

        public void Lift(Vector3 worldPosition, int sortingBoost)
        {
            if (_view == null)
                return;

            _view.SetSortingOrder(sortingBoost);
            _view.SetColliderEnabled(false);
            _view.SetLiftScale(RuntimeConstants.Game.Presentation.DragLiftScale);
            _view.SetWorldPosition(WithLiftZ(worldPosition));
        }

        public void MoveLiftedTo(Vector3 worldPosition)
        {
            if (_view != null)
                _view.SetWorldPosition(WithLiftZ(worldPosition));
        }

        private static Vector3 WithLiftZ(Vector3 worldPosition)
        {
            worldPosition.z = RuntimeConstants.Game.Presentation.DragLiftZ;
            return worldPosition;
        }

        // Chooses the sprite + label for a card. Real per-card art (if dropped into
        // Resources/Cards) wins and hides the label; otherwise a default card background is shown
        // with the rank+suit (face) or the back motif rendered via UniText.
        private CardVisual ResolveVisual(Card card)
        {
            if (card.FaceUp)
            {
                var art = _assets.GetCardSprite(card.Suit, card.Rank);
                if (art != null)
                    return new CardVisual(art, string.Empty, Color.white, false);

                var background = _view != null ? _view.DefaultFront : null;
                return new CardVisual(background, FaceText(card), FaceColor(card), true);
            }

            var back = _assets.GetCardBackSprite();
            if (back != null)
                return new CardVisual(back, string.Empty, Color.white, false);

            var backBackground = _view != null ? _view.DefaultBack : null;
            return new CardVisual(backBackground, BackText, BackColor, true);
        }

        private readonly struct CardVisual
        {
            public readonly Sprite Sprite;
            public readonly string LabelText;
            public readonly Color LabelColor;
            public readonly bool ShowLabel;

            public CardVisual(Sprite sprite, string labelText, Color labelColor, bool showLabel)
            {
                Sprite = sprite;
                LabelText = labelText;
                LabelColor = labelColor;
                ShowLabel = showLabel;
            }
        }

        private static readonly Color RedColor = new Color32(200, 30, 30, 255);
        private static readonly Color BlackColor = new Color32(25, 25, 25, 255);
        private static readonly Color BackColor = new Color32(255, 205, 60, 255);

        // Centered card-back motif: a full-color star emoji rendered by UniText.
        // Identical on every face-down card, as card backs must be.
        private const string BackText = "🌟"; // 

        private static string FaceText(Card card) => RankText(card.Rank) + SuitGlyph(card.Suit);

        private static Color FaceColor(Card card) => card.Color == CardColor.Red ? RedColor : BlackColor;

        private static string RankText(Rank rank) => rank switch
        {
            Rank.Ace => "A",
            Rank.Ten => "10",
            Rank.Jack => "J",
            Rank.Queen => "Q",
            Rank.King => "K",
            _ => ((int)rank).ToString()
        };

        // Suit symbols with the emoji variation selector (U+FE0F) so UniText picks the color glyph.
        private static string SuitGlyph(Suit suit) => suit switch
        {
            Suit.Spades => "♠️",
            Suit.Hearts => "♥️",
            Suit.Diamonds => "♦️",
            Suit.Clubs => "♣️",
            _ => string.Empty
        };

        public void Dispose()
        {
            if (_view != null)
            {
                Object.Destroy(_view.gameObject);
                _view = null;
            }
        }
    }
}
