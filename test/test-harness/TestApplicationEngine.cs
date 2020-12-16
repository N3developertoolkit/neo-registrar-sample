using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Neo;
using Neo.Cryptography;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;

namespace NeoTestHarness
{
    using WitnessChecker = Func<byte[], bool>;
    using ServiceMethod = Func<TestApplicationEngine, IReadOnlyList<InteropParameterDescriptor>, Neo.VM.Types.StackItem?>;
    using StackItem = Neo.VM.Types.StackItem;

    public class TestApplicationEngine : Neo.SmartContract.ApplicationEngine
    {
        private readonly static IReadOnlyDictionary<uint, ServiceMethod> overriddenServices;

        static TestApplicationEngine()
        {
            var builder = ImmutableDictionary.CreateBuilder<uint, ServiceMethod>();
            builder.Add(HashMethodName("System.Runtime.CheckWitness"), CheckWitnessOverride);
            builder.Add(HashMethodName("System.Blockchain.GetBlock"), GetBlockOverride);
            builder.Add(HashMethodName("System.Blockchain.GetTransactionFromBlock"), GetTransactionFromBlockOverride);
            overriddenServices = builder.ToImmutable();

            uint HashMethodName(string name)
            {
                return BitConverter.ToUInt32(System.Text.Encoding.ASCII.GetBytes(name).Sha256(), 0);
            }
        }

        private readonly WitnessChecker witnessChecker;
        private readonly Lazy<IReadOnlyDictionary<uint, UInt256>> blockIndexMap;

        public TestApplicationEngine(StoreView snapshot) : this(TriggerType.Application, null, snapshot, ApplicationEngine.TestModeGas, _ => true)
        {
        }

        public TestApplicationEngine(TriggerType trigger, IVerifiable? container, StoreView snapshot, long gas, WitnessChecker? witnessChecker)
            : base(trigger, container, snapshot, gas)
        {
            this.witnessChecker = witnessChecker ?? CheckWitness;
            this.blockIndexMap = new Lazy<IReadOnlyDictionary<uint, UInt256>>(() =>
                snapshot.Blocks.Find().ToDictionary(t => t.Value.Index, t => t.Value.Hash));

            ApplicationEngine.Log += OnLog;
            ApplicationEngine.Notify += OnNotify;
        }

        public override void Dispose()
        {
            ApplicationEngine.Log -= OnLog;
            ApplicationEngine.Notify -= OnNotify;
            base.Dispose();
        }

        public new event EventHandler<LogEventArgs>? Log;
        public new event EventHandler<NotifyEventArgs>? Notify;

        private void OnLog(object? sender, LogEventArgs args)
        {
            if (ReferenceEquals(this, sender))
            {
                this.Log?.Invoke(sender, args);
            }
        }

        private void OnNotify(object? sender, NotifyEventArgs args)
        {
            if (ReferenceEquals(this, sender))
            {
                this.Notify?.Invoke(sender, args);
            }
        }

        private static StackItem CheckWitnessOverride(
            TestApplicationEngine engine,
            IReadOnlyList<InteropParameterDescriptor> paramDescriptors)
        {

            Debug.Assert(paramDescriptors.Count == 1);
            var hashOrPubkey = (byte[])engine.Convert(engine.Pop(), paramDescriptors[0]);

            return engine.witnessChecker.Invoke(hashOrPubkey);
        }

        private static StackItem GetBlockOverride(
            TestApplicationEngine engine,
            IReadOnlyList<InteropParameterDescriptor> paramDescriptors)
        {
            Debug.Assert(paramDescriptors.Count == 1);
            var indexOrHash = (byte[])engine.Convert(engine.Pop(), paramDescriptors[0]);

            var hash = engine.GetBlockHash(indexOrHash);
            if (hash is null) return StackItem.Null;
            Block block = engine.Snapshot.GetBlock(hash);
            if (block is null) return StackItem.Null;
            return block.ToStackItem(engine.ReferenceCounter);
        }

        private static StackItem GetTransactionFromBlockOverride(
            TestApplicationEngine engine,
            IReadOnlyList<InteropParameterDescriptor> paramDescriptors)
        {
            Debug.Assert(paramDescriptors.Count == 2);
            var blockIndexOrHash = (byte[])engine.Convert(engine.Pop(), paramDescriptors[0]);
            var txIndex = (int)engine.Convert(engine.Pop(), paramDescriptors[1]);

            var hash = engine.GetBlockHash(blockIndexOrHash);
            if (hash is null) return StackItem.Null;
            var block = engine.Snapshot.Blocks.TryGet(hash);
            if (block is null) return StackItem.Null;
            if (txIndex < 0 || txIndex >= block.Hashes.Length - 1)
                throw new ArgumentOutOfRangeException(nameof(txIndex));
            return engine.Snapshot.GetTransaction(block.Hashes[txIndex + 1])
                .ToStackItem(engine.ReferenceCounter);
        }

        readonly Lazy<System.Reflection.MethodInfo> interopDescriptorGetHandler = new Lazy<System.Reflection.MethodInfo>(() => typeof(InteropDescriptor)
            .GetProperty("Handler", System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.NonPublic)?
            .GetMethod ?? throw new Exception());

        protected override void OnSysCall(uint methodHash)
        {
            if (overriddenServices.TryGetValue(methodHash, out var method))
            {
                InteropDescriptor descriptor = Services[methodHash];
                ValidateCallFlags(descriptor);
                AddGas(descriptor.FixedPrice);

                var result = method(this, descriptor.Parameters);
                if (result != null)
                {
                    Push(result);
                }
            }
            else
            {
                base.OnSysCall(methodHash);
            }
        }

        private UInt256? GetBlockHash(byte[] indexOrHash)
        {
            if (indexOrHash.Length == UInt256.Length)
            {
                return new UInt256(indexOrHash);
            }

            if (indexOrHash.Length < UInt256.Length)
            {
                var index = new BigInteger(indexOrHash);
                if (index < uint.MinValue || index > uint.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(indexOrHash));
                if (blockIndexMap.Value.TryGetValue((uint)index, out var hash))
                {
                    return hash;
                }
            }

            return null;
        }
    }
}
