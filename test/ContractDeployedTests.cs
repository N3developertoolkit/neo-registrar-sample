using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Neo.Assertions;
using Neo.BlockchainToolkit;
using Neo.BlockchainToolkit.Models;
using Neo.BlockchainToolkit.SmartContract;
using Neo.VM;
using Neo.Wallets;
using NeoTestHarness;
using Xunit;

using static DevHawk.RegistrarTests.Common;

namespace DevHawk.RegistrarTests
{
    [CheckpointPath("checkpoints/contract-deployed.neoxp-checkpoint")]
    public class ContractDeployedTests : IClassFixture<CheckpointFixture<ContractDeployedTests>>
    {
        readonly CheckpointFixture fixture;
        readonly ExpressChain chain;

        public ContractDeployedTests(CheckpointFixture<ContractDeployedTests> fixture)
        {
            this.fixture = fixture;
            this.chain = fixture.FindChain();
        }

        [Fact]
        public void Can_register_domain()
        {
            var settings = chain.GetProtocolSettings();
            var alice = chain.GetDefaultAccount("alice").ToScriptHash(settings.AddressVersion);

            using var snapshot = fixture.GetSnapshot();

            // pretest check to ensure storage is empty as expected
            snapshot.GetContractStorages<Registrar>().Any().Should().BeFalse();

            // ExecuteScript converts the provided expression(s) into a Neo script
            // loads them into the engine and executes it 
            using var engine = new TestApplicationEngine(snapshot, settings, alice);

            var logs = new List<string>();
            engine.Log += (sender, args) =>
            {
                logs.Add(args.Message);
            };

            engine.ExecuteScript<Registrar>(c => c.register(DOMAIN_NAME, alice));

            engine.State.Should().Be(VMState.HALT);
            engine.ResultStack.Should().HaveCount(1);
            engine.ResultStack.Peek(0).Should().BeTrue();

            // ensure correct storage item was created 
            var storages = snapshot.GetContractStorages<Registrar>();
            storages.TryGetValue(DOMAIN_NAME_BYTES, out var item).Should().BeTrue();
            item!.Should().Be(alice);
        }
    }
}

