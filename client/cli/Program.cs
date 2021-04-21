using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
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
        const uint Magic = 112694212;
        const byte AddressVersion = 53;
        const ushort RpcPort = 50012;
        readonly static UInt160 RegistrarContractHash = UInt160.Parse("0xdfa2d9762736cd6edb09c066db646d967e09abbb");

        static ProtocolSettings ProtocolSettings => ProtocolSettings.Default with
        {
            Magic = Magic,
            AddressVersion = AddressVersion,
        };

        static async Task Main(string[] args)
        {
            var queryCmd = new Command("query")
            {
                new Argument<string>("domain"),
            };
            queryCmd.Handler = CommandHandler.Create<string>(QueryAsync);

            var registerCmd = new Command("register")
            {
                new Argument<string>("domain"),
                new Argument<string>("owner"),
                new Option<FileInfo>(new[] { "-w", "--wallet-path" }, "Path to wallet file used to sign transaction"),
                new Option<string>(new[] { "-k", "--private-key" }, "Private key used to sign transaction"),
            };

            registerCmd.Handler = CommandHandler.Create<string, string, Options>(RegisterAsync);

            var rootCmd = new RootCommand()
            {
                queryCmd,
                registerCmd,
            };

            await rootCmd.InvokeAsync(args).ConfigureAwait(false);
        }


        static Task<int> QueryAsync(string domain)
        {
            return HandleErrors(async () =>
            {
                var domainParam = new ContractParameter(ContractParameterType.String) { Value = domain };
                using var builder = new ScriptBuilder();
                builder.EmitDynamicCall(RegistrarContractHash, "query", domainParam);

                var rpcClient = new RpcClient(new Uri($"http://localhost:{RpcPort}"), protocolSettings: ProtocolSettings);
                var result = await rpcClient.InvokeScriptAsync(builder.ToArray());

                if (!string.IsNullOrEmpty(result.Exception))
                {
                    throw new Exception(result.Exception);
                }

                if (result.Stack.Length != 1)
                {
                    throw new Exception($"Unexpected result stack length {result.Stack.Length}");
                }

                var ownerScriptHash = ToUInt160(result.Stack[0]);
                await Console.Out.WriteLineAsync($"query: {domain} owned by {ownerScriptHash.ToAddress(AddressVersion)}");
            });
        }

        static Task<int> RegisterAsync(string domain, string owner, Options options)
        {
            return HandleErrors(async () =>
            {
                if (!options.TryGetKeyPair(out var keyPair))
                {
                    throw new Exception("private key or wallet must be specified");
                }

                var ownerAccount = owner.ToScriptHash(AddressVersion);
                var domainParam = new ContractParameter(ContractParameterType.String) { Value = domain };
                var ownerParam = new ContractParameter(ContractParameterType.Hash160) { Value = ownerAccount };
                using var builder = new ScriptBuilder();
                builder.EmitDynamicCall(RegistrarContractHash, "register", domainParam, ownerParam);

                var rpcClient = new RpcClient(new Uri($"http://localhost:{RpcPort}"), protocolSettings: ProtocolSettings);
                var factory = new TransactionManagerFactory(rpcClient);
                var signers = new[] { new Signer { Account = ownerAccount, Scopes = WitnessScope.CalledByEntry } };
                var tm = await factory.MakeTransactionAsync(builder.ToArray(), signers).ConfigureAwait(false);
                tm.AddSignature(keyPair);
                var tx = await tm.SignAsync();
                var txHash = await rpcClient.SendRawTransactionAsync(tx);

                await Console.Out.WriteLineAsync($"register: {domain} to {owner} (tx hash: {txHash})");
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
            try
            {
                await func();
            }
            catch (Exception ex)
            {
                using var _ = ConsoleColorManager.SetColor(ConsoleColor.Red);
                Console.WriteLine(ex.Message);
                return 1;
            }
            return 0;
        }
    }
}
