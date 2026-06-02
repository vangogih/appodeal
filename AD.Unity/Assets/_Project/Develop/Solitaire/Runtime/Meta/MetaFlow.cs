using Appodeal.Solitaire.Runtime.Core;
using Appodeal.Solitaire.Runtime.Utilities;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace Appodeal.Solitaire.Runtime.Meta
{
    /// <summary>
    /// Meta scene entry point. Advances the scene pipeline to the Core (gameplay) scene.
    /// </summary>
    public sealed class MetaFlow : IStartable
    {
        private readonly SceneManager _sceneManager;

        public MetaFlow(SceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }

        public void Start()
        {
            _sceneManager.LoadScene(RuntimeConstants.Scenes.Core).Forget();
        }
    }
}
