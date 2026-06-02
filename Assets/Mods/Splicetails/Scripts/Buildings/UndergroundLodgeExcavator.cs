using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using Timberborn.TerrainPhysics;
using UnityEngine;

namespace Timberborn.Splicetails {

    // On placement (InitializeEntity): records the building's grid coordinate.
    // On construction complete (OnEnterFinishedState): excavates the 3x3 terrain
    // layer one level below the surface (coord.z-2).
    public class UndergroundLodgeExcavator : BaseComponent, IInitializableEntity, IFinishedStateListener {

        private readonly TerrainDestroyer _terrainDestroyer;
        private Vector3Int _doorCoord;

        public UndergroundLodgeExcavator(TerrainDestroyer terrainDestroyer) {
            _terrainDestroyer = terrainDestroyer;
        }

        public void InitializeEntity() {
            var blockObject = GetComponent<BlockObject>();
            if (blockObject == null) return;
            foreach (var coord in blockObject.PositionedBlocks.GetAllCoordinates()) {
                _doorCoord = coord;
                break;
            }
        }

        public void OnEnterFinishedState() {
            // Excavate 3x3 area one layer below the surface (coord.z-2 because
            // building completion removes coord.z terrain = first layer).
            for (int dx = -1; dx <= 1; dx++) {
                for (int dy = -1; dy <= 1; dy++) {
                    var excavate = new Vector3Int(_doorCoord.x + dx, _doorCoord.y + dy, _doorCoord.z - 2);
                    if (excavate.z >= 0)
                        _terrainDestroyer.DestroyTerrain(excavate);
                }
            }
        }

        public void OnExitFinishedState() { }
    }
}
