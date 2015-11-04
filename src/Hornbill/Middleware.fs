namespace Hornbill

open Microsoft.Owin
open Owin
open System
open System.Threading.Tasks
open System.Collections.Generic

type Response = 
  private
  | Response of statusCode : int * body : string option
  static member Create(statusCode, body) = Response(statusCode, Some body)
  static member Create(statusCode) = Response(statusCode, None)

module private Middleware = 
  let task = Task.Delay 0
  
  let handler (responses : Dictionary<string, Response>) (ctx : IOwinContext) = 
    match responses.[ctx.Request.Path.Value] with
    | Response(statusCode, Some body) -> 
      ctx.Response.StatusCode <- statusCode
      ctx.Response.WriteAsync body
    | Response(statusCode, None) -> 
      ctx.Response.StatusCode <- statusCode
      task
  
  let app responses (app : IAppBuilder) = Func<_, _>(handler responses) |> app.Run

type FakeService() = 
  let responses = Dictionary<string, Response>()
  member __.AddResponse(path, response) = responses.Add(path, response)
  member __.App appBuilder = Middleware.app responses appBuilder
