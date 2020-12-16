using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Neo;
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
            InitializeProtocolSettings(magic);

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

        static long initMagic = -1;
        static void InitializeProtocolSettings(long magic)
        {
            if (initMagic < 0)
            {
                var settings = new[] { KeyValuePair.Create("ProtocolConfiguration:Magic", $"{magic}") };
                var protocolConfig = new ConfigurationBuilder()
                    .AddInMemoryCollection(settings)
                    .Build();

                if (!ProtocolSettings.Initialize(protocolConfig))
                {
                    throw new Exception("could not initialize protocol settings");
                }
                initMagic = magic;
            }
            else
            {
                if (magic != initMagic)
                {
                    throw new Exception($"ProtocolSettings already initialized with {initMagic}");
                }
            }
        }
    }
}
