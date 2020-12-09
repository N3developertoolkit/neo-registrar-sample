# dotnet build C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj

dotnet build ./src
dotnet run -p C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj --no-build -- reset -f
dotnet run -p C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj --no-build -- transfer gas 10000 genesis owen
dotnet run -p C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj --no-build -- contract deploy ./src/bin/Debug/netstandard2.1/registrar.nef owen
dotnet run -p C:\Users\harry\Source\neo\seattle\express\src\nxp3\nxp3.csproj --no-build -- checkpoint create checkpoints/contract-deployed -f
