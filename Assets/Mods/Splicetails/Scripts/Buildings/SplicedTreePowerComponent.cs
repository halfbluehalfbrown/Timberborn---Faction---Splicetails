using System.Reflection;
using Timberborn.MechanicalSystem;
using UnityEngine;

namespace Timberborn.Splicetails
{
    // Disables mechanical power output when the Spliced Tree is not alive and mature.
    // Uses reflection to call SetPowerOutput because the publicizer does not expose
    // that method at compile time.
    public class SplicedTreePowerComponent : MonoBehaviour
    {
        private MechanicalBuilding _mechanicalBuilding;
        private MethodInfo _setPowerOutput;
        private PropertyInfo _powerOutput;
        private Transform _matureAlive;
        private float _basePowerOutput;
        private bool _wasActive;

        private void Start()
        {
            _mechanicalBuilding = GetComponent<MechanicalBuilding>();
            _matureAlive = transform.Find("#Models/Mature/#Alive");

            if (_mechanicalBuilding != null)
            {
                var type = typeof(MechanicalBuilding);
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                _setPowerOutput = type.GetMethod("SetPowerOutput", flags);
                _powerOutput = type.GetProperty("PowerOutput", flags);

                if (_powerOutput != null)
                    _basePowerOutput = (float)(_powerOutput.GetValue(_mechanicalBuilding) ?? 0f);
            }
        }

        private void Update()
        {
            if (_mechanicalBuilding == null || _setPowerOutput == null) return;

            bool isActive = _matureAlive != null && _matureAlive.gameObject.activeInHierarchy;
            if (isActive == _wasActive) return;

            _wasActive = isActive;
            _setPowerOutput.Invoke(_mechanicalBuilding, new object[] { isActive ? _basePowerOutput : 0f });
        }
    }
}
