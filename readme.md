# Neo N3 Domain Registrar Smart Contract Sample

This repo contains the [Domain Smart Contract sample](https://github.com/neo-project/examples/tree/0ab03a0ed5e1e331b756d9ad51b01657385470c7/csharp/Domain)
updated for Neo N3 and with additional assets to demonstrate the
[Neo Blockchain Toolkit](https://marketplace.visualstudio.com/items?itemName=ngd-seattle.neo-blockchain-toolkit).

## Prerequisites

> Note, if you're using [VS Code Remote Container](https://code.visualstudio.com/docs/remote/containers)
  or [GitHub Codespaces](https://github.com/features/codespaces),
  the [devcontainer Dockerfile](.devcontainer/Dockerfile) for this repo has all the prerequisites installed.

- [.NET 5.0 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
- [Visual Studio Code (v1.52 or later)](https://code.visualstudio.com/Download)

### Ubuntu Prerequisites

Installing on Ubuntu 18.04 or 20.04 also requires installing libsnappy-dev and libc6-dev
via apt-get. 

``` shell
$ sudo apt install libsnappy-dev libc6-dev -y
```

### MacOS Prerequisites

Installing on MacOS requires installing rocksdb via [Homebrew](https://brew.sh/).

``` shell
$ brew install rocksdb
```

## NeoTrace Quickstart

Among other things, this repo demonstrates the use of NeoTrace.

The registrar contract has been deployed to Neo N3 TestNet
(contract hash [0x476c264ac5ec4ac95cfe0e5ba92abd87f47bdf3f](https://dora.coz.io/contract/neo3/testnet_rc4/0x476c264ac5ec4ac95cfe0e5ba92abd87f47bdf3f)).
The `register` operation was invoked in transaction
[0x45239b3764a0973c89c1fca6bf1ef438a462f7fb705cdf7cf1739abe48328dad](https://dora.coz.io/transaction/neo3/testnet_rc4/0x45239b3764a0973c89c1fca6bf1ef438a462f7fb705cdf7cf1739abe48328dad). 
You can generate a .neo-trace file for this transaction locally via the `neotrace` tool and then step
thru the generated trace file with the Neo Smart Contract Debugger. 

1. Open the registrar contract sample folder in VSCode
2. Run the default build task via Terminal -> Run Build Task... menu item or Ctrl-Shift-B
3. Select Terminal -> Run Task... menu item and select the `generate trace` task
4. Select the `register (trace)` configuration from the "Run and Debug" window and press the green 
   arrow to launch the debugger
5. Step thru the code to see how it executed on TestNet. Note, since this is a trace file you can
   step forwards and backwards thru the code.

> Note, the build step will also ensure the Neo tools (NCCS, NeoExpress and NeoTrace) are installed and up to date
> via the `dotnet tool restore` command

The `generate trace` task executes the command 
`dotnet neotrace tx 0x45239b3764a0973c89c1fca6bf1ef438a462f7fb705cdf7cf1739abe48328dad -r testnet`
in the trace-files directory (where `register (trace)` launch configuration expects to find the generated
.neo-trace file). This command downloads the block containing the specified transaction from the specified
network and executes it locally with the
[TraceApplicationEngine](https://github.com/ngdenterprise/neo-blockchaintoolkit-library/blob/master/src/bctklib/smart-contract/TraceApplicationEngine.cs)
to generate the .neo-trace file. The generated .net-trace file can be debugged by specifying the `invocation.trace-file`
property in the VSCode launch configuration.

``` json
{
    "name": "register (trace)",
    "type": "neo-contract",
    "request": "launch",
    "program": "${workspaceFolder}/src/bin/sc/registrar.nef",
    "invocation": {
        "trace-file": "${workspaceFolder}/trace-files/0x45239b3764a0973c89c1fca6bf1ef438a462f7fb705cdf7cf1739abe48328dad.neo-trace"
    }
}
```
