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
  
  let tryPick path methd (kvp : KeyValuePair<_, _>) = 
    let p, m = kvp.Key
    if m = methd && Regex.IsMatch(path, p) then Some kvp.Value
    else None
  
  let findResponse (path, methd) = responses |> Seq.tryPick (tryPick path methd)
  let setResponse (path, methd) response = responses.[(path, methd)] <- response
  
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
