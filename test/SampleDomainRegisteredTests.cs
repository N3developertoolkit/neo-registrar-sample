using System.Linq;
using Neo;
using Neo.Persistence;
using NeoTestHarness;
using Xunit;

using static DevHawk.RegistrarTests.Common;

namespace DevHawk.RegistrarTests
{

    public class SampleDomainRegisteredTests : IClassFixture<SampleDomainRegisteredTests.Fixture>
    {
        public class Fixture : CheckpointFixture
        {
            const string PATH = @"../../../../checkpoints/sample-domain-registered.nxp3-checkpoint";
            public Fixture() : base(PATH) { }
        }

        readonly Fixture fixture;

        public SampleDomainRegisteredTests(Fixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void Fail_to_register_existing_domain()
        {
            using var store = fixture.GetCheckpointStore();
            using var snapshot = new SnapshotView(store);

            var storageItem = snapshot.GetContractStorageItem<DevHawk.Registrar>(DOMAIN_NAME_BYTES);
            Assert.Equal(BOB, new UInt160(storageItem.Value));

            var script = snapshot.CreateScript<DevHawk.Registrar>(c => c.register(DOMAIN_NAME, ALICE));

            using var engine = new TestApplicationEngine(snapshot);
            engine.LoadScript(script);
            engine.AssertExecute();

            Assert.Single(engine.ResultStack);
            Assert.False(engine.ResultStack.Pop().GetBoolean());
        }

        [Fact]
        public void Can_delete_existing_domain()
        {
            using var store = fixture.GetCheckpointStore();
            using var snapshot = new SnapshotView(store);

            var storageItem = snapshot.GetContractStorageItem<DevHawk.Registrar>(DOMAIN_NAME_BYTES);
            Assert.Equal(BOB, new UInt160(storageItem.Value));

            var script = snapshot.CreateScript<DevHawk.Registrar>(c => c.delete(DOMAIN_NAME));

            using var engine = new TestApplicationEngine(snapshot);
            engine.LoadScript(script);
            engine.AssertExecute();

            Assert.Single(engine.ResultStack);
            Assert.True(engine.ResultStack.Pop().GetBoolean());

            Assert.False(snapshot.GetContractStorages<DevHawk.Registrar>().Any());
        }
    }
}

