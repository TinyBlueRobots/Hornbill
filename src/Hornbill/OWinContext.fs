module internal OWinContext

open System
open Microsoft.Owin
open Hornbill
open System.IO
open System.Collections.Generic

let toMethod m = Enum.Parse(typeof<Method>, m) :?> Method
let requestMethod (ctx : IOwinContext) = ctx.Request.Method |> toMethod
let requestPath (ctx : IOwinContext) = ctx.Request.Path.Value
let requestUri (ctx : IOwinContext) = sprintf "%s%s" ctx.Request.Path.Value ctx.Request.QueryString.Value
let responseHeaders (ctx : IOwinContext) = ctx.Response.Headers
let writeResponseBody (body : string) (ctx : IOwinContext) = ctx.Response.WriteAsync body

let toRequest (ctx : IOwinContext) = 
  { Method = ctx.Request.Method |> toMethod
    Path = ctx |> requestPath
    Body = (new StreamReader(ctx.Request.Body)).ReadToEnd()
    Headers = Dictionary ctx.Request.Headers
    Query = 
      ctx.Request.Query
      |> Seq.map (fun x -> x.Key, x.Value)
      |> dict }

let responseKey ctx = ctx |> requestUri, ctx |> requestMethod

let withStatusCode statusCode (ctx : IOwinContext) = 
  ctx.Response.StatusCode <- statusCode
  ctx

let withHeaders headers ctx = 
  let ctxHeaders = ctx |> responseHeaders
  headers |> Seq.iter (fun (k, v) -> ctxHeaders.Add(k, [| v |]))
  ctx
