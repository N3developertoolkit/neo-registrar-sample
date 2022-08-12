using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Neo;
using Neo.Network.RPC;
using Neo.SmartContract.Manifest;

namespace DevHawk.Registrar.Cli
{
    static class Extensions
    {
        public static async Task<IReadOnlyList<(UInt160 hash, ContractManifest manifest)>> ListContractsAsync(this RpcClient rpcClient)
        {
            var json = await rpcClient.RpcSendAsync("expresslistcontracts").ConfigureAwait(false);

            if (json != null && json is Neo.Json.JArray array)
            {
                return array
                    .Select(j => (
                        UInt160.Parse(j!["hash"]!.AsString()),
                        ContractManifest.FromJson((Neo.Json.JObject)j!["manifest"]!)))
                    .ToList();
            }

            return Array.Empty<(UInt160 hash, ContractManifest manifest)>();
        }
    }
}
