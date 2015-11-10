module internal Middleware

open Microsoft.Owin
open Owin
open System
open System.Threading.Tasks
open System.Collections.Generic
open Hornbill
open System.Text.RegularExpressions
open System.IO

let toMethod m = Enum.Parse(typeof<Method>, m) :?> Method
let getMethod (ctx : IOwinContext) = ctx.Request.Method |> toMethod

let toRequest (request : IOwinRequest) = 
  { Method = request.Method |> toMethod
    Path = request.Path.Value
    Body = (new StreamReader(request.Body)).ReadToEnd()
    Headers = Dictionary request.Headers }

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
  ctx.Request
  |> toRequest
  |> requests.Add
  let writeResponse = 
    function 
    | Body(statusCode, body) -> 
      ctx
      |> withStatusCode statusCode
      |> withBody body
    | StatusCode statusCode -> 
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
  match responses |> Seq.tryFind (find ctx) with
  | Some kvp -> writeResponse kvp.Value
  | _ -> 
    ctx.Response.StatusCode <- 404
    send()

let app requests responses (app : IAppBuilder) = Func<_, _>(handler requests responses) |> app.Run
