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
        // Fixture is used to share checkpoint across multiple tests
        public class Fixture : CheckpointFixture
        {
            const string PATH = "checkpoints/contract-deployed.nxp3-checkpoint";
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

            // pretest check to ensure storage is empty as expected
            Assert.False(snapshot.GetContractStorages<Registrar>().Any());

            // AssertScript converts the provided expression(s) into a Neo script
            // loads them into the engine, executes it and asserts the results
            using var engine = new TestApplicationEngine(snapshot);
            engine.AssertScript<Registrar>(c => c.register(DOMAIN_NAME, ALICE));

            // check the execution results one at a time
            Assert.True(engine.ResultStack.Pop().GetBoolean());
            // Ensure there are no more results than expected
            Assert.Empty(engine.ResultStack);

            // ensure correct storage item was created 
            var storageItem = snapshot.GetContractStorageItem<Registrar>(DOMAIN_NAME_BYTES);
            Assert.Equal(ALICE, new UInt160(storageItem.Value));
        }
    }
}

