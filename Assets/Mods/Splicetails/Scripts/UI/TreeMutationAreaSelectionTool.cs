using System.Collections.Generic;
using Timberborn.AreaSelectionSystemUI;
using Timberborn.BlockSystem;
using Timberborn.Forestry;
using Timberborn.SelectionSystem;
using Timberborn.Localization;
using Timberborn.SelectionToolSystem;
using Timberborn.SingletonSystem;
using Timberborn.TerrainQueryingSystem;
using Timberborn.ToolSystem;
using Timberborn.ToolSystemUI;
using UnityEngine;

namespace Timberborn.Splicetails {

    public class TreeMutationAreaSelectionTool : ITool, IToolDescriptor, ILoadableSingleton {

        private static readonly string CursorKey = "CutTreeCursor";
        private static readonly string TitleLocKey = "Tool.SeumApplicationArea.Title";
        private static readonly string DescriptionLocKey = "Tool.SeumApplicationArea.Description";
        private static readonly Color PreviewColor = new Color(0.0f, 0.85f, 0.75f, 0.6f);
        private static readonly Color OutOfRangeColor = new Color(0.5f, 0.5f, 0.5f, 0.35f);

        private readonly TreeMutationArea _mutationArea;
        private readonly TerrainAreaService _terrainAreaService;
        private readonly AreaHighlightingService _areaHighlightingService;
        private readonly IBlockService _blockService;
        private readonly SelectionToolProcessorFactory _selectionToolProcessorFactory;
        private readonly MeasurableAreaDrawer _measurableAreaDrawer;
        private readonly ILoc _loc;

        private SelectionToolProcessor _processor;

        public TreeMutationAreaSelectionTool(TreeMutationArea mutationArea, TerrainAreaService terrainAreaService,
                                             AreaHighlightingService areaHighlightingService, IBlockService blockService,
                                             SelectionToolProcessorFactory selectionToolProcessorFactory,
                                             MeasurableAreaDrawer measurableAreaDrawer, ILoc loc) {
            _mutationArea = mutationArea;
            _terrainAreaService = terrainAreaService;
            _areaHighlightingService = areaHighlightingService;
            _blockService = blockService;
            _selectionToolProcessorFactory = selectionToolProcessorFactory;
            _measurableAreaDrawer = measurableAreaDrawer;
            _loc = loc;
        }

        public void Load() {
            _processor = _selectionToolProcessorFactory.Create(PreviewCallback, ActionCallback, ShowNoneCallback, CursorKey);
        }

        public ToolDescription DescribeTool() =>
            new ToolDescription.Builder(_loc.T(TitleLocKey)).AddSection(_loc.T(DescriptionLocKey)).Build();

        public void Enter() => _processor.Enter();

        public void Exit() {
            _areaHighlightingService.UnhighlightAll();
            _processor.Exit();
        }

        private void PreviewCallback(IEnumerable<Vector3Int> blocks, Ray ray) {
            _areaHighlightingService.UnhighlightAll();
            foreach (var coord in _terrainAreaService.InMapLeveledCoordinates(blocks, ray)) {
                if (!_mutationArea.IsMarked(coord)) {
                    bool inRange = _mutationArea.IsInSplicerRange(coord);
                    _areaHighlightingService.DrawTile(coord, inRange ? PreviewColor : OutOfRangeColor);
                    if (inRange)
                        _measurableAreaDrawer.AddMeasurableCoordinates(coord);
                }
            }
        }

        private void ActionCallback(IEnumerable<Vector3Int> blocks, Ray ray) {
            var coords = new List<Vector3Int>();
            foreach (var coord in _terrainAreaService.InMapLeveledCoordinates(blocks, ray))
                coords.Add(coord);
            _mutationArea.AddCoordinates(coords);
            _areaHighlightingService.UnhighlightAll();
        }

        private void ShowNoneCallback() {
            _areaHighlightingService.UnhighlightAll();
        }
    }
}
