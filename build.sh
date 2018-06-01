export FrameworkPathOverride=$(dirname $(which mono))/../lib/mono/4.5/
for line in `ls ./tests/**/*proj`; do
  pushd `dirname $line`
  dotnet run
  popd;
done
dotnet build