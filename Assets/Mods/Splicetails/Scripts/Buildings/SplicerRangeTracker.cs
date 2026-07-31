using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;   // IFinishedStateListener, IFinishedPostLoadStateListener, BlockObject
using Timberborn.EntitySystem;  // IDeletableEntity
using UnityEngine;

namespace Timberborn.Splicetails {

    // Attached to every Splicer building via SplicetailsConfigurator (AddDecorator on SerumApplicatorSpec).
    // Registers the building's grid position with SplicerRangeService when construction
    // finishes (or when a saved game is loaded), and unregisters when the building is deleted.
    public class SplicerRangeTracker : BaseComponent, IAwakableComponent, IFinishedStateListener, IFinishedPostLoadStateListener, IDeletableEntity {

        private readonly SplicerRangeService _rangeService;

        private BlockObject _blockObject;
        private Vector3Int _position;
        private bool _registered;

        public SplicerRangeTracker(SplicerRangeService rangeService) {
            _rangeService = rangeService;
        }

        // IAwakableComponent — called after DI construction, before entity events
        public void Awake() {
            _blockObject = GetComponent<BlockObject>();
        }

        // IFinishedStateListener — fires when beavers complete construction
        public void OnEnterFinishedState() => Register();
        public void OnExitFinishedState() => Unregister();

        // IFinishedPostLoadStateListener — fires on save-game load for already-built buildings
        public void OnEnterFinishedPostLoadState() => Register();
        public void OnExitFinishedPostLoadState() => Unregister();

        // IDeletableEntity — fires when the building is demolished
        public void DeleteEntity() => Unregister();

        private void Register() {
            if (_registered) return;
            _position = _blockObject.CoordinatesAtBaseZ;
            _rangeService.RegisterSplicer(_position);
            _registered = true;
        }

        private void Unregister() {
            if (!_registered) return;
            _rangeService.UnregisterSplicer(_position);
            _registered = false;
        }
    }
}
