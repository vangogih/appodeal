using Appodeal.Solitaire.Runtime.Assets;
using Appodeal.Solitaire.Runtime.Game;
using Appodeal.Solitaire.Runtime.Input;
using Appodeal.Solitaire.Runtime.Layout;
using Appodeal.Solitaire.Runtime.Presentation;
using Appodeal.Solitaire.Runtime.Undo;
using Appodeal.Solitaire.Runtime.Utilities;
using VContainer;
using VContainer.Unity;

namespace Appodeal.Solitaire.Runtime.Core
{
    public sealed class CoreScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // Infrastructure (kept self-contained for the Core scene).
            builder.Register<LoadingService>(Lifetime.Scoped);
            builder.Register<SceneManager>(Lifetime.Singleton);

            // Game domain (pure logic).
            builder.Register<IUndoSystem, UndoSystem>(Lifetime.Singleton);
            builder.Register<IGameSystem, GameSystem>(Lifetime.Singleton);

            // Unity-side systems.
            builder.Register<ILayoutSystem, LayoutSystem>(Lifetime.Singleton);
            builder.Register<IGameAssetsSystem, GameAssetsSystem>(Lifetime.Singleton);
            builder.Register<IPresentationSystem, PresentationSystem>(Lifetime.Singleton);

            // InputSystem is an ITickable entry point; AsImplementedInterfaces (inside
            // RegisterEntryPoint) also exposes it as IInputSystem for injection.
            builder.RegisterEntryPoint<InputSystem>(Lifetime.Singleton);

            builder.RegisterEntryPoint<CoreFlow>();
        }
    }
}
