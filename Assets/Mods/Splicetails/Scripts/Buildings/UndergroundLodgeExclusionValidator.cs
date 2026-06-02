using Timberborn.BlockSystem;
using UnityEngine;

namespace Timberborn.Splicetails {

    // Global IBlockObjectValidator that blocks placement of an Underground Lodge
    // within 2 tiles (Chebyshev) of any existing lodge. This prevents their 3x3
    // underground excavation zones from overlapping.
    //
    // IBlockObjectValidator (not IPreviewValidator) is required here:
    //   IPreviewValidator = warning color only, does NOT block placement.
    //   IBlockObjectValidator = actually prevents placement (red preview + blocked).
    public class UndergroundLodgeExclusionValidator : IBlockObjectValidator {

        private readonly IBlockService _blockService;

        public UndergroundLodgeExclusionValidator(IBlockService blockService) {
            _blockService = blockService;
        }

        public bool IsValid(BlockObject blockObject, out string errorMessage) {
            if (blockObject.GetComponent<UndergroundLodgeSpec>() == null) {
                errorMessage = null;
                return true;
            }
            foreach (var coord in blockObject.PositionedBlocks.GetAllCoordinates()) {
                for (int dx = -2; dx <= 2; dx++) {
                    for (int dy = -2; dy <= 2; dy++) {
                        if (dx == 0 && dy == 0) continue;
                        var check = new Vector3Int(coord.x + dx, coord.y + dy, coord.z);
                        if (_blockService.GetFirstObjectWithComponentAt<UndergroundLodgeSpec>(check) != null) {
                            errorMessage = "Too close to another Underground Lodge";
                            return false;
                        }
                    }
                }
                break; // 1x1 building — one coord
            }
            errorMessage = null;
            return true;
        }
    }
}
