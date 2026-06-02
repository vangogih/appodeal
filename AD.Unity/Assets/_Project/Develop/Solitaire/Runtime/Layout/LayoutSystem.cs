using Appodeal.Solitaire.Runtime.Domain;
using UnityEngine;

namespace Appodeal.Solitaire.Runtime.Layout
{
    public interface ILayoutSystem
    {
        Vector3 GetPilePosition(PileId pile);
        Vector3 GetCardOffset(PileKind kind, int indexInPile, bool faceUp);
    }

    /// <summary>
    /// Pure layout math of the playing field: pile anchors and per-card offsets in board-local
    /// coordinates. Holds no board state and knows nothing about views; every value is derived from
    /// the arguments and <see cref="RuntimeConstants.Layout"/>.
    /// </summary>
    public sealed class LayoutSystem : ILayoutSystem
    {
        // REQ-LAY-001
        public Vector3 GetPilePosition(PileId pile)
        {
            return pile.Kind switch
            {
                PileKind.Stock => new Vector3(RuntimeConstants.Layout.StockX, RuntimeConstants.Layout.TopRowY, 0f),
                PileKind.Waste => new Vector3(RuntimeConstants.Layout.WasteX, RuntimeConstants.Layout.TopRowY, 0f),
                PileKind.Foundation => new Vector3(
                    RuntimeConstants.Layout.FoundationStartX + pile.Index * RuntimeConstants.Layout.PileStepX,
                    RuntimeConstants.Layout.TopRowY,
                    0f),
                PileKind.Tableau => new Vector3(
                    RuntimeConstants.Layout.TableauStartX + pile.Index * RuntimeConstants.Layout.PileStepX,
                    RuntimeConstants.Layout.TableauTopY,
                    0f),
                _ => Vector3.zero
            };
        }

        // REQ-LAY-002: a uniform downward fan in tableau; a stack (zero offset) elsewhere.
        public Vector3 GetCardOffset(PileKind kind, int indexInPile, bool faceUp)
        {
            if (kind != PileKind.Tableau)
                return Vector3.zero;

            float step = faceUp
                ? RuntimeConstants.Layout.Tableau.FaceUpFanY
                : RuntimeConstants.Layout.Tableau.FaceDownFanY;

            return new Vector3(0f, -indexInPile * step, 0f);
        }
    }
}
