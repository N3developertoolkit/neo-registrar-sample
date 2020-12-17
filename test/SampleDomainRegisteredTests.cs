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
        // Fixture is used to share checkpoint across multiple tests
        public class Fixture : CheckpointFixture
        {
            const string PATH = "checkpoints/sample-domain-registered.nxp3-checkpoint";
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

            var storageItem = snapshot.GetContractStorageItem<Registrar>(DOMAIN_NAME_BYTES);
            Assert.Equal(BOB, new UInt160(storageItem.Value));

            using var engine = new TestApplicationEngine(snapshot);
            engine.AssertScript<Registrar>(c => c.register(DOMAIN_NAME, ALICE));

            Assert.False(engine.ResultStack.Pop().GetBoolean());
            Assert.Empty(engine.ResultStack);
        }

        [Fact]
        public void Can_delete_existing_domain()
        {
            using var store = fixture.GetCheckpointStore();
            using var snapshot = new SnapshotView(store);

            var storageItem = snapshot.GetContractStorageItem<Registrar>(DOMAIN_NAME_BYTES);
            Assert.Equal(BOB, new UInt160(storageItem.Value));

            using var engine = new TestApplicationEngine(snapshot);
            engine.AssertScript<Registrar>(c => c.delete(DOMAIN_NAME));

            Assert.True(engine.ResultStack.Pop().GetBoolean());
            Assert.Empty(engine.ResultStack);

            Assert.False(snapshot.GetContractStorages<Registrar>().Any());
        }
    }
}

