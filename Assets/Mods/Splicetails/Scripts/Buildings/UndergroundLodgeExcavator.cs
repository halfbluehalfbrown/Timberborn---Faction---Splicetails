using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using Timberborn.TerrainPhysics;
using UnityEngine;

namespace Timberborn.Splicetails {

    // The building is 1x1 (just the door hatch). The 8 surrounding tiles stay as
    // natural terrain so beavers can plant crops and trees around the door.
    // On construction complete, excavates a 3x3 area one layer below the surface
    // to create the underground chamber.
    public class UndergroundLodgeExcavator : BaseComponent, IFinishedStateListener {

        private readonly TerrainDestroyer _terrainDestroyer;

        public UndergroundLodgeExcavator(TerrainDestroyer terrainDestroyer) {
            _terrainDestroyer = terrainDestroyer;
        }

        public void OnEnterFinishedState() {
            var blockObject = GetComponent<BlockObject>();
            if (blockObject == null) return;

            // Use the door tile as the centre; excavate a 3x3 footprint below it.
            foreach (var coord in blockObject.PositionedBlocks.GetAllCoordinates()) {
                for (int dx = -1; dx <= 1; dx++) {
                    for (int dy = -1; dy <= 1; dy++) {
                        var excavate = new Vector3Int(coord.x + dx, coord.y + dy, coord.z - 2);
                        if (excavate.z >= 0)
                            _terrainDestroyer.DestroyTerrain(excavate);
                    }
                }
                break; // only one block in the 1x1 building
            }
        }

        public void OnExitFinishedState() { }
    }
}
