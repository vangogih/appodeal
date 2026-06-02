using Appodeal.Solitaire.Runtime.Assets;
using Appodeal.Solitaire.Runtime.Game;
using Appodeal.Solitaire.Runtime.Presentation;
using Appodeal.Solitaire.Runtime.Utilities.Logging;
using VContainer.Unity;

namespace Appodeal.Solitaire.Runtime.Core
{
    /// <summary>
    /// Core scene entry point. Loads game assets, initializes presentation (subscriptions + board
    /// skeleton), then starts a new game. Initialization logic lives here / in InitializeAsync rather
    /// than in constructors / Awake / Start (see unity-rules.md).
    /// </summary>
    public sealed class CoreFlow : IStartable
    {
        private readonly IGameAssetsSystem _gameAssets;
        private readonly IPresentationSystem _presentation;
        private readonly IGameSystem _game;

        public CoreFlow(IGameAssetsSystem gameAssets, IPresentationSystem presentation, IGameSystem game)
        {
            _gameAssets = gameAssets;
            _presentation = presentation;
            _game = game;
        }

        public async void Start()
        {
            try
            {
                await _gameAssets.LoadAsync();
                await _presentation.InitializeAsync();
                _game.StartNewGame();
            }
            catch (System.Exception e)
            {
                Log.Default.E(e);
            }
        }
    }
}
