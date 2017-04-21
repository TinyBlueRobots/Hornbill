namespace Hornbill

open System
open System.Net.Sockets
open System.Net
open System.Collections.Generic
open System.Text.RegularExpressions
open Microsoft.AspNetCore.Hosting
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

  let mutable webHost = Unchecked.defaultof<_>

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

  member __.AddResponsesFromText text =
   for parsedRequest in ResponsesParser.parse text do
      let response = ResponsesParser.mapToResponse parsedRequest
      __.AddResponse parsedRequest.Path parsedRequest.Method response

  member __.AddResponsesFromFile filePath =
    File.ReadAllText filePath |> __.AddResponsesFromText

  member __.Url =
    match url with
    | "" -> failwith "Service not started"
    | _ -> url

  member __.Uri = Uri __.Url

  member __.Start() =
    url <- sprintf "http://0.0.0.0:%i" port
    webHost <- WebHostBuilder().UseUrls(url).Configure(fun app -> Middleware.app requests.Add findResponse setResponse requestReceived.Trigger app).UseKestrel().Build()
    webHost.Start()
    url

  member __.Stop() = if isNull webHost |> not then webHost.Dispose()
  member __.Requests = requests
  member __.Dispose() = __.Stop()

  interface IDisposable with
    member __.Dispose() = __.Stop()
