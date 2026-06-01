using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Growing;
using Timberborn.NaturalResourcesLifecycle;
using Timberborn.ReservableSystem;
using UnityEngine;

namespace Timberborn.Splicetails {

    public class Mutatable : BaseComponent, IAwakableComponent {

        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
        private static readonly int TintEnabledId = Shader.PropertyToID("_TintEnabled");
        private static readonly Color MutationTint = new Color(0.2f, 0.5f, 0.9f, 1f);

        private readonly TreeMutationArea _mutationArea;

        private LivingNaturalResource _living;
        private Growable _growable;
        private Reservable _reservable;
        private BlockObject _blockObject;

        public bool IsMutated { get; private set; }

        public bool CanBeMutated =>
            !IsMutated &&
            !_reservable.Reserved &&
            !_living.IsDead &&
            _growable.GrowthProgress >= 1f;

        public bool IsMarked => _blockObject != null && _mutationArea.IsMarked(_blockObject.Coordinates);

        public Mutatable(TreeMutationArea mutationArea) {
            _mutationArea = mutationArea;
        }

        public void Awake() {
            _living = GetComponent<LivingNaturalResource>();
            _growable = GetComponent<Growable>();
            _reservable = GetComponent<Reservable>();
            _blockObject = GetComponent<BlockObject>();
        }

        public void Reserve() => _reservable.Reserve();

        public void Unreserve() {
            if (_reservable.Reserved)
                _reservable.Unreserve();
        }

        public void Mutate() {
            IsMutated = true;
            _reservable.Unreserve();
            if (_blockObject != null)
                _mutationArea.RemoveCoordinate(_blockObject.Coordinates);
            ApplyTint();
        }

        private void ApplyTint() {
            foreach (var renderer in GameObject.GetComponentsInChildren<Renderer>()) {
                foreach (var mat in renderer.materials) {
                    if (mat.HasProperty(TintColorId))
                        mat.SetColor(TintColorId, MutationTint);
                    if (mat.HasProperty(TintEnabledId))
                        mat.SetFloat(TintEnabledId, 1f);
                }
            }
        }
    }
}
