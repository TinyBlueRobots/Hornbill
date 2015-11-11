module internal Middleware

open Owin
open System
open System.Threading.Tasks
open Hornbill
open OWinContext

let send _ = Task.Delay 0

let handler storeRequest findResponse setResponse ctx = 
  ctx
  |> toRequest
  |> storeRequest
  let notFound = 
    ctx.Response.StatusCode <- 404
    send
  
  let handleResponse = 
    function 
    | Body(statusCode, body) -> 
      ctx
      |> withStatusCode statusCode
      |> writeResponseBody body
    | StatusCode statusCode -> 
      ctx
      |> withStatusCode statusCode
      |> send
    | Headers(statusCode, headers) -> 
      ctx
      |> withStatusCode statusCode
      |> withHeaders headers
      |> send
    | HeadersAndBody(statusCode, headers, body) -> 
      ctx
      |> withStatusCode statusCode
      |> withHeaders headers
      |> writeResponseBody body
    | _ -> notFound()
  
  let key = ctx |> responseKey
  match findResponse key with
  | Some(Responses(response :: responses)) -> 
    Responses responses |> setResponse key
    handleResponse response
  | Some response -> handleResponse response
  | _ -> notFound()

let app storeRequest findResponse setResponse (app : IAppBuilder) = 
  Func<_, _>(handler storeRequest findResponse setResponse) |> app.Run
