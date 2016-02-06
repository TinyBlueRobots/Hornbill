namespace Hornbill

open System
open System.Net.Sockets
open System.Net
open System.Collections.Generic
open System.Text.RegularExpressions
open Microsoft.Owin.Hosting
open System.IO

type FakeService(port) =
  let responses = Dictionary<_, _>()
  let requests = ResizeArray<_>()
  let tryFindKey path methd = responses.Keys |> Seq.tryFind (fun (p, m) -> m = methd && Regex.IsMatch(path, p, RegexOptions.IgnoreCase))
  let mutable url = ""
  let requestReceived = Event<Request>()
  
  let findResponse (path, methd) = 
    let path = if Regex.IsMatch(path, ":/[^/]") then path.Replace(":/", "://") else path
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

  let port = if port = 0 then findPort() else port
  
  let mutable webApp = 
    { new IDisposable with
        member __.Dispose() = () }
  
  new() = new FakeService 0
  
  member __.OnRequestReceived(f : Action<Request>) = requestReceived.Publish.Add f.Invoke
  
  member __.AddResponse (path : string) verb response =
    let formatter : Printf.StringFormat<_> = 
      match path.StartsWith "/", path.EndsWith "$" with
      | false, false -> "/%s$"
      | false, true -> "/%s"
      | true, false -> "%s$"
      | _ -> "%s"
    responses.Add((sprintf formatter path, verb), response)
  
  member this.AddResponses filePath =
    for parsedRequest in File.ReadAllText filePath |> ResponsesParser.parseApi do
      let response = ResponsesParser.mapToResponse parsedRequest
      this.AddResponse parsedRequest.Path parsedRequest.Method response
  
  member __.Url = 
    match url with
    | "" -> failwith "Service not started"
    | _ -> url
  
  member this.Uri = Uri this.Url
  
  member __.Start() =
    let createHost =
      fun name -> sprintf "http://%s:%i" name port
    url <- createHost "localhost"
    webApp <- WebApp.Start(createHost "*", Middleware.app requests.Add findResponse setResponse requestReceived.Trigger)
    url
  
  [<Obsolete"Use Start()">]
  member this.Host() = this.Start()
  
  member __.Stop() = webApp.Dispose()
  member __.Requests = requests
  interface IDisposable with
    member this.Dispose() = this.Stop()
