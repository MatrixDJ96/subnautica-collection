using System.Collections.Generic;

namespace BetterMap.Components
{
    public class FakeStorage : UserStorage
    {
        private UserStorageUtils.AsyncOperation FakeAsyncOperation { get; } = new();
        private UserStorageUtils.QueryOperation FakeQueryOperation { get; } = new();
        private UserStorageUtils.SlotsOperation FakeSlotsOperation { get; } = new();
        private UserStorageUtils.LoadOperation FakeLoadOperation { get; } = new();
        private UserStorageUtils.SaveOperation FakeSaveOperation { get; } = new();
        private UserStorageUtils.CopyOperation FakeCopyOperation { get; } = new();

        public FakeStorage()
        {
            FakeAsyncOperation.SetComplete(UserStorageUtils.Result.NotFound, "Fake Message");
            FakeQueryOperation.SetComplete(UserStorageUtils.Result.NotFound, "Fake Message");
            FakeSlotsOperation.SetComplete(UserStorageUtils.Result.NotFound, "Fake Message");
            FakeLoadOperation.SetComplete(UserStorageUtils.Result.NotFound, "Fake Message");
            FakeSaveOperation.SetComplete(UserStorageUtils.Result.NotFound, "Fake Message");
            FakeCopyOperation.SetComplete(UserStorageUtils.Result.NotFound, "Fake Message");
        }

        public UserStorageUtils.AsyncOperation InitializeAsync() => FakeAsyncOperation;

        public UserStorageUtils.QueryOperation GetContainerNamesAsync() => FakeQueryOperation;

        public UserStorageUtils.AsyncOperation CreateContainerAsync(string containerName) => FakeAsyncOperation;

        public UserStorageUtils.AsyncOperation DeleteContainerAsync(string containerName) => FakeAsyncOperation;

        public UserStorageUtils.LoadOperation LoadFilesAsync(string containerName, List<string> fileNames) => FakeLoadOperation;

        public UserStorageUtils.SlotsOperation LoadSlotsAsync(List<string> containerNames, List<string> fileNames) => FakeSlotsOperation;

        public UserStorageUtils.SaveOperation SaveFilesAsync(string containerName, Dictionary<string, byte[]> files) => FakeSaveOperation;

        public UserStorageUtils.AsyncOperation DeleteFilesAsync(string containerName, List<string> filePaths) => FakeAsyncOperation;

        public UserStorageUtils.CopyOperation CopyFilesFromContainerAsync(string containerName, string destPath) => FakeCopyOperation;

        public UserStorageUtils.SaveOperation CopyFilesToContainerAsync(string containerName, string srcPath, List<string> updatedFiles, List<string> deletedFiles, List<string> unchangedFiles) => FakeSaveOperation;
    }
}
