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

            // Building is 3x3. Excavate one layer below each surface tile.
            // coord.z = surface; coord.z-1 removed by building; coord.z-2 = the chamber.
            foreach (var coord in blockObject.PositionedBlocks.GetAllCoordinates()) {
                var excavate = new Vector3Int(coord.x, coord.y, coord.z - 2);
                if (excavate.z >= 0)
                    _terrainDestroyer.DestroyTerrain(excavate);
            }
        }

        public void OnExitFinishedState() { }
    }
}
