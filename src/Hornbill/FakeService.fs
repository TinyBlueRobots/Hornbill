namespace Hornbill

open System
open System.Net.Sockets
open System.Net
open Microsoft.Owin.Hosting
open System.Collections.Generic
open System.Text.RegularExpressions

type FakeService() = 
  let responses = Dictionary<_, _>()
  let requests = ResizeArray<_>()
  let tryFindKey path methd = 
    responses.Keys |> Seq.tryFind (fun (p, m) -> m = methd && Regex.IsMatch(path, p, RegexOptions.IgnoreCase))
  
  let findResponse (path, methd) = 
    match tryFindKey path methd with
    | Some key -> Some responses.[key]
    | _ -> None
  
  let setResponse (path, methd) response = 
    match tryFindKey path methd with
    | Some key -> responses.[key] <- response
    | _ -> ()
  
  let findPort() = 
    TcpListener(IPAddress.Loopback, 0) |> fun l -> 
      l.Start()
      (l, (l.LocalEndpoint :?> IPEndPoint).Port) |> fun (l, p) -> 
        l.Stop()
        p
  
  let mutable webApp = 
    { new IDisposable with
        member __.Dispose() = () }
  
  member __.AddResponse(path : string, verb, response) = 
    let path = 
      match path.[path.Length - 1] with
      | '$' -> path
      | _ -> sprintf "%s$" path
    match path.[0] with
    | '/' -> responses.Add((path, verb), response)
    | _ -> responses.Add((sprintf "/%s" path, verb), response)
  
  member __.App appBuilder = Middleware.app requests.Add findResponse setResponse appBuilder
  
  member this.Host() = 
    let host = findPort() |> sprintf "http://localhost:%i"
    webApp <- WebApp.Start(host, this.App)
    host
  
  member __.Requests = requests
  interface IDisposable with
    member __.Dispose() = webApp.Dispose()
