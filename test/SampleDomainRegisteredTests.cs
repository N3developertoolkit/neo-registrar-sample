using System.Linq;
using FluentAssertions;
using Neo.Assertions;
using Neo.BlockchainToolkit;
using Neo.BlockchainToolkit.Models;
using Neo.BlockchainToolkit.SmartContract;
using Neo.SmartContract;
using Neo.VM;
using NeoTestHarness;
using Xunit;
using Xunit.Abstractions;
using static DevHawk.RegistrarTests.Common;

namespace DevHawk.RegistrarTests
{
    [CheckpointPath("checkpoints/sample-domain-registered.neoxp-checkpoint")]
    public class SampleDomainRegisteredTests : IClassFixture<CheckpointFixture<SampleDomainRegisteredTests>>
    {
        readonly CheckpointFixture fixture;
        readonly ExpressChain chain;
        readonly ITestOutputHelper output;


        public SampleDomainRegisteredTests(CheckpointFixture<SampleDomainRegisteredTests> fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;
            this.chain = fixture.FindChain();
            this.output = output;
        }

        [Fact]
        public void Fail_to_register_existing_domain()
        {
            var settings = chain.GetProtocolSettings();
            var alice = chain.GetDefaultAccount("alice").ToScriptHash(settings.AddressVersion);
            var bob = chain.GetDefaultAccount("bob").ToScriptHash(settings.AddressVersion);

            using var snapshot = fixture.GetSnapshot();
 
            var domainOwners = snapshot.GetContractStorages<Registrar>().StorageMap(DOMAIN_OWNERS_PREFIX);
            domainOwners.TryGetValue(DOMAIN_NAME, out var item).Should().BeTrue();
            item!.Should().Be(bob);

            using var engine = new TestApplicationEngine(snapshot, settings, alice);
            using var monitor = engine.Monitor();
            engine.ExecuteScript<Registrar>(c => c.register(DOMAIN_NAME, alice));

            if (engine.State != VMState.HALT)
            {
                output.WriteLine($"FaultException: {engine.FaultException}");
            }

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
            var settings = chain.GetProtocolSettings();
            var bob = chain.GetDefaultAccount("bob").ToScriptHash(settings.AddressVersion);

            using var snapshot = fixture.GetSnapshot();

            var domainOwners = snapshot.GetContractStorages<Registrar>().StorageMap(0x00);
            domainOwners.TryGetValue(DOMAIN_NAME, out var item).Should().BeTrue();
            item!.Should().Be(bob);

            using var engine = new TestApplicationEngine(snapshot, settings, bob);
            engine.ExecuteScript<Registrar>(c => c.delete(DOMAIN_NAME));

            if (engine.State != VMState.HALT)
            {
                output.WriteLine($"FaultException: {engine.FaultException}");
            }

            engine.State.Should().Be(VMState.HALT);
            engine.ResultStack.Should().HaveCount(1);
            engine.ResultStack.Peek(0).Should().BeTrue();

            domainOwners = snapshot.GetContractStorages<Registrar>().StorageMap(DOMAIN_OWNERS_PREFIX);
            domainOwners.TryGetValue(DOMAIN_NAME, out _).Should().BeFalse();
        }
    }
}

