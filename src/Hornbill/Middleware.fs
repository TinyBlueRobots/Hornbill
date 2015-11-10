namespace Hornbill

open Microsoft.Owin
open Owin
open System
open System.Threading.Tasks
open System.Collections.Generic

type StatusCode = int

type Headers = KeyValuePair<string, string> seq

type Body = string

type Response = 
  private
  | Text of Body
  | Code of statusCode : StatusCode
  | Headers of StatusCode * Headers
  | Full of StatusCode * Headers * Body
  | Raw of httpResponse : string
  static member CreateText body = Text body
  static member CreateCode statusCode = Code statusCode
  static member CreateHeaders(statusCode, headers) = Headers(statusCode, headers)
  static member CreateFull(statusCode, headers, body) = Full(statusCode, headers, body)
  static member CreateRaw httpResponse = Raw httpResponse

module private Middleware = 
  let task = Task.Delay 0
  
  let withStatusCode statusCode (ctx : IOwinContext) = 
    ctx.Response.StatusCode <- statusCode
    ctx
  
  let withHeaders headers (ctx : IOwinContext) = 
    headers |> Seq.iter (fun (header : KeyValuePair<_, _>) -> ctx.Response.Headers.Add(header.Key, [| header.Value |]))
    ctx
  
  let withBody (body : string) (ctx : IOwinContext) = ctx.Response.WriteAsync body
  let send _ = task
  
  let handler (responses : Dictionary<string, Response>) (ctx : IOwinContext) = 
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
      | Raw _ -> task
    
    let path = ctx.Request.Path.Value
    match responses.ContainsKey path with
    | true -> writeResponse responses.[path]
    | _ -> 
      ctx.Response.StatusCode <- 404
      sprintf "Path not found : %s" path |> ctx.Response.WriteAsync
  
  let app responses (app : IAppBuilder) = Func<_, _>(handler responses) |> app.Run

type FakeService() = 
  let responses = Dictionary<string, Response>()
  member __.AddResponse(path, response) = responses.Add(path, response)
  member __.App appBuilder = Middleware.app responses appBuilder
