using Timberborn.Rendering;
using Timberborn.RootProviders;
using Timberborn.SingletonSystem;
using Timberborn.ToolSystem;
using UnityEngine;

namespace Timberborn.Splicetails {

    // Draws a semi-transparent overlay on all tiles within Splicer range while the
    // TreeCutting tool group is open.  This lets players see exactly where they can
    // paint serum application areas before trying to mark tiles.
    public class SplicerRangeVisualizer : ILoadableSingleton {

        private static readonly string TreeCuttingGroupId = "TreeCutting";
        // Soft yellow-green tint — distinct from the teal mutation-area tiles
        private static readonly Color RangeColor = new Color(0.85f, 0.95f, 0.3f, 0.25f);

        private readonly SplicerRangeService _rangeService;
        private readonly AreaTileDrawerFactory _areaTileDrawerFactory;
        private readonly RootObjectProvider _rootObjectProvider;
        private readonly EventBus _eventBus;

        private AreaTileDrawer _areaTileDrawer;
        private bool _groupOpen;

        public SplicerRangeVisualizer(SplicerRangeService rangeService,
                                      AreaTileDrawerFactory areaTileDrawerFactory,
                                      RootObjectProvider rootObjectProvider,
                                      EventBus eventBus) {
            _rangeService = rangeService;
            _areaTileDrawerFactory = areaTileDrawerFactory;
            _rootObjectProvider = rootObjectProvider;
            _eventBus = eventBus;
        }

        public void Load() {
            var parent = _rootObjectProvider.CreateRootObject("SplicerRangeVisualizer");
            _areaTileDrawer = _areaTileDrawerFactory.Create(RangeColor, parent);
            _areaTileDrawer.HideAllTiles();
            _eventBus.Register(this);
        }

        [OnEvent]
        public void OnToolGroupEntered(ToolGroupEnteredEvent e) {
            if (e.ToolGroup == null || e.ToolGroup.Id != TreeCuttingGroupId)
                return;
            _groupOpen = true;
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
        public void OnSplicerRangeChanged(SplicerRangeChangedEvent _) {
            if (_groupOpen)
                Redraw();
        }

        private void Redraw() {
            _areaTileDrawer.UpdateArea(_rangeService.GetRangeTiles());
            _areaTileDrawer.ShowAllTiles();
        }
    }
}
