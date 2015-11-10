namespace Hornbill

open Microsoft.Owin
open Owin
open System
open System.Threading.Tasks
open System.Collections.Generic
open System.Net.Sockets
open System.Net
open Microsoft.Owin.Hosting

type StatusCode = int

type Headers = KeyValuePair<string, string> seq

type Body = string

type Method = 
  | DELETE = 0
  | GET = 1
  | HEAD = 2
  | OPTIONS = 3
  | POST = 4
  | PUT = 5
  | TRACE = 6

type Request = 
  { Method : Method
    Path : string
    Body : string
    Headers : Dictionary<string, string array> }

type Response = 
  private
  | Text of Body
  | Code of StatusCode
  | Headers of StatusCode * Headers
  | Full of StatusCode * Headers * Body
  static member CreateText body = Text body
  static member CreateCode statusCode = Code statusCode
  static member CreateHeaders(statusCode, headers) = Headers(statusCode, headers)
  static member CreateFull(statusCode, headers, body) = Full(statusCode, headers, body)

module private Middleware = 
  open System.Text.RegularExpressions
  open System.IO
  
  let toMethod m = Enum.Parse(typeof<Method>, m) :?> Method
  let getMethod (ctx : IOwinContext) = ctx.Request.Method |> toMethod
  
  let toRequest (request : IOwinRequest) = 
    { Method = request.Method |> toMethod
      Path = request.Path.Value
      Body = (new StreamReader(request.Body)).ReadToEnd()
      Headers = Dictionary request.Headers}
  
  let withStatusCode statusCode (ctx : IOwinContext) = 
    ctx.Response.StatusCode <- statusCode
    ctx
  
  let withHeaders headers (ctx : IOwinContext) = 
    headers |> Seq.iter (fun (header : KeyValuePair<_, _>) -> ctx.Response.Headers.Add(header.Key, [| header.Value |]))
    ctx
  
  let withBody (body : string) (ctx : IOwinContext) = ctx.Response.WriteAsync body
  let send _ = Task.Delay 0
  
  let find (ctx : IOwinContext) (kvp : KeyValuePair<_, _>) = 
    let p, m = kvp.Key
    getMethod ctx = m && Regex.IsMatch(ctx.Request.Path.Value, p)
  
  let handler (requests : ResizeArray<_>) (responses : Dictionary<string * Method, Response>) (ctx : IOwinContext) = 
    ctx.Request |> toRequest |> requests.Add 
    let writeResponse = 
      function 
      | Text body -> ctx |> withBody body
      | Code statusCode -> 
        ctx
        |> withStatusCode statusCode
        |> send
      | Headers(statusCode, headers) -> 
        ctx
        |> withStatusCode statusCode
        |> withHeaders headers
        |> send
      | Full(statusCode, headers, body) -> 
        ctx
        |> withStatusCode statusCode
        |> withHeaders headers
        |> withBody body
    
    let path = ctx.Request.Path.Value
    match responses |> Seq.tryFind (find ctx) with
    | Some kvp -> writeResponse kvp.Value
    | _ -> 
      ctx.Response.StatusCode <- 404
      sprintf "Path not found : %s" path |> ctx.Response.WriteAsync
  
  let app requests responses (app : IAppBuilder) = Func<_, _>(handler requests responses) |> app.Run

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
