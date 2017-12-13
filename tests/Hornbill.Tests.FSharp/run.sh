pushd "$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
dotnet publish -f netcoreapp20 && dotnet bin/Debug/netcoreapp20/publish/Hornbill.Tests.FSharp.dll
popd