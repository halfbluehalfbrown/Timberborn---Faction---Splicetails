using Timberborn.Rendering;
using Timberborn.RootProviders;
using Timberborn.SingletonSystem;
using Timberborn.ToolSystem;

namespace Timberborn.Splicetails {

    // Draws persistent teal ground tiles over all cells marked for serum application.
    // Tiles are visible only while the TreeCutting tool group is open, matching the
    // vanilla tree-cutting area visualizer pattern. A deferred-redraw flag ensures
    // changes made while the group is closed are applied immediately on re-open.
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
            _areaTileDrawer.HideAllTiles();
            _eventBus.Register(this);
        }

        [OnEvent]
        public void OnToolGroupEntered(ToolGroupEnteredEvent e) {
            if (e.ToolGroup == null || e.ToolGroup.Id != TreeCuttingGroupId)
                return;
            _groupOpen = true;
            _updateAreaOnEnter = false;
            Redraw();
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
                Redraw();
            } else {
                _updateAreaOnEnter = true;
            }
        }

        private void Redraw() {
            _areaTileDrawer.UpdateArea(_mutationArea.Area);
        }
    }
}
