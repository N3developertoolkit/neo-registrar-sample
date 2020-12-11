dotnet tool restore
mkdir ./checkpoints -force | out-null

dotnet build ./src
dotnet nxp3 reset -f
dotnet nxp3 transfer gas 10000 genesis owen
dotnet nxp3 transfer gas 10000 genesis alice
dotnet nxp3 transfer gas 10000 genesis bob
dotnet nxp3 contract deploy ./src/bin/Debug/netstandard2.1/registrar.nef owen
dotnet nxp3 checkpoint create checkpoints/contract-deployed -f
dotnet nxp3 contract invoke ./invoke-files/register-sample-domain.neo-invoke.json bob
dotnet nxp3 checkpoint create checkpoints/sample-domain-registered -f
