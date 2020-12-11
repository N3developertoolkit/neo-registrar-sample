using static Bullseye.Targets;
using System;
using System.Threading.Tasks;
using System.IO;
using SimpleExec;

class Neo
{
    public static void Transfer(string asset, Decimal amount, string sender, string receiver)
        => Command.Run("dotnet", $"nxp3 transfer {asset} {amount} {sender} {receiver}");
    public static void Deploy(string contractPath, string account)
        => Command.Run("dotnet", $"nxp3 contract deploy {contractPath} {account}");
    public static void Invoke(string invokeFilePath, string account)
        => Command.Run("dotnet", $"nxp3 contract invoke {invokeFilePath} {account}");
    public static void Checkpoint(string checkpointPath, bool force = true)
        => Command.Run("dotnet", $"nxp3 checkpoint create {checkpointPath} {(force ? "--force" : "")}");
    public static void Checkpoint(string checkpointDir, string checkpointFile, bool force = true)
        => Checkpoint(Path.Join(checkpointDir, checkpointFile), force);
    public static void Reset(bool force = true)
        => Command.Run("dotnet", $"nxp3 reset {(force ? "--force" : "")}");
}

class DotNet
{
    public static void RestoreTools() => Command.Run("dotnet", "tool restore");
    public static void Build(string path) => Command.Run("dotnet", $"build {path}");
}

class FileSys
{
    public static void MakeDir(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}

class Program
{
    private const string CHECKPOINTS_PATH = "./checkpoints";

    static void Main(string[] args)
    {
        Target("restore-tools", () => DotNet.RestoreTools());
        Target("build-contract", () => DotNet.Build("./src"));
        Target("create-checkpoint-dir", () => FileSys.MakeDir(CHECKPOINTS_PATH));
        Target("reset-express", DependsOn("restore-tools"), () => Neo.Reset());
        Target("setup-accounts", DependsOn("reset-express"), () =>
        {
            Neo.Transfer("gas", 10000, "genesis", "owen");
            Neo.Transfer("gas", 10000, "genesis", "alice");
            Neo.Transfer("gas", 10000, "genesis", "bob");
        });
        Target("deploy-contract", DependsOn("setup-accounts", "build-contract"), () => 
            Neo.Deploy("./src/bin/Debug/netstandard2.1/registrar.nef", "owen"));
        Target("contract-deployed-checkpoint", DependsOn("deploy-contract", "create-checkpoint-dir"), () =>
            Neo.Checkpoint(CHECKPOINTS_PATH, "contract-deployed"));
        Target("invoke-register-sample-domain", DependsOn("contract-deployed-checkpoint"), () =>
            Neo.Invoke("./invoke-files/register-sample-domain.neo-invoke.json", "bob"));
        Target("sample-domain-registered-checkpoint", DependsOn("invoke-register-sample-domain"), () =>
            Neo.Checkpoint(CHECKPOINTS_PATH, "sample-domain-registered"));

        Target("default", DependsOn("sample-domain-registered-checkpoint"));
        RunTargetsAndExit(args);
    }
}