using System;
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
using Xunit.Abstractions;
using static DevHawk.RegistrarTests.Common;

namespace DevHawk.RegistrarTests
{
    [CheckpointPath("checkpoints/contract-deployed.neoxp-checkpoint")]
    public class ContractDeployedTests : IClassFixture<CheckpointFixture<ContractDeployedTests>>
    {
        readonly CheckpointFixture fixture;
        readonly ExpressChain chain;
        readonly ITestOutputHelper output;

        const string envName = "NEO_TEST_APP_ENGINE_COVERAGE_PATH";

        public ContractDeployedTests(CheckpointFixture<ContractDeployedTests> fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;
            this.chain = fixture.FindChain();
            this.output = output;
        }

        [Fact]
        public void get_coverage_path()
        {
            var coveragePath = Environment.GetEnvironmentVariable(envName)
                ?? "<not found>";
            output.WriteLine($"CoveragePath: {coveragePath}");
        }

        [Fact]
        public void contract_owner_in_storage()
        {
            var settings = chain.GetProtocolSettings();
            var owen = chain.GetDefaultAccount("owen").ToScriptHash(settings.AddressVersion);

            using var snapshot = fixture.GetSnapshot();

            // check to make sure contract owner stored in contract storage
            var storages = snapshot.GetContractStorages<SampleRegistrar>();
            storages.Count().Should().Be(1);
            storages.TryGetValue(CONTRACT_OWNER_PREFIX, out var item).Should().BeTrue();
            item!.Should().Be(owen);
        }

        [Fact]
        public void Can_register_domain()
        {
            var settings = chain.GetProtocolSettings();
            var alice = chain.GetDefaultAccount("alice").ToScriptHash(settings.AddressVersion);
            var owen = chain.GetDefaultAccount("owen").ToScriptHash(settings.AddressVersion);

            using var snapshot = fixture.GetSnapshot();

            // ExecuteScript converts the provided expression(s) into a Neo script
            // loads them into the engine and executes it 
            using var engine = new TestApplicationEngine(snapshot, settings, alice);

            var logs = new List<string>();
            engine.Log += (sender, args) =>
            {
                logs.Add(args.Message);
            };

            engine.ExecuteScript<SampleRegistrar>(c => c.register(DOMAIN_NAME, alice));

            if (engine.State != VMState.HALT)
            {
                output.WriteLine($"FaultException: {engine.FaultException}");
            }
            engine.State.Should().Be(VMState.HALT);
            engine.ResultStack.Should().HaveCount(1);
            engine.ResultStack.Peek(0).Should().BeTrue();

            // ensure correct storage item was created 
            var storages = snapshot.GetContractStorages<SampleRegistrar>();
            var domainOwners = storages.StorageMap(DOMAIN_OWNERS_PREFIX);
            domainOwners.TryGetValue(DOMAIN_NAME, out var item).Should().BeTrue();
            item!.Should().Be(alice);
        }
    }
}

