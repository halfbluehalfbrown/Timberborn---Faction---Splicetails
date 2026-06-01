using Timberborn.Rendering;
using Timberborn.RootProviders;
using Timberborn.SingletonSystem;

namespace Timberborn.Splicetails {

    // Draws persistent teal ground tiles over all cells marked for serum application.
    // Tiles are always visible so the player can see which trees are queued for mutation.
    // Acts as the "range border" showing the Splicer's active working area.
    public class TreeMutationAreaVisualizer : ILoadableSingleton {

        private static readonly UnityEngine.Color TealTileColor = new UnityEngine.Color(0.0f, 0.75f, 0.85f, 0.6f);

        private readonly TreeMutationArea _mutationArea;
        private readonly AreaTileDrawerFactory _areaTileDrawerFactory;
        private readonly RootObjectProvider _rootObjectProvider;
        private readonly EventBus _eventBus;

        private AreaTileDrawer _areaTileDrawer;

        public TreeMutationAreaVisualizer(TreeMutationArea mutationArea,
                                          AreaTileDrawerFactory areaTileDrawerFactory,
                                          RootObjectProvider rootObjectProvider,
                                          EventBus eventBus) {
            _mutationArea = mutationArea;
            _areaTileDrawerFactory = areaTileDrawerFactory;
            _rootObjectProvider = rootObjectProvider;
            _eventBus = eventBus;
        }

        public void Load() {
            var parent = _rootObjectProvider.CreateRootObject("TreeMutationAreaVisualizer");
            _areaTileDrawer = _areaTileDrawerFactory.Create(TealTileColor, parent);
            _areaTileDrawer.HideAllTiles();
            _eventBus.Register(this);
        }

        [OnEvent]
        public void OnMutationAreaChanged(TreeMutationAreaChangedEvent _) => Redraw();

        private void Redraw() {
            _areaTileDrawer.UpdateArea(_mutationArea.Area);
        }
    }
}
