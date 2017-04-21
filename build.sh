
mono=mono
if [ "$OS" = "Windows_NT" ]
  then
    mono=""
fi

dotnet restore
$mono packages/FAKE/tools/Fake.exe $@