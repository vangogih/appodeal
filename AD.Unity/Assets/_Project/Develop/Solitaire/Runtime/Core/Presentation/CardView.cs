using System.Threading;
using Appodeal.Solitaire.Runtime.Core.Domain;
using Cysharp.Threading.Tasks;
using LightSide;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace Appodeal.Solitaire.Runtime.Core.Presentation
{
    /// <summary>
    /// View (display only) of a single card: a <see cref="SpriteRenderer"/> + <see cref="BoxCollider2D"/>.
    /// Holds no game logic; the owning <see cref="CardModel"/> drives sprite, sorting and position.
    /// Also stores the card's current pile/index so the presentation hit-test can build a move.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private BoxCollider2D _collider;

        [Header("UniText labels (used when no per-card sprite is supplied)")]
        [SerializeField] private UniTextWorld _centerLabel;
        [SerializeField] private UniTextWorld _cornerLabel;

        [Header("Default backgrounds (white front / decorated back)")]
        [SerializeField] private Sprite _defaultFront;
        [SerializeField] private Sprite _defaultBack;

        private CardModel _model;
        private MotionHandle _moveHandle;
        private MotionHandle _flipHandle;

        public PileId Pile { get; private set; }
        public int IndexInPile { get; private set; }
        public bool FaceUp { get; private set; }
        public CardModel Model => _model;

        public Sprite DefaultFront => _defaultFront;
        public Sprite DefaultBack => _defaultBack;

        public UniTask InitializeAsync(CardModel model, CancellationToken ct = default)
        {
            _model = model;

            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();
            if (_collider == null)
                _collider = GetComponent<BoxCollider2D>();

            return UniTask.CompletedTask;
        }

        public void SetSlot(PileId pile, int indexInPile, bool faceUp)
        {
            Pile = pile;
            IndexInPile = indexInPile;
            FaceUp = faceUp;
        }

        public void SetSprite(Sprite sprite)
        {
            if (_renderer != null)
                _renderer.sprite = sprite;
        }

        // Drives the rank/suit (or back motif) text. Called with show=false when a per-card
        // sprite is used, so the procedurally rendered label is hidden behind real art.
        public void SetLabel(string text, Color color, bool show)
        {
            ApplyLabel(_centerLabel, text, color, show);
            ApplyLabel(_cornerLabel, text, color, show);
        }

        private static void ApplyLabel(UniTextWorld label, string text, Color color, bool show)
        {
            if (label == null)
                return;

            if (label.gameObject.activeSelf != show)
                label.gameObject.SetActive(show);

            if (!show)
                return;

            label.color = color;
            label.Text = text;
        }

        // The label is offset by +1 so each card's text occupies its own sorting order, sitting
        // above the card's own sprite but below the next card's sprite (orders are spaced by 2).
        public void SetSortingOrder(int order)
        {
            if (_renderer != null)
                _renderer.sortingOrder = order;
            if (_centerLabel != null)
                _centerLabel.SortingOrder = order + 1;
            if (_cornerLabel != null)
                _cornerLabel.SortingOrder = order + 1;
        }

        public void SetLocalPosition(Vector3 localPosition, bool animate)
        {
            if (_moveHandle.IsActive())
                _moveHandle.TryCancel();

            if (!animate)
            {
                transform.localPosition = localPosition;
                return;
            }

            _moveHandle = LMotion
                .Create(transform.localPosition, localPosition, RuntimeConstants.Game.Presentation.MoveDuration)
                .WithEase(Ease.OutQuad)
                .BindToLocalPosition(transform)
                .AddTo(gameObject);
        }

        public void SetWorldPosition(Vector3 worldPosition)
        {
            if (_moveHandle.IsActive())
                _moveHandle.TryCancel();

            transform.position = worldPosition;
        }

        // REQ-PRES-003.2: animate a face change (flip) by squashing on X, swapping the sprite, expanding back.
        public void AnimateFlip(Sprite newSprite, string labelText, Color labelColor, bool showLabel)
        {
            if (_flipHandle.IsActive())
                _flipHandle.TryCancel();

            float half = RuntimeConstants.Game.Presentation.FlipDuration * 0.5f;

            _flipHandle = LMotion
                .Create(1f, 0f, half)
                .WithEase(Ease.InQuad)
                .WithOnComplete(() =>
                {
                    SetSprite(newSprite);
                    SetLabel(labelText, labelColor, showLabel);
                    LMotion.Create(0f, 1f, half)
                        .WithEase(Ease.OutQuad)
                        .Bind(transform, static (x, t) =>
                        {
                            var s = t.localScale;
                            s.x = x;
                            t.localScale = s;
                        })
                        .AddTo(gameObject);
                })
                .Bind(transform, static (x, t) =>
                {
                    var s = t.localScale;
                    s.x = x;
                    t.localScale = s;
                })
                .AddTo(gameObject);
        }

        public void SetLiftScale(float scale)
        {
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        public void ResetScale()
        {
            transform.localScale = Vector3.one;
        }

        public void SetColliderEnabled(bool enabled)
        {
            if (_collider != null)
                _collider.enabled = enabled;
        }

        /// <summary>Factory method: no extra dependencies, so a static creator on the View itself.</summary>
        public static CardView Create(CardView prefab, Transform parent)
        {
            var view = Instantiate(prefab, parent);
            return view;
        }
    }
}
