namespace Hornbill

open System
open System.Net.Sockets
open System.Net
open System.Collections.Generic
open System.Text.RegularExpressions
open Microsoft.AspNetCore.Hosting
open System.IO
open Microsoft.AspNetCore.TestHost
open Microsoft.AspNetCore.Builder

type FakeService(port) =
  let responses = Dictionary<_, _>()
  let requests = ResizeArray()

  let tryFindKey path methd =
    responses.Keys
    |> Seq.tryFind (fun (p, m) -> m = methd && Regex.IsMatch(path, p, RegexOptions.IgnoreCase))

  let mutable url = ""
  let requestReceived = Event<Request>()

  let findResponse (path, methd) =
    let path =
      if Regex.IsMatch(path, ":/[^/]") then
        path.Replace(":/", "://")
      else
        path

    match tryFindKey path methd with
    | Some key -> Some responses.[key]
    | _ -> None

  let setResponse (path, methd) response =
    match tryFindKey path methd with
    | Some key -> responses.[key] <- response
    | _ -> ()

  let findPort () =
    TcpListener(IPAddress.Loopback, 0)
    |> fun l ->
         l.Start()

         (l, (l.LocalEndpoint :?> IPEndPoint).Port)
         |> fun (l, p) ->
              l.Stop()
              p

  let webHostBuilder =
    WebHostBuilder()
      .Configure(fun app -> Middleware.app requests.Add findResponse setResponse requestReceived.Trigger app)

  let mutable webHost = Unchecked.defaultof<_>
  let mutable testServer = Unchecked.defaultof<_>
  new() = new FakeService 0
  member _.OnRequestReceived(f: Action<Request>) = requestReceived.Publish.Add f.Invoke

  member _.AddResponse (path: string) verb response =
    let formatter: Printf.StringFormat<_> =
      match path.StartsWith "/", path.EndsWith "$" with
      | false, false -> "/%s$"
      | false, true -> "/%s"
      | true, false -> "%s$"
      | _ -> "%s"

    let key, value = (sprintf formatter path, verb), response

    if responses.ContainsKey key then
      responses.Remove key |> ignore

    responses.Add(key, value)

  member __.AddResponsesFromText text =
    for parsedRequest in ResponsesParser.parse text do
      let response = ResponsesParser.mapToResponse parsedRequest

      __.AddResponse parsedRequest.Path parsedRequest.Method response

  member __.AddResponsesFromFile filePath =
    File.ReadAllText filePath |> __.AddResponsesFromText

  member _.Url =
    match url with
    | "" -> failwith "Service not started"
    | _ -> url

  member __.Uri = Uri __.Url

  member _.Start() =
    let port = if port = 0 then findPort () else port
    url <- sprintf "http://127.0.0.1:%i" port

    webHost <-
      webHostBuilder
        .UseUrls(url)
        .UseKestrel(fun options -> options.AllowSynchronousIO <- true)
        .Build()

    webHost.Start()
    url

  member _.StartTestHost() =
    testServer <- new TestServer(webHostBuilder)
    testServer.CreateClient()

  member _.StartApp(app: Action<IApplicationBuilder>) =
    let port = if port = 0 then findPort () else port
    url <- sprintf "http://127.0.0.1:%i" port

    webHost <-
      WebHostBuilder()
        .Configure(app)
        .UseUrls(url)
        .UseKestrel(fun options -> options.AllowSynchronousIO <- true)
        .Build()

    webHost.Start()
    url

  member _.Stop() =
    let dispose (disposable: #IDisposable) =
      if isNull disposable |> not then
        disposable.Dispose()

    dispose testServer
    dispose webHost

  member _.Requests = requests
  member _.Responses = responses
  member __.Dispose() = __.Stop()

  interface IDisposable with
    member __.Dispose() = __.Stop()
