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
        private readonly ToolGroupService _toolGroupService;
        private readonly EventBus _eventBus;

        public TreeMutationAreaVisualizer(TreeMutationArea mutationArea,
                                          AreaHighlightingService areaHighlightingService,
                                          ToolGroupService toolGroupService,
                                          EventBus eventBus) {
            _mutationArea = mutationArea;
            _areaHighlightingService = areaHighlightingService;
            _toolGroupService = toolGroupService;
            _eventBus = eventBus;
        }

        public void Load() {
            _eventBus.Register(this);
        }

        public void LateUpdateSingleton() {
            var active = _toolGroupService.ActiveToolGroup;
            if (active == null || active.Id != TreeCuttingGroupId) return;
            foreach (var coord in _mutationArea.Area)
                _areaHighlightingService.DrawTile(coord, MarkedTileColor);
        }

    }
}
