using System;
using Neo;
using Xunit;

namespace DevHawk
{
    // this interface will eventually be generated from contract manifest ABI
    interface Registrar
    {
        UInt160 query(string domain);
        bool register(string domain, UInt160 owner);
        bool delete(string domain);
    }
}

namespace DevHawkTest.Contracts
{
    using System.IO;
    using System.Linq;
    using Moq;
    using Neo.BlockchainToolkit.Persistence;
    using Neo.Persistence;
    using Neo.SmartContract;
    using NeoTestHarness;

    public class SampleDomainRegisteredFixture : IDisposable
    {
        const string PATH = @"C:\Users\harry\Source\neo\seattle\samples\registrar-sample\checkpoints\sample-domain-registered.nxp3-checkpoint";
        string checkpointTempPath;
        RocksDbStore rocksDbStore;

        public SampleDomainRegisteredFixture()
        {
            do
            {
                checkpointTempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            }
            while (Directory.Exists(checkpointTempPath));

            var magic = RocksDbStore.RestoreCheckpoint(PATH, checkpointTempPath);
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

    public class RegistrarTests
    {
        [Fact]
        public void Test1()
        {
            const string DOMAIN_NAME = "sample.domain";
            var DOMAIN_NAME_BYTES = Neo.Utility.StrictUTF8.GetBytes(DOMAIN_NAME);

            using var store = Utility.OpenCheckpoint(@"C:\Users\harry\Source\neo\seattle\samples\registrar-sample\checkpoints\contract-deployed.nxp3-checkpoint");
            using var snapshot = new SnapshotView(store);

            Assert.False(snapshot.GetContractStorages<DevHawk.Registrar>().Any());

            var alice = "NhGxW6BtLRhFLqh2oWqeRpNj8aNzKybRoV".FromAddress();
            var script = snapshot.CreateScript<DevHawk.Registrar>(c => c.register(DOMAIN_NAME, alice));

            using var engine = new TestApplicationEngine(TriggerType.Application, null, snapshot, ApplicationEngine.TestModeGas, _ => true);
            engine.Log += (s, a) => Console.WriteLine(a.Message);
            engine.LoadScript(script);
            engine.AssertExecute();

            Assert.Single(engine.ResultStack);
            Assert.True(engine.ResultStack.Pop().GetBoolean());

            var storage = snapshot.GetContractStorages<DevHawk.Registrar>().Single(s => s.key.SequenceEqual(DOMAIN_NAME_BYTES));
            Assert.Equal(alice, new UInt160(storage.item.Value));
        }
    }

    public class SampleDomainRegisteredTests : IClassFixture<SampleDomainRegisteredFixture>
    {
        const string DOMAIN_NAME = "sample.domain";
        readonly byte[] DOMAIN_NAME_BYTES = Neo.Utility.StrictUTF8.GetBytes(DOMAIN_NAME);
        readonly SampleDomainRegisteredFixture fixture;

        public SampleDomainRegisteredTests(SampleDomainRegisteredFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void Test2()
        {
            using var store = fixture.GetCheckpointStore();
            using var snapshot = new SnapshotView(store);

            var bob = "NXZQqdnqQKFxQfgMVwmV59yU8tf1P28tEM".FromAddress();
            var storage = snapshot.GetContractStorages<DevHawk.Registrar>().Single(s => s.key.SequenceEqual(DOMAIN_NAME_BYTES));
            Assert.Equal(bob, new UInt160(storage.item.Value));

            var alice = "NhGxW6BtLRhFLqh2oWqeRpNj8aNzKybRoV".FromAddress();
            var script = snapshot.CreateScript<DevHawk.Registrar>(c => c.register(DOMAIN_NAME, alice));

            using var engine = new TestApplicationEngine(TriggerType.Application, null, snapshot, ApplicationEngine.TestModeGas, _ => true);
            engine.Log += (s, a) => Console.WriteLine(a.Message);
            engine.LoadScript(script);
            engine.AssertExecute();

            Assert.Single(engine.ResultStack);
            Assert.False(engine.ResultStack.Pop().GetBoolean());
        }

        [Fact]
        public void Test3()
        {
            using var store = fixture.GetCheckpointStore();
            using var snapshot = new SnapshotView(store);

            var bob = "NXZQqdnqQKFxQfgMVwmV59yU8tf1P28tEM".FromAddress();
            var storage = snapshot.GetContractStorages<DevHawk.Registrar>().Single(s => s.key.SequenceEqual(DOMAIN_NAME_BYTES));
            Assert.Equal(bob, new UInt160(storage.item.Value));

            var script = snapshot.CreateScript<DevHawk.Registrar>(c => c.delete(DOMAIN_NAME));

            using var engine = new TestApplicationEngine(TriggerType.Application, null, snapshot, ApplicationEngine.TestModeGas, _ => true);
            engine.Log += (s, a) => Console.WriteLine(a.Message);
            engine.LoadScript(script);
            engine.AssertExecute();

            Assert.Single(engine.ResultStack);
            Assert.True(engine.ResultStack.Pop().GetBoolean());

            Assert.False(snapshot.GetContractStorages<DevHawk.Registrar>().Any());
        }
    }
}

