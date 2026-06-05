using Timberborn.Rendering;
using Timberborn.RootProviders;
using Timberborn.SingletonSystem;
using Timberborn.ToolSystem;

namespace Timberborn.Splicetails {

    public class TreeMutationAreaVisualizer : ILoadableSingleton {

        private static readonly string TreeCuttingGroupId = "TreeCutting";
        private static readonly UnityEngine.Color TealTileColor = new UnityEngine.Color(0.0f, 0.75f, 0.85f, 0.6f);

        private readonly TreeMutationArea _mutationArea;
        private readonly AreaTileDrawerFactory _areaTileDrawerFactory;
        private readonly RootObjectProvider _rootObjectProvider;
        private readonly EventBus _eventBus;

        private AreaTileDrawer _areaTileDrawer;
        private bool _groupOpen;
        private bool _updateAreaOnEnter;

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
            _updateAreaOnEnter = true;
            _eventBus.Register(this);
        }

        [OnEvent]
        public void OnToolGroupEntered(ToolGroupEnteredEvent e) {
            if (e.ToolGroup == null || e.ToolGroup.Id != TreeCuttingGroupId)
                return;
            _groupOpen = true;
            if (_updateAreaOnEnter) {
                _updateAreaOnEnter = false;
                _areaTileDrawer.UpdateArea(_mutationArea.Area);
            }
            _areaTileDrawer.ShowAllTiles();
        }

        [OnEvent]
        public void OnToolGroupExited(ToolGroupExitedEvent e) {
            if (e.ToolGroup == null || e.ToolGroup.Id != TreeCuttingGroupId)
                return;
            _groupOpen = false;
            _areaTileDrawer.HideAllTiles();
        }

        [OnEvent]
        public void OnMutationAreaChanged(TreeMutationAreaChangedEvent _) {
            if (_groupOpen) {
                _areaTileDrawer.UpdateArea(_mutationArea.Area);
                _areaTileDrawer.ShowAllTiles();
            } else {
                _updateAreaOnEnter = true;
            }
        }
    }
}
