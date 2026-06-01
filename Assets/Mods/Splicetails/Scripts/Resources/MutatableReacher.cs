using Timberborn.BaseComponentSystem;
using Timberborn.EntitySystem;
using Timberborn.NaturalResourcesModelSystem;
using Timberborn.ReservableSystem;
using Timberborn.WalkingSystem;
using UnityEngine;

namespace Timberborn.Splicetails {

    public class MutatableReacher : ReservableReacher, IInitializableEntity {

        private static readonly float ApproachRadius = 0.5f;

        private readonly PositionDestinationFactory _positionDestinationFactory;

        private PositionDestination _destination;

        public override IDestination Destination => _destination;

        public MutatableReacher(PositionDestinationFactory positionDestinationFactory) {
            _positionDestinationFactory = positionDestinationFactory;
        }

        public void InitializeEntity() {
            var centerProvider = GetComponent<NaturalResourceCenterProvider>();
            Vector3 center = centerProvider != null
                ? centerProvider.GetWorldCenter()
                : GameObject.transform.position + new Vector3(0.5f, 0f, 0.5f);
            _destination = _positionDestinationFactory.Create(center, ApproachRadius);
        }

        public override void NotifyReservableReached(BaseComponent agent) {
        }
    }
}
