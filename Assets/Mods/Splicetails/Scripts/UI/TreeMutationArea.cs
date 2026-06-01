using System.Collections.Generic;
using Timberborn.BlockSystem;
using Timberborn.Forestry;
using Timberborn.MapStateSystem;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.TerrainSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace Timberborn.Splicetails {

    public class TreeMutationAreaChangedEvent { }

    public class TreeMutationArea : ISaveableSingleton, ILoadableSingleton, IPostLoadableSingleton {

        private static readonly SingletonKey MutationAreaKey = new SingletonKey("TreeMutationArea");
        private static readonly ListKey<Vector3Int> CoordinatesKey = new ListKey<Vector3Int>("Coordinates");

        private readonly ISingletonLoader _singletonLoader;
        private readonly IBlockService _blockService;
        private readonly EventBus _eventBus;
        private readonly ITerrainService _terrainService;
        private readonly MapEditorMode _mapEditorMode;

        private readonly HashSet<Vector3Int> _area = new HashSet<Vector3Int>();
        private readonly Dictionary<Vector3Int, Mutatable> _mutablesInArea = new Dictionary<Vector3Int, Mutatable>();

        public IEnumerable<Mutatable> MutablesInArea => _mutablesInArea.Values;
        public IEnumerable<Vector3Int> Area => _area;
        public bool HasAnyTarget => _mutablesInArea.Count > 0;

        public TreeMutationArea(ISingletonLoader singletonLoader, IBlockService blockService,
                                EventBus eventBus, ITerrainService terrainService, MapEditorMode mapEditorMode) {
            _singletonLoader = singletonLoader;
            _blockService = blockService;
            _eventBus = eventBus;
            _terrainService = terrainService;
            _mapEditorMode = mapEditorMode;
        }

        public void Load() {
            if (_singletonLoader.TryGetSingleton(MutationAreaKey, out var loader))
                _area.UnionWith(loader.Get(CoordinatesKey));
            _terrainService.TerrainHeightChanged += (_, _) => Refresh();
            _eventBus.Register(this);
        }

        public void PostLoad() {
            foreach (var coord in _area)
                TryAddMutable(coord);
        }

        public void Save(ISingletonSaver saver) {
            if (!_mapEditorMode.IsMapEditor)
                saver.GetSingleton(MutationAreaKey).Set(CoordinatesKey, _area);
        }

        public bool IsMarked(Vector3Int coord) => _area.Contains(coord);

        public void AddCoordinates(IEnumerable<Vector3Int> coordinates) {
            foreach (var coord in coordinates) {
                _area.Add(coord);
                TryAddMutable(coord);
            }
            _eventBus.Post(new TreeMutationAreaChangedEvent());
        }

        public void RemoveCoordinates(IEnumerable<Vector3Int> coordinates) {
            foreach (var coord in coordinates) {
                _area.Remove(coord);
                _mutablesInArea.Remove(coord);
            }
            _eventBus.Post(new TreeMutationAreaChangedEvent());
        }

        public void RemoveCoordinate(Vector3Int coord) {
            _area.Remove(coord);
            _mutablesInArea.Remove(coord);
            _eventBus.Post(new TreeMutationAreaChangedEvent());
        }

        [OnEvent]
        public void OnTreeAdded(TreeAddedToCuttingAreaEvent _) => Refresh();

        private void TryAddMutable(Vector3Int coord) {
            var tree = _blockService.GetBottomObjectComponentAt<TreeComponent>(coord);
            if (tree != null) {
                var mutatable = tree.GetComponent<Mutatable>();
                if (mutatable != null && !mutatable.IsMutated)
                    _mutablesInArea[coord] = mutatable;
            }
        }

        private void Refresh() {
            _mutablesInArea.Clear();
            foreach (var coord in _area)
                TryAddMutable(coord);
        }
    }
}
