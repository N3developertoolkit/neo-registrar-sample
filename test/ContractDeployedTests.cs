using System.Linq;
using FluentAssertions;
using Neo.Assertions;
using Neo.Persistence;
using Neo.VM;
using NeoTestHarness;
using Xunit;

using static DevHawk.RegistrarTests.Common;

namespace DevHawk.RegistrarTests
{
    [CheckpointPath("checkpoints/contract-deployed.nxp3-checkpoint")]
    public class ContractDeployedTests : IClassFixture<CheckpointFixture<ContractDeployedTests>>
    {
        readonly CheckpointFixture fixture;

        public ContractDeployedTests(CheckpointFixture<ContractDeployedTests> fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void Can_register_domain()
        {
            using var store = fixture.GetCheckpointStore();
            using var snapshot = new SnapshotCache(store);

            // pretest check to ensure storage is empty as expected
            snapshot.GetContractStorages<Registrar>().Any().Should().BeFalse();

            // ExecuteScript converts the provided expression(s) into a Neo script
            // loads them into the engine and executes it 
            using var engine = new TestApplicationEngine(snapshot, ALICE);
            engine.ExecuteScript<Registrar>(c => c.register(DOMAIN_NAME, ALICE));

            engine.State.Should().Be(VMState.HALT);
            engine.ResultStack.Should().HaveCount(1);
            engine.ResultStack.Peek(0).Should().BeTrue();

            // ensure correct storage item was created 
            var storages = snapshot.GetContractStorages<Registrar>();
            storages.TryGetValue(DOMAIN_NAME_BYTES, out var item).Should().BeTrue();
            item!.Should().Be(ALICE);
        }
    }
}

