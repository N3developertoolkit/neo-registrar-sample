# Registrar Neo 3 Smart Contract Sample

This repo contains the [Domain Smart Contract sample](https://github.com/neo-project/examples/tree/0ab03a0ed5e1e331b756d9ad51b01657385470c7/csharp/Domain)
updated for Neo 3 and with additional assets that can be used with the Neo 3 preview 4 version of the
[Neo Blockchain Toolkit](https://marketplace.visualstudio.com/items?itemName=ngd-seattle.neo-blockchain-toolkit).

## Prerequisites

> Note, if you're using [VS Code Remote Container](https://code.visualstudio.com/docs/remote/containers)
  or [GitHub Codespaces](https://github.com/features/codespaces),
  the [devcontainer Dockerfile](.devcontainer/Dockerfile) for this repo has all the prerequisites installed.

- [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1)
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

## Installing Neo Blockchain Toolkit for Neo 3 preview 4

### Command Line Tools

To build the contract in this repo, you will need [NEON](https://github.com/neo-project/neo-devpack-dotnet) - the
Neo smart contract compiler for .NET. To deploy and test the contract, you will need
[Neo-Express](https://github.com/neo-project/neo-express) to run a Neo privatenet for development purposes.
Both of these are distributed as [.NET tools](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools).
This repo contains a [tool manifest](.config/dotnet-tools.json) specifying the correct versions of these tools
to install.  You can install the tools from the command line via the `dotnet tool restore` command.

``` shell
$ dotnet tool restore
Tool 'neo.express3' (version '1.2.85-insiders') was restored. Available commands: nxp3
Tool 'neo.neon' (version '3.0.0-preview4') was restored. Available commands: neon

Restore was successful.
```

> Note, If you're using Remote Container or Codespaces, the [devcontainer file](.devcontainer/devcontainer.json)
  executes `dotnet tool restore` during container creation.

If you are building in VS Code, running the default `build` task (Terminal menu -> Run Build Task...) will
also run `dotnet tool restore` automatically.

### VS Code Extensions

The Neo Blockchain Toolkit also contains two VS Code extensions - [Visual DevTracker](https://github.com/ngdenterprise/neo3-visual-tracker)
and the [Smart Contract Debugger](https://github.com/neo-project/neo-debugger). As of Neo 3 preview 4,
these extensions are not yet available via the VS Code Marketplace. In the meantime, you can install
the extensions manually by downloading the preview 4 compatible releases from their GitHub repos and
then installing downloaded VSIX files as per the
[official VS Code documentation](https://code.visualstudio.com/docs/editor/extension-gallery#_install-from-a-vsix)

* Visual DevTracker: https://github.com/neo-project/neo-debugger/releases/download/1.2.58-preview/neo-contract-debug-1.2.58-preview.vsix
* Smart Contract Debugger: https://github.com/ngdenterprise/neo3-visual-tracker/releases/download/v0.5.424-preview/neo3-visual-tracker.vsix

## Building the Contract

Building the contract (via VS Code or by simply running `dotnet build` from the terminal) will also
deploy the compiled contract to an instance of Neo-Express, configure several test accounts and
create the checkpoints used for automated testing. Resetting and deploying the updated contract is
directly integrated into the MSBuild files used to compile the C# contract project. This ensures the
checkpoints used for testing and debugging are always in sync with the contract code. The 
[neo-express.targets file](./neo-express.targets) contains the Neo-Express commands that configure
the privatenet as desired for debugging and testing.

# Running Automated Contract Tests

Tests can be executed from the command line by running `dotnet test`. 

To run .NET based tests in VS Code, you can use the "Run Test" CodeLens provided the
[MSFT C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) or
install the [.NET Core Test Explorer](https://marketplace.visualstudio.com/items?itemName=formulahendry.dotnet-test-explorer).

The [automated tests](./test) for this contract are using the 
[Neo Test Harness](https://github.com/ngdenterprise/neo-test/tree/main/src/test-harness) and
[Neo Assertions](https://github.com/ngdenterprise/neo-test/tree/main/src/assertions) libraries. 
More information on these libraries is available in the [neo-test repo](https://github.com/ngdenterprise/neo-test).
