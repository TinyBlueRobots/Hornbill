namespace Hornbill

open Microsoft.Owin
open Owin
open System
open System.Threading.Tasks
open System.Collections.Generic

type Response = 
  private
  | Text of body : string
  | Code of statusCode : int
  | Full of statusCode : int * headers : KeyValuePair<string, string> seq * body : string
  | Raw of httpResponse : string

  static member CreateText(body) = Text(body)
  static member CreateCode(statusCode) = Code(statusCode)
  static member CreateFull(statusCode, headers, body) = Full(statusCode, headers, body)
  static member CreateRaw(httpResponse) = Raw(httpResponse)

module private Middleware = 
  let task = Task.Delay 0
  
  let handler (responses : Dictionary<string, Response>) (ctx : IOwinContext) = 
    match responses.[ctx.Request.Path.Value] with
    | Text(body) -> 
      ctx.Response.WriteAsync body
    | Code(statusCode) -> 
      ctx.Response.StatusCode <- statusCode
      task
    | Full(statusCode, headers, body) -> 
      ctx.Response.StatusCode <- statusCode
      headers |> Seq.iter (fun header -> ctx.Response.Headers.Add(header.Key, [| header.Value |]))      
      ctx.Response.WriteAsync body
    | Raw(httpResponse) -> 
      task
  
  let app responses (app : IAppBuilder) = Func<_, _>(handler responses) |> app.Run

type FakeService() = 
  let responses = Dictionary<string, Response>()
  member __.AddResponse(path, response) = responses.Add(path, response)
  member __.App appBuilder = Middleware.app responses appBuilder
