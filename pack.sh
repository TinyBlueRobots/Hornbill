export FrameworkPathOverride=$(dirname $(which mono))/../lib/mono/4.5/
NUGETVERSION=2.6.2
dotnet pack src/Hornbill -c Release /p:PackageVersion=$NUGETVERSION
dotnet nuget push src/Hornbill/bin/Release/Hornbill.$NUGETVERSION.nupkg -k $NUGETKEY -s nuget.org