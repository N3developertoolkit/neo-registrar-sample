using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Neo;
using Neo.Network.RPC;
using NEP6Wallet = Neo.Wallets.NEP6.NEP6Wallet;

namespace DevHawk.Registrar.Cli
{
    // TODO: generate Registrar interface at build time
    interface Registrar
    {
        bool delete(string domain);
        Neo.UInt160 query(string domain);
        bool register(string domain, Neo.UInt160 owner);
        bool transfer(string domain, Neo.UInt160 to);
    }

    class Program
    {
        const uint Magic = 112694212;
        const byte AddressVersion = 53;
        const ushort RpcPort = 50012;

        static ProtocolSettings ProtocolSettings => ProtocolSettings.Default with
        {
            Magic = Magic,
            AddressVersion = AddressVersion,
        };

        static RpcClient BuildRpcClient() => new RpcClient(
            new Uri($"http://localhost:{RpcPort}"),
            protocolSettings: ProtocolSettings);

        static async Task Main(string[] args)
        {
            var queryCmd = new Command(nameof(Registrar.query))
            {
                new Argument<string>("domain"),
            };
            queryCmd.Handler = CommandHandler.Create<string>(Query);

            var registerCmd = new Command(nameof(Registrar.register))
            {
                new Argument<string>("domain"),
                new Argument<FileInfo>("walletPath"),
            };
            registerCmd.Handler = CommandHandler.Create<string, FileInfo>(Register);

            var transferCmd = new Command(nameof(Registrar.transfer))
            {
                new Argument<string>("domain"),
                new Argument<string>("toAddress"),
                new Argument<FileInfo>("walletPath"),
            };
            transferCmd.Handler = CommandHandler.Create<string, string, FileInfo>(Transfer);

            var deleteCmd = new Command(nameof(Registrar.delete))
            {
                new Argument<string>("domain"),
                new Argument<FileInfo>("walletPath"),
            };
            deleteCmd.Handler = CommandHandler.Create<string, FileInfo>(Delete);

            var rootCmd = new RootCommand()
            {
                queryCmd, registerCmd, transferCmd, deleteCmd
            };

            await rootCmd.InvokeAsync(args).ConfigureAwait(false);
        }

        static async Task<int> Query(string domain)
        {
            await Console.Out.WriteLineAsync($"query {domain}");
            return 0;
        }

        static async Task<int> Register(string domain, FileInfo walletPath)
        {
            await Console.Out.WriteLineAsync($"register {domain} {walletPath.FullName}");
            return 0;
        }

        static async Task<int> Transfer(string domain, string toAddress, FileInfo walletPath)
        {
            if (TryToScriptHash(toAddress, AddressVersion, out var scriptHash))
            {
                await Console.Out.WriteLineAsync($"transfer {domain} {scriptHash}");
                return 0;
            }
            else
            {
                await Console.Error.WriteLineAsync($"Invalid address {toAddress}").ConfigureAwait(false);
                return 1;
            }
        }

        static async Task<int> Delete(string domain, FileInfo walletPath)
        {
            await Console.Out.WriteLineAsync($"delete {domain}");
            return 0;
        }

        static bool TryOpenWallet(FileInfo walletPath, [MaybeNullWhen(false)] out Neo.Wallets.NEP6.NEP6Wallet wallet)
        {
            try
            {
                if (walletPath.Exists)
                {
                    wallet = new NEP6Wallet(walletPath.FullName, ProtocolSettings);
                    Console.Out.WriteLine("Enter Wallet Password: ");
                    var password = GetConsolePassword();
                    if (wallet.VerifyPassword(password))
                    {
                        wallet.Unlock(password);
                        return true;
                    }
                }
            }
            catch { }

            wallet = default;
            return false;
        }

        // https://gist.github.com/huobazi/1039424#file-gistfile1-cs-L35
        private static string GetConsolePassword()
        {
            var sb = new System.Text.StringBuilder();
            while (true)
            {
                var cki = Console.ReadKey(true);
                if (cki.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }

                if (cki.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0)
                    {
                        Console.Write("\b\0\b");
                        sb.Length--;
                    }

                    continue;
                }

                Console.Write('*');
                sb.Append(cki.KeyChar);
            }

            return sb.ToString();
        }

        static bool TryToScriptHash(string address, byte version, [MaybeNullWhen(false)] out UInt160 scriptHash)
        {
            try
            {
                scriptHash = Neo.Wallets.Helper.ToScriptHash(address, version);
                return true;
            }
            catch { }

            scriptHash = default;
            return false;
        }
    }
}
