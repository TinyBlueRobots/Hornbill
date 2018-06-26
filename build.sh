export FrameworkPathOverride=$(dirname $(which mono))/../lib/mono/4.5/
ls ./tests/**/*.fsproj | xargs -I {} "$SHELL" -c "echo {} && dotnet run -p {}"
dotnet build