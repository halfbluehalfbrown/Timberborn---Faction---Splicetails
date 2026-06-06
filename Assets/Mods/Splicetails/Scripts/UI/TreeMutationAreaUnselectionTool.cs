using System.Collections.Generic;
using Timberborn.AreaSelectionSystemUI;
using Timberborn.BlockSystem;
using Timberborn.SelectionSystem;
using Timberborn.Localization;
using Timberborn.SelectionToolSystem;
using Timberborn.SingletonSystem;
using Timberborn.TerrainQueryingSystem;
using Timberborn.ToolSystem;
using Timberborn.ToolSystemUI;
using UnityEngine;

namespace Timberborn.Splicetails {

    public class TreeMutationAreaUnselectionTool : ITool, IToolDescriptor, ILoadableSingleton {

        private static readonly string CursorKey = "CancelCursor";
        private static readonly string TitleLocKey = "Tool.SeumApplicationAreaRemove.Title";
        private static readonly string DescriptionLocKey = "Tool.SeumApplicationAreaRemove.Description";
        private static readonly Color PreviewColor = new Color(0.9f, 0.3f, 0.2f, 0.5f);

        private readonly TreeMutationArea _mutationArea;
        private readonly TreeMutationAreaVisualizer _visualizer;
        private readonly TerrainAreaService _terrainAreaService;
        private readonly AreaHighlightingService _areaHighlightingService;
        private readonly SelectionToolProcessorFactory _selectionToolProcessorFactory;
        private readonly MeasurableAreaDrawer _measurableAreaDrawer;
        private readonly ILoc _loc;

        private SelectionToolProcessor _processor;

        public TreeMutationAreaUnselectionTool(TreeMutationArea mutationArea,
                                               TreeMutationAreaVisualizer visualizer,
                                               TerrainAreaService terrainAreaService,
                                               AreaHighlightingService areaHighlightingService,
                                               SelectionToolProcessorFactory selectionToolProcessorFactory,
                                               MeasurableAreaDrawer measurableAreaDrawer, ILoc loc) {
            _mutationArea = mutationArea;
            _visualizer = visualizer;
            _terrainAreaService = terrainAreaService;
            _areaHighlightingService = areaHighlightingService;
            _selectionToolProcessorFactory = selectionToolProcessorFactory;
            _measurableAreaDrawer = measurableAreaDrawer;
            _loc = loc;
        }

        public void Load() {
            _processor = _selectionToolProcessorFactory.Create(PreviewCallback, ActionCallback, ShowNoneCallback, CursorKey);
        }

        public ToolDescription DescribeTool() =>
            new ToolDescription.Builder(_loc.T(TitleLocKey)).AddSection(_loc.T(DescriptionLocKey)).Build();

        public void Enter() {
            _visualizer.SetToolActive(true);
            _processor.Enter();
        }

        public void Exit() {
            _visualizer.SetToolActive(false);
            _areaHighlightingService.UnhighlightAll();
            _processor.Exit();
        }

        private void PreviewCallback(IEnumerable<Vector3Int> blocks, Ray ray) {
            _areaHighlightingService.UnhighlightAll();
            foreach (var coord in _terrainAreaService.InMapLeveledCoordinates(blocks, ray)) {
                if (_mutationArea.IsMarked(coord)) {
                    _areaHighlightingService.DrawTile(coord, PreviewColor);
                    _measurableAreaDrawer.AddMeasurableCoordinates(coord);
                }
            }
        }

        private void ActionCallback(IEnumerable<Vector3Int> blocks, Ray ray) {
            var coords = new List<Vector3Int>();
            foreach (var coord in _terrainAreaService.InMapLeveledCoordinates(blocks, ray))
                coords.Add(coord);
            _mutationArea.RemoveCoordinates(coords);
            _areaHighlightingService.UnhighlightAll();
        }

        private void ShowNoneCallback() {
            _areaHighlightingService.UnhighlightAll();
        }
    }
}
