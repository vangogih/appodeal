using Appodeal.Solitaire.Runtime.Domain;
using UnityEngine;

namespace Appodeal.Solitaire.Runtime.Presentation
{
    /// <summary>
    /// View (display only) of a pile slot: a hit-test area (<see cref="BoxCollider2D"/>) plus its
    /// <see cref="PileId"/>. Used as the drop target during drag-and-drop.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class PileView : MonoBehaviour
    {
        public PileId PileId { get; private set; }

        public void Setup(PileId pileId, Vector3 localPosition)
        {
            PileId = pileId;
            transform.localPosition = localPosition;
            name = $"Pile_{pileId.Kind}_{pileId.Index}";
        }

        /// <summary>Factory method: no extra dependencies, so a static creator on the View itself.</summary>
        public static PileView Create(PileView prefab, Transform parent)
        {
            var view = Instantiate(prefab, parent);
            return view;
        }
    }
}
