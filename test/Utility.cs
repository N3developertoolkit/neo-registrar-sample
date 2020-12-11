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
using Neo.SmartContract.Native;

namespace NeoTestHarness
{
    public static class Utility
    {
        // TODO: replace with ManagementContract.ListContracts when https://github.com/neo-project/neo/pull/2134 is merged
        public static IEnumerable<ContractState> ListContracts(StoreView snapshot)
        {
            const byte Prefix_Contract = 8;
            var key = new KeyBuilder(NativeContract.Management.Id, Prefix_Contract);
            byte[] listContractsPrefix = key.ToArray();
            return snapshot.Storages.Find(listContractsPrefix).Select(kvp => kvp.Value.GetInteroperable<ContractState>());
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

        static long initMagic = -1;
        public static void InitializeProtocolSettings(long magic)
        {
            if (initMagic < 0)
            {
                var settings = new[] { KeyValuePair.Create("ProtocolConfiguration:Magic", $"{magic}") };
                var protocolConfig = new ConfigurationBuilder()
                    .AddInMemoryCollection(settings)
                    .Build();

                if (!ProtocolSettings.Initialize(protocolConfig))
                {
                    throw new Exception("could not initialize protocol settings");
                }
                initMagic = magic;
            }
            else
            {
                if (magic != initMagic)
                {
                    throw new Exception($"ProtocolSettings already initialized with {initMagic}");
                }
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
            InitializeProtocolSettings(magic);

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
            foreach (var contractState in Utility.ListContracts(store))
            {
                var name = contractState.Id >= 0 ? contractState.Manifest.Name : "Neo.SmartContract.Native." + contractState.Manifest.Name;
                if (string.Equals(typeName, name))
                {
                    return contractState;
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
