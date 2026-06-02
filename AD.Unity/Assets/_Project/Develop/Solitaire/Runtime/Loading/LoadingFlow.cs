using Appodeal.Solitaire.Runtime.Core;
using Appodeal.Solitaire.Runtime.Utilities;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace Appodeal.Solitaire.Runtime.Loading
{
    /// <summary>
    /// Loading scene entry point. Advances the scene pipeline to the Meta scene.
    /// </summary>
    public sealed class LoadingFlow : IStartable
    {
        private readonly SceneManager _sceneManager;

        public LoadingFlow(SceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }

        public void Start()
        {
            _sceneManager.LoadScene(RuntimeConstants.Scenes.Meta).Forget();
        }
    }
}
