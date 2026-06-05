using System.Collections.Generic;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace Timberborn.Splicetails {

    public class SplicerRangeChangedEvent { }

    // Tracks the grid positions of all finished Splicer buildings and exposes
    // helpers for range checks (Chebyshev ≤ 5 tiles) used by the mutation-area
    // tools to filter out-of-range tile selections.
    public class SplicerRangeService {

        public const int Range = 5;

        private readonly HashSet<Vector3Int> _splicerPositions = new HashSet<Vector3Int>();
        private readonly EventBus _eventBus;

        public SplicerRangeService(EventBus eventBus) {
            _eventBus = eventBus;
        }

        public void RegisterSplicer(Vector3Int position) {
            if (_splicerPositions.Add(position))
                _eventBus.Post(new SplicerRangeChangedEvent());
        }

        public void UnregisterSplicer(Vector3Int position) {
            if (_splicerPositions.Remove(position))
                _eventBus.Post(new SplicerRangeChangedEvent());
        }

        public bool IsInRange(Vector3Int coord) {
            foreach (var pos in _splicerPositions) {
                if (ChebyshevXY(coord, pos) <= Range)
                    return true;
            }
            return false;
        }

        public bool HasAnySplicer => _splicerPositions.Count > 0;

        // Returns all coordinates within [Range] tiles (Chebyshev XY) of any Splicer,
        // at the same Z-level as the Splicer.  Used by the range overlay visualizer.
        public IEnumerable<Vector3Int> GetRangeTiles() {
            foreach (var pos in _splicerPositions) {
                for (int dx = -Range; dx <= Range; dx++) {
                    for (int dy = -Range; dy <= Range; dy++) {
                        yield return new Vector3Int(pos.x + dx, pos.y + dy, pos.z);
                    }
                }
            }
        }

        private static int ChebyshevXY(Vector3Int a, Vector3Int b) {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }
    }
}
