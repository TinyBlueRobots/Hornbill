NUGETVERSION=3.1.0
dotnet pack src/Hornbill -c Release /p:PackageVersion=$NUGETVERSION
dotnet nuget push src/Hornbill/bin/Release/Hornbill.$NUGETVERSION.nupkg -k $NUGETKEY -s nuget.org