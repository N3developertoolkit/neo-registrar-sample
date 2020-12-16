using System.Linq;
using Neo;
using Neo.Persistence;
using NeoTestHarness;
using Xunit;

using static DevHawk.RegistrarTests.Common;

namespace DevHawk.RegistrarTests
{

    public class ContractDeployedTests : IClassFixture<ContractDeployedTests.Fixture>
    {
        public class Fixture : CheckpointFixture
        {
            const string PATH = @"../../../../checkpoints/contract-deployed.nxp3-checkpoint";
            public Fixture() : base(PATH) { }
        }

        readonly Fixture fixture;

        public ContractDeployedTests(Fixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void Can_register_domain()
        {
            using var store = fixture.GetCheckpointStore();
            using var snapshot = new SnapshotView(store);

            Assert.False(snapshot.GetContractStorages<DevHawk.Registrar>().Any());

            var script = snapshot.CreateScript<DevHawk.Registrar>(c => c.register(DOMAIN_NAME, ALICE));

            using var engine = new TestApplicationEngine(snapshot);
            engine.LoadScript(script);
            engine.AssertExecute();

            Assert.Single(engine.ResultStack);
            Assert.True(engine.ResultStack.Pop().GetBoolean());

            var storageItem = snapshot.GetContractStorageItem<DevHawk.Registrar>(DOMAIN_NAME_BYTES);
            Assert.Equal(ALICE, new UInt160(storageItem.Value));
        }
    }
}

