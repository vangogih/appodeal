using Appodeal.Solitaire.Runtime.Core;
using Appodeal.Solitaire.Runtime.Utilities;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace Appodeal.Solitaire.Runtime.Bootstrap
{
    /// <summary>
    /// First scene entry point. Advances the scene pipeline to the Loading scene.
    /// </summary>
    public sealed class BootstrapFlow : IStartable
    {
        private readonly SceneManager _sceneManager;

        public BootstrapFlow(SceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }

        public void Start()
        {
            _sceneManager.LoadScene(RuntimeConstants.Scenes.Loading).Forget();
        }
    }
}
