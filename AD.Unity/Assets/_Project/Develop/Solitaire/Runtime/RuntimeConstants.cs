using UnityEngine.SceneManagement;

namespace Appodeal.Solitaire.Runtime
{
    public static class RuntimeConstants
    {
        public static class Scenes
        {
            public static readonly int Bootstrap = SceneUtility.GetBuildIndexByScenePath("0.Bootstrap");
            public static readonly int Loading = SceneUtility.GetBuildIndexByScenePath("1.Loading");
            public static readonly int Meta = SceneUtility.GetBuildIndexByScenePath("2.Meta");
            public static readonly int Core = SceneUtility.GetBuildIndexByScenePath("3.Core");
            public static readonly int Empty = SceneUtility.GetBuildIndexByScenePath("4.Empty");
        }

        public static class Game
        {
            public static class Board
            {
                public const int TableauColumns = 7;
                public const int FoundationCount = 4;
            }

            public static class Cards
            {
                public const int DeckSize = 52;
                public const int RankCount = 13;
                public const int SuitCount = 4;
            }

            public static class Stock
            {
                public const int DrawCount = 1;
            }
        }

        public static class Input
        {
            // Displacement (screen pixels) below which a press+release counts as a tap, not a drag.
            public const float DragThresholdPixels = 12f;
        }

        public static class Assets
        {
            public static class Cards
            {
                // {0} = suit (e.g. "Clubs"), {1} = rank (e.g. "Ace"); resolved under a Resources folder.
                public const string FacePathFormat = "Cards/{0}_{1}";
                public const string BackPath = "Cards/Back";
            }

            public static class Prefabs
            {
                public const string CardView = "Prefabs/CardView";
                public const string PileView = "Prefabs/PileView";
                public const string BoardView = "Prefabs/BoardView";
            }
        }

        public static class Layout
        {
            public const float TopRowY = 4f;
            public const float TableauTopY = 2f;

            public const float StockX = -6f;
            public const float WasteX = -4.5f;
            public const float FoundationStartX = 0f;
            public const float TableauStartX = -6f;
            public const float PileStepX = 1.5f;

            public const float CardSize = 1f;

            public static class Tableau
            {
                public const float FaceUpFanY = 0.45f;
                public const float FaceDownFanY = 0.18f;
            }
        }

        public static class Presentation
        {
            public const float MoveDuration = 0.18f;
            public const float FlipDuration = 0.12f;

            public const float DragLiftScale = 1.05f;
            public const float DragLiftZ = -1f;

            // Base sorting order applied per pile depth so later cards render on top.
            public const int CardSortingBase = 0;
            public const int DragSortingBoost = 1000;
        }

        public static class Undo
        {
            public static class History
            {
                // 0 == unlimited within a game (minimal scope keeps full history).
                public const int MaxDepth = 0;
            }
        }
    }
}
