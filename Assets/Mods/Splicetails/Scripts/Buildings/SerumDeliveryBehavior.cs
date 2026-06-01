using Timberborn.BaseComponentSystem;
using Timberborn.BehaviorSystem;
using Timberborn.ReservableSystem;

namespace Timberborn.Splicetails {

    // Per-worker behavior. Walks the assigned worker to a reserved Mutatable tree
    // and triggers mutation on arrival.
    public class SerumDeliveryBehavior : Behavior, IAwakableComponent {

        private WalkToReservableExecutor _walkExecutor;
        private Mutatable _target;

        public void Awake() {
            _walkExecutor = GetComponent<WalkToReservableExecutor>();
        }

        public void SetTarget(Mutatable target) {
            _target = target;
        }

        public override Decision Decide(BehaviorAgent agent) {
            if (_target == null || _target.IsMutated) {
                Release();
                return Decision.ReleaseNow();
            }

            MutatableReacher reacher = _target.GetComponent<MutatableReacher>();
            if (reacher == null) {
                Release();
                return Decision.ReleaseNow();
            }

            switch (_walkExecutor.Launch(reacher)) {
                case ExecutorStatus.Success:
                    _target.Mutate();
                    _target = null;
                    return Decision.ReleaseNow();

                case ExecutorStatus.Failure:
                    Release();
                    return Decision.ReleaseNow();

                case ExecutorStatus.Running:
                    return Decision.ReturnWhenFinished(_walkExecutor);

                default:
                    Release();
                    return Decision.ReleaseNow();
            }
        }

        private void Release() {
            _target?.Unreserve();
            _target = null;
        }
    }
}
