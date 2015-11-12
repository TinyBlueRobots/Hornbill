#r "packages/FAKE/tools/FakeLib.dll"

open Fake

let solutionFile = !!"*.sln" |> Seq.head

let version = 
  if isLocalBuild then "0.0.0.0"
  else appVeyorBuildVersion

Target "KillProcesses" <| fun () -> 
  ProcessHelper.killProcess "nunit-agent"
  ProcessHelper.killMSBuild()

Target "Build" <| fun () -> 
  !!"src/**/bin/Release/" |> CleanDirs
  build (fun x -> 
    { x with Verbosity = Some MSBuildVerbosity.Quiet
             Properties = [ "Configuration", "Release" ] }) solutionFile

Target "Test" <| fun () -> 
  let tests = !!"src/**/bin/Release/Tests.dll"
  
  let nUnitParams p = 
    { p with DisableShadowCopy = true
             TimeOut = System.TimeSpan.FromMinutes 10. }
  try 
    tests |> NUnit nUnitParams
  finally
    if not isLocalBuild then AppVeyor.UploadTestResultsXml AppVeyor.TestResultsType.NUnit currentDirectory

Target "Nuget" <| fun () -> 
  let version = getUserInput "Version : "
  let apiKey = getUserInput "ApiKey : "
  Paket.Pack(fun p -> { p with Version = version })
  Paket.Push(fun p -> 
    { p with ApiKey = apiKey
             WorkingDir = "./temp" })

Target "Default" DoNothing
"KillProcesses" ==> "Build" ==> "Test" ==> "Default"
"Default" ==> "Nuget"
RunTargetOrDefault "Default"
