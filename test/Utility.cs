using System;
using Neo.Persistence;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Neo;
using Neo.BlockchainToolkit.Persistence;
using Neo.SmartContract;
using System.Linq.Expressions;
using Neo.VM;
using Neo.Ledger;

namespace NeoTestHarness
{
    public static class Utility
    {
        public static void DeployNativeContracts(IStore store)
        {
            using var snapshot = new SnapshotView(store);
            if (snapshot.Contracts.Find().Any(c => c.Value.Id < 0)) return;

            using var sb = new Neo.VM.ScriptBuilder();
            sb.EmitSysCall(Neo.SmartContract.ApplicationEngine.Neo_Native_Deploy);

            using var engine = Neo.SmartContract.ApplicationEngine.Run(sb.ToArray(), snapshot, persistingBlock: new Neo.Network.P2P.Payloads.Block());
            if (engine.State != Neo.VM.VMState.HALT) throw new Exception("Neo_Native_Deploy failed");
            snapshot.Commit();
        }

        class FolderDisposer : IDisposable
        {
            readonly string pathToDelete;

            public FolderDisposer(string pathToDelete)
            {
                this.pathToDelete = pathToDelete;
            }

            public void Dispose()
            {
                if (Directory.Exists(pathToDelete)) Directory.Delete(pathToDelete, true);
            }
        }

        public static IStore OpenCheckpoint(string checkpoint)
        {
            string checkpointTempPath;
            do
            {
                checkpointTempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            }
            while (Directory.Exists(checkpointTempPath));

            var cleanup = new FolderDisposer(checkpointTempPath);

            var magic = RocksDbStore.RestoreCheckpoint(checkpoint, checkpointTempPath);
            var settings = new[] { KeyValuePair.Create("ProtocolConfiguration:Magic", $"{magic}") };
            var protocolConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            if (!ProtocolSettings.Initialize(protocolConfig))
            {
                throw new Exception("could not initialize protocol settings");
            }

            return new CheckpointStore(
                RocksDbStore.OpenReadOnly(checkpointTempPath),
                cleanup);
        }

        public static Script CreateScript<T>(this StoreView store, Expression<Action<T>> expression)
        {
            using var builder = new ScriptBuilder();
            builder.AddInvoke<T>(store, expression);
            return builder.ToArray();
        }

        public static void AddInvoke<T>(this ScriptBuilder builder, StoreView store, Expression<Action<T>> expression)
        {
            var methodCall = (MethodCallExpression)expression.Body;

            var scriptHash = store.GetContractAddress<T>();
            var operation = methodCall.Method.Name;

            for (var x = methodCall.Arguments.Count - 1; x >= 0; x--)
            {
                var obj = Expression.Lambda(methodCall.Arguments[x]).Compile().DynamicInvoke();
                builder.EmitPush(obj);
            }
            builder.EmitPush(methodCall.Arguments.Count);
            builder.Emit(OpCode.PACK);
            builder.EmitPush(operation);
            builder.EmitPush(scriptHash);
            builder.EmitSysCall(ApplicationEngine.System_Contract_Call);
        }

        public static UInt160 FromAddress(this string address)
        {
            return Neo.Wallets.Helper.ToScriptHash(address);
        }

        public static IEnumerable<(byte[] key, StorageItem item)> GetContractStorages<T>(this StoreView store)
        {
            var contract = store.GetContract<T>();
            var prefix = StorageKey.CreateSearchPrefix(contract.Id, default);
            return store.Storages.Find(prefix)
                .Select(s => (s.Key.Key, s.Value));
        }

        public static UInt160 GetContractAddress<T>(this StoreView store)
        {
            return store.GetContract<T>().Hash;
        }

        public static ContractState GetContract<T>(this StoreView store)
        {
            var typeName = typeof(T).FullName;
            foreach (var (key, value) in store.Contracts.Find())
            {
                var name = value.Id >= 0 ? value.Manifest.Name : "Neo.NativeContracts." + value.Manifest.Name;
                if (string.Equals(typeName, name))
                {
                    return value;
                }
            }

            throw new Exception($"couldn't find {typeName} contract");
        }

        public static VMState AssertExecute(this Neo.SmartContract.ApplicationEngine engine)
        {
            var state = engine.Execute();

            if (state != Neo.VM.VMState.HALT)
            {
                if (engine.FaultException != null)
                    throw engine.FaultException;
                else
                    throw new Xunit.Sdk.EqualException(Neo.VM.VMState.HALT, state);
            }

            return state;
        }
    }
}
