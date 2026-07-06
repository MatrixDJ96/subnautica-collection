namespace BetterVehicles.MonoBehaviours
{
    public class ExosuitStorageController : AbstractVehicleStorageController
    {
        public override IItemsContainer[] GetDefaultStorage()
        {
            return new[] { (component as Exosuit).storageContainer.container };
        }

        public override IItemsContainer[] GetTorpedoStorage()
        {
            return GetStorageContainers(TechType.ExosuitTorpedoArmModule);
        }

        public override IItemsContainer[] GetVehicleStorage()
        {
            // Default storage is already "merged" with extra storages, so return default
            return GetDefaultStorage();
        }
    }
}
