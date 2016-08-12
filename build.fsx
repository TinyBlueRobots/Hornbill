#r "packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Testing.NUnit3
open Fake.AssemblyInfoFile

let solutionFile = !!"*.sln" |> Seq.head

let version = 
  if isLocalBuild then "0.0.0.0"
  else appVeyorBuildVersion

Target "KillProcesses" <| fun () -> 
  ProcessHelper.killProcess "nunit-agent"
  ProcessHelper.killMSBuild()

Target "Build" <| fun () -> 
  !!"src/**/bin/Release/" |> CleanDirs
  CleanDir "build"
  CreateFSharpAssemblyInfo "src/Hornbill/AssemblyInfo.fs"
    [Attribute.Title "Hornbill"
     Attribute.Guid "272AEABD-394C-4153-A530-2C015FB32DF3"
     Attribute.Product "Hornbill"
     Attribute.Version version
     Attribute.FileVersion version]
  build (fun x -> 
    { x with Verbosity = Some MSBuildVerbosity.Quiet
             Properties = [ "Configuration", "Release" ] }) solutionFile

Target "Test" <| fun () -> 
  let tests = !!"src/**/bin/Release/Tests*.dll"
  
  let nUnitParams _ = NUnit3Defaults
  try 
    tests |> NUnit3 nUnitParams
  finally
    if not isLocalBuild then AppVeyor.UploadTestResultsXml AppVeyor.TestResultsType.NUnit currentDirectory
    
Target "Nuget" <| fun () -> 
  let version = getUserInput "Version : "
  let apiKey = getUserInput "ApiKey : "
  Paket.Pack(fun p -> { p with Version = version })
  Paket.Push(fun p -> 
    { p with ApiKey = apiKey
             WorkingDir = "./temp" })
  DeleteDir "./temp"

Target "Default" DoNothing
"KillProcesses" ==> "Build" ==> "Test" ==> "Default"
"Default" ==> "Nuget"
RunTargetOrDefault "Default"
