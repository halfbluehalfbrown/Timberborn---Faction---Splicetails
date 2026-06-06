using Timberborn.Rendering;
using Timberborn.RootProviders;
using Timberborn.SingletonSystem;
using Timberborn.ToolSystem;
using UnityEngine;

namespace Timberborn.Splicetails {

    public class TreeMutationAreaVisualizer : ILoadableSingleton {

        private static readonly string TreeCuttingGroupId = "TreeCutting";
        private static readonly Color TealTileColor = new Color(0.0f, 0.75f, 0.85f, 0.6f);

        private readonly TreeMutationArea _mutationArea;
        private readonly AreaTileDrawerFactory _areaTileDrawerFactory;
        private readonly RootObjectProvider _rootObjectProvider;
        private readonly EventBus _eventBus;

        private AreaTileDrawer _areaTileDrawer;
        private bool _visible;

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

        // Called by the mark and unmark tools on Enter/Exit.
        public void SetToolActive(bool active) {
            _visible = active;
            if (active) Redraw();
            else _areaTileDrawer.HideAllTiles();
        }

        // Backup: also respond to the group-level open/close event so tiles appear
        // even if the individual tool Enter fires before SetToolActive is called.
        [OnEvent]
        public void OnToolGroupEntered(ToolGroupEnteredEvent e) {
            if (e.ToolGroup?.Id == TreeCuttingGroupId) SetToolActive(true);
        }

        [OnEvent]
        public void OnToolGroupExited(ToolGroupExitedEvent e) {
            if (e.ToolGroup?.Id == TreeCuttingGroupId) SetToolActive(false);
        }

        [OnEvent]
        public void OnMutationAreaChanged(TreeMutationAreaChangedEvent _) {
            if (_visible) Redraw();
        }

        private void Redraw() {
            _areaTileDrawer.UpdateArea(_mutationArea.Area);
            _areaTileDrawer.ShowAllTiles();
        }
    }
}
