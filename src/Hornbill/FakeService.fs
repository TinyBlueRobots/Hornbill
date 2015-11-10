namespace Hornbill

open System
open System.Collections.Generic
open System.Net.Sockets
open System.Net
open Microsoft.Owin.Hosting

type FakeService() = 
  let responses = Dictionary<string * Method, Response>()
  let requests = ResizeArray<_>()
  
  let findPort() = 
    TcpListener(IPAddress.Loopback, 0) |> fun l -> 
      l.Start()
      (l, (l.LocalEndpoint :?> IPEndPoint).Port) |> fun (l, p) -> 
        l.Stop()
        p
  
  let mutable webApp = 
    { new IDisposable with
        member __.Dispose() = () }
  
  member __.AddResponse(path, verb, response) = responses.Add((path, verb), response)
  member __.App appBuilder = Middleware.app requests responses appBuilder
  
  member this.Host() = 
    let host = findPort() |> sprintf "http://localhost:%i"
    webApp <- WebApp.Start(host, this.App)
    host
  
  member __.Requests = requests
  interface IDisposable with
    member __.Dispose() = webApp.Dispose()