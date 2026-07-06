#if SUBNAUTICA
using System;

namespace BetterVehicles.MonoBehaviours
{
    public class SeamothStorageController : AbstractVehicleStorageController
    {
        public override IItemsContainer[] GetDefaultStorage()
        {
            return Array.Empty<IItemsContainer>();
        }

        public override IItemsContainer[] GetTorpedoStorage()
        {
            return GetStorageContainers(TechType.SeamothTorpedoModule);
        }
    }
}
#endif
