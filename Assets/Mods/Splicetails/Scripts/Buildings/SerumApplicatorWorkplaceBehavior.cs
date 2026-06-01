using System.Linq;
using Timberborn.BehaviorSystem;
using Timberborn.WorkSystem;
using UnityEngine;

namespace Timberborn.Splicetails {

    // Finds the nearest marked, unmutated mature tree and assigns a worker to apply serum.
    public class SerumApplicatorWorkplaceBehavior : WorkplaceBehavior {

        private readonly TreeMutationArea _mutationArea;

        public SerumApplicatorWorkplaceBehavior(TreeMutationArea mutationArea) {
            _mutationArea = mutationArea;
        }

        public override Decision Decide(BehaviorAgent agent) {
            Mutatable target = FindNearestTarget();
            if (target == null)
                return Decision.ReleaseNow();

            target.Reserve();
            SerumDeliveryBehavior delivery = agent.GetComponent<SerumDeliveryBehavior>();
            delivery.SetTarget(target);
            Decision deliveryDecision = delivery.Decide(agent);
            if (!deliveryDecision.ShouldReleaseNow)
                return Decision.TransferNow(delivery, in deliveryDecision);

            target.Unreserve();
            return Decision.ReleaseNow();
        }

        private Mutatable FindNearestTarget() {
            Vector3 origin = GameObject.transform.position;
            return _mutationArea.MutablesInArea
                .Where(m => m.CanBeMutated)
                .OrderBy(m => Vector3.SqrMagnitude(origin - m.GameObject.transform.position))
                .FirstOrDefault();
        }
    }
}
