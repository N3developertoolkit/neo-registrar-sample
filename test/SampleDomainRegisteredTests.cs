using System.Linq;
using FluentAssertions;
using Neo.Assertions;
using Neo.BlockchainToolkit.SmartContract;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using NeoTestHarness;
using Xunit;

using static DevHawk.RegistrarTests.Common;

namespace DevHawk.RegistrarTests
{
    [CheckpointPath("checkpoints/sample-domain-registered.neoxp-checkpoint")]
    public class SampleDomainRegisteredTests : IClassFixture<CheckpointFixture<SampleDomainRegisteredTests>>
    {
        readonly CheckpointFixture fixture;

        public SampleDomainRegisteredTests(CheckpointFixture<SampleDomainRegisteredTests> fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public void Fail_to_register_existing_domain()
        {
            using var store = fixture.GetCheckpointStore();
            using var snapshot = new SnapshotCache(store);

            var storages = snapshot.GetContractStorages<Registrar>();
            storages.TryGetValue(DOMAIN_NAME_BYTES, out var item).Should().BeTrue();
            item!.Should().Be(BOB);

            using var engine = new TestApplicationEngine(snapshot, ALICE);
            using var monitor = engine.Monitor();
            engine.ExecuteScript<Registrar>(c => c.register(DOMAIN_NAME, ALICE));
            monitor.Should().Raise("Log")
                .WithSender(engine)
                .WithArgs<LogEventArgs>(args => args.Message == "Domain already registered");

            engine.State.Should().Be(VMState.HALT);
            engine.ResultStack.Should().HaveCount(1);
            engine.ResultStack.Peek(0).Should().BeFalse();
        }

        [Fact]
        public void Can_delete_existing_domain()
        {
            using var store = fixture.GetCheckpointStore();
            using var snapshot = new SnapshotCache(store);

            var storages = snapshot.GetContractStorages<Registrar>();
            storages.TryGetValue(DOMAIN_NAME_BYTES, out var item).Should().BeTrue();
            item!.Should().Be(BOB);

            using var engine = new TestApplicationEngine(snapshot, BOB);
            engine.ExecuteScript<Registrar>(c => c.delete(DOMAIN_NAME));

            engine.State.Should().Be(VMState.HALT);
            engine.ResultStack.Should().HaveCount(1);
            engine.ResultStack.Peek(0).Should().BeTrue();

            snapshot.GetContractStorages<Registrar>().Any().Should().BeFalse();
        }
    }
}

