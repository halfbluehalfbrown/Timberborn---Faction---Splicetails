using Timberborn.SingletonSystem;
using UnityEngine;

namespace Timberborn.Splicetails
{
    public class SplicetailsInitializer : ILoadableSingleton
    {
        public void Load()
        {
            Debug.Log("[Splicetails] Faction initialized.");
        }
    }
}
