using Timberborn.SelectionSystem;
using Timberborn.SingletonSystem;
using Timberborn.ToolSystem;
using UnityEngine;

namespace Timberborn.Splicetails {

    public class TreeMutationAreaVisualizer : ILoadableSingleton, ILateUpdatableSingleton {

        private static readonly string TreeCuttingGroupId = "TreeCutting";
        private static readonly Color MarkedTileColor = new Color(0.0f, 0.75f, 0.85f, 0.5f);

        private readonly TreeMutationArea _mutationArea;
        private readonly AreaHighlightingService _areaHighlightingService;
        private readonly EventBus _eventBus;

        private bool _visible;

        public void SetToolActive(bool active) => _visible = active;

        public TreeMutationAreaVisualizer(TreeMutationArea mutationArea,
                                          AreaHighlightingService areaHighlightingService,
                                          EventBus eventBus) {
            _mutationArea = mutationArea;
            _areaHighlightingService = areaHighlightingService;
            _eventBus = eventBus;
        }

        public void Load() {
            _eventBus.Register(this);
        }

        public void LateUpdateSingleton() {
            if (!_visible) return;
            foreach (var coord in _mutationArea.Area)
                _areaHighlightingService.DrawTile(coord, MarkedTileColor);
        }

        [OnEvent]
        public void OnToolGroupEntered(ToolGroupEnteredEvent e) {
            if (e.ToolGroup != null && e.ToolGroup.Id == TreeCuttingGroupId)
                _visible = true;
        }

        [OnEvent]
        public void OnToolGroupExited(ToolGroupExitedEvent e) {
            if (e.ToolGroup != null && e.ToolGroup.Id == TreeCuttingGroupId)
                _visible = false;
        }

    }
}
