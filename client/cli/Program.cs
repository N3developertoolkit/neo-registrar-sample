using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Neo;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;

namespace DevHawk.Registrar.Cli
{
    class Program
    {
        const ushort RpcPort = 50012;

        readonly ProtocolSettings settings;
        readonly UInt160 contractHash;

        public Program(ProtocolSettings settings, UInt160 contractHash)
        {
            this.settings = settings;
            this.contractHash = contractHash;
        }

        static async Task Main(string[] args)
        {
            var rpcClient = new RpcClient(new Uri($"http://localhost:{RpcPort}"));

            var version = await rpcClient.GetVersionAsync().ConfigureAwait(false);
            var settings = ProtocolSettings.Default with
            {
                Network = version.Protocol.Network,
                AddressVersion = version.Protocol.AddressVersion
            };

            var contracts = await rpcClient.ListContractsAsync().ConfigureAwait(false);
            var contract = contracts.Single(t => t.manifest.Name == "DevHawk.Registrar");

            var program = new Program(settings, contract.hash);

            var domainArg = new Argument<string>("domain");
            var queryCmd = new Command("query") { domainArg };

            queryCmd.SetHandler((string domain) => program.QueryAsync(domain), domainArg);

            var ownerArg = new Argument<string>("owner");
            var privateKeyArg = new Argument<string>("private-key");
            var registerCmd = new Command("register")
            {
                domainArg, ownerArg, privateKeyArg
            };

            registerCmd.SetHandler(
                (string domain, string owner, string privateKey) =>
                    program.RegisterAsync(domain, owner, privateKey),
                domainArg, ownerArg, privateKeyArg);

            var rootCmd = new RootCommand()
            {
                queryCmd, registerCmd,
            };

            await rootCmd.InvokeAsync(args).ConfigureAwait(false);
        }


        Task<int> QueryAsync(string domain)
        {
            return HandleErrors(async () =>
            {
                var domainParam = new ContractParameter(ContractParameterType.String) { Value = domain };
                using var builder = new ScriptBuilder();
                builder.EmitDynamicCall(contractHash, "query", domainParam);

                var rpcClient = new RpcClient(new Uri($"http://localhost:{RpcPort}"), protocolSettings: settings);
                var result = await rpcClient.InvokeScriptAsync(builder.ToArray()).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(result.Exception))
                {
                    throw new Exception(result.Exception);
                }

                if (result.Stack.Length != 1)
                {
                    throw new Exception($"Unexpected result stack length {result.Stack.Length}");
                }

                var ownerScriptHash = ToUInt160(result.Stack[0]);
                var text = ownerScriptHash.Equals(UInt160.Zero)
                    ? $"query: {domain} unowned"
                    : $"query: {domain} owned by {ownerScriptHash.ToAddress(settings.AddressVersion)}";

                await Console.Out.WriteLineAsync(text).ConfigureAwait(false);
            });
        }

        Task<int> RegisterAsync(string domain, string owner, string privateKey)
        {
            return HandleErrors(async () =>
            {
                var keyPair = new KeyPair(Convert.FromHexString(privateKey));

                var ownerAccount = owner.ToScriptHash(settings.AddressVersion);
                var domainParam = new ContractParameter(ContractParameterType.String) { Value = domain };
                var ownerParam = new ContractParameter(ContractParameterType.Hash160) { Value = ownerAccount };
                using var builder = new ScriptBuilder();
                builder.EmitDynamicCall(contractHash, "register", domainParam, ownerParam);

                var rpcClient = new RpcClient(new Uri($"http://localhost:{RpcPort}"), protocolSettings: settings);
                var factory = new TransactionManagerFactory(rpcClient);
                var signers = new[] { new Signer { Account = ownerAccount, Scopes = WitnessScope.CalledByEntry } };
                var tm = await factory.MakeTransactionAsync(builder.ToArray(), signers).ConfigureAwait(false);
                tm.AddSignature(keyPair);
                var tx = await tm.SignAsync().ConfigureAwait(false);
                var txHash = await rpcClient.SendRawTransactionAsync(tx).ConfigureAwait(false);

                await Console.Out.WriteLineAsync($"register: {domain} to {owner} (tx hash: {txHash})").ConfigureAwait(false);
            });
        }

        static UInt160 ToUInt160(Neo.VM.Types.StackItem item)
        {
            if (item.Type != Neo.VM.Types.StackItemType.ByteString)
            {
                throw new Exception($"Unexpected result stack type {item.Type}");
            }

            if (item.GetSpan().Length != UInt160.Length)
            {
                throw new Exception($"Unexpected result stack length {item.GetSpan().Length}");
            }

            return new UInt160(item.GetSpan());
        }

        static async Task<int> HandleErrors(Func<Task> func)
        {
            var originalForegroundColor = Console.ForegroundColor;
            try
            {
                await func().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                return 1;
            }
            finally
            {
                Console.ForegroundColor = originalForegroundColor;
            }
            return 0;
        }
    }
}
