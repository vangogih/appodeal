using System;
using System.Threading;
using Appodeal.Solitaire.Runtime.Core.Assets;
using Appodeal.Solitaire.Runtime.Core.Game;
using Appodeal.Solitaire.Runtime.Core.Presentation;
using Appodeal.Solitaire.Runtime.Utilities.Logging;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace Appodeal.Solitaire.Runtime.Core
{
    /// <summary>
    /// Core scene entry point and the single owner of the gameplay lifecycle. Loads assets,
    /// initializes presentation and starts a game; a New Game request tears the whole stack down
    /// (DisposeAsync) and re-initializes it (assets included). Initialization and disposal of the
    /// systems are driven only from here — async disposal lives in this Flow, never in the systems
    /// themselves (see unity-rules.md).
    /// </summary>
    public sealed class CoreFlow : IAsyncStartable, IDisposable
    {
        private readonly IGameAssetsSystem _gameAssets;
        private readonly IPresentationSystem _presentation;
        private readonly IGameSystem _game;

        // Stored delegate so the restart subscription can be removed (see event-subscriptions.md).
        private readonly Action _onNewGameRequested;

        private CancellationToken _lifetime;
        private bool _initialized;
        private bool _restarting;

        public CoreFlow(IGameAssetsSystem gameAssets, IPresentationSystem presentation, IGameSystem game)
        {
            _gameAssets = gameAssets;
            _presentation = presentation;
            _game = game;
            _onNewGameRequested = HandleNewGameRequested;
        }

        public async UniTask StartAsync(CancellationToken cancellation)
        {
            _lifetime = cancellation;

            try
            {
                await InitializeAsync(cancellation);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Log.Default.E(e);
            }
        }

        private async UniTask InitializeAsync(CancellationToken ct)
        {
            await _gameAssets.LoadAsync(ct);
            await _presentation.InitializeAsync(ct);

            _presentation.OnNewGameRequested += _onNewGameRequested;
            _initialized = true;

            _game.StartNewGame();
        }

        public async UniTask DisposeAsync()
        {
            if (!_initialized)
                return;

            _initialized = false;

            _presentation.OnNewGameRequested -= _onNewGameRequested;
            _presentation.Teardown();
            await _gameAssets.UnloadAsync();
        }

        private void HandleNewGameRequested() => RestartAsync().Forget();

        // A restart is a full dispose + re-initialize cycle, gated so overlapping requests are ignored.
        private async UniTask RestartAsync()
        {
            if (_restarting)
                return;

            _restarting = true;

            try
            {
                await DisposeAsync();
                await InitializeAsync(_lifetime);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Log.Default.E(e);
            }
            finally
            {
                _restarting = false;
            }
        }

        // VContainer scope teardown is synchronous; bridge to async disposal (allowed in a Flow).
        public void Dispose() => DisposeAsync().Forget();
    }
}
