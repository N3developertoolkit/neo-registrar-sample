# Registrar Neo 3 Smart Contract Sample

This repo contains the [Domain Smart Contract sample](https://github.com/neo-project/examples/tree/0ab03a0ed5e1e331b756d9ad51b01657385470c7/csharp/Domain)
updated for Neo 3 RC1 and with additional assets that can be used with the Neo 3 RC1 version of the
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

