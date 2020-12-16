using System;
using System.IO;
using Neo.BlockchainToolkit.Persistence;

namespace NeoTestHarness
{
    public class CheckpointFixture : IDisposable
    {
        string checkpointTempPath;
        RocksDbStore rocksDbStore;

        protected CheckpointFixture(string checkpointPath)
        {
            do
            {
                checkpointTempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            }
            while (Directory.Exists(checkpointTempPath));

            var magic = RocksDbStore.RestoreCheckpoint(checkpointPath, checkpointTempPath);
            Utility.InitializeProtocolSettings(magic);

            rocksDbStore = RocksDbStore.OpenReadOnly(checkpointTempPath);
        }

        public CheckpointStore GetCheckpointStore()
        {
            return new CheckpointStore(rocksDbStore, false);
        }

        public void Dispose()
        {
            rocksDbStore.Dispose();
            if (Directory.Exists(checkpointTempPath)) Directory.Delete(checkpointTempPath, true);
        }
    }
}
