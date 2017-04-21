#r "packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.DotNetCli

Target "Test" <| fun () ->
  let run proj =
    let workingDir = sprintf "**/%s*proj" proj |> Include |> Seq.head |> DirectoryName
    DotNetCli.Publish (fun p -> { p with WorkingDir = workingDir })
    sprintf "bin/Release/netcoreapp1.1/publish/%s.dll" proj |> DotNetCli.RunCommand (fun p -> { p with WorkingDir = workingDir })
  run "Hornbill.Tests.CSharp"
  run "Hornbill.Tests.FSharp"

Target "Nuget" <| fun () ->
  let version = getUserInput "Version : "
  let apiKey = getUserInput "ApiKey : "
  Paket.Pack(fun p -> { p with Version = version })
  Paket.Push(fun p ->
    { p with ApiKey = apiKey
             WorkingDir = "./temp" })
  DeleteDir "./temp"

Target "Default" DoNothing
"Test" ==> "Default"
"Default" ==> "Nuget"
RunTargetOrDefault "Default"
