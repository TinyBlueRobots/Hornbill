module internal HttpContext

open System
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives
open Hornbill
open System.IO

let toMethod m = Enum.Parse(typeof<Method>, m) :?> Method
let requestMethod (ctx : HttpContext) = ctx.Request.Method |> toMethod
let requestPath (ctx : HttpContext) = ctx.Request.Path.Value
let requestUri (ctx : HttpContext) = sprintf "%s%s" ctx.Request.Path.Value ctx.Request.QueryString.Value
let responseHeaders (ctx : HttpContext) = ctx.Response.Headers
let writeResponseBody (body : string) (ctx : HttpContext) = ctx.Response.WriteAsync body
let writeResponseBytes (body : byte array) (ctx : HttpContext) = ctx.Response.Body.WriteAsync(body, 0, body.Length)

let toRequest (ctx : HttpContext) =
  { Method = ctx.Request.Method |> toMethod
    Path = ctx |> requestPath
    Body = (new StreamReader(ctx.Request.Body)).ReadToEnd()
    Headers = ctx.Request.Headers |> Seq.map (fun (KeyValue (k,v)) -> k, v.ToArray()) |> dict
    Query = ctx.Request.Query |> Seq.map (fun (KeyValue(k, v)) -> k, v.ToArray()) |> dict }

let responseKey ctx = ctx |> requestUri, ctx |> requestMethod

let withStatusCode statusCode (ctx : HttpContext) =
  ctx.Response.StatusCode <- statusCode
  ctx

let withHeaders headers ctx =
  let ctxHeaders = ctx |> responseHeaders
  headers |> Seq.iter (fun (k, v : string) -> ctxHeaders.Add(k, StringValues v))
  ctx
