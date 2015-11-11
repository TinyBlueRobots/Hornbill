module internal Middleware

open Owin
open System
open System.Threading.Tasks
open Hornbill
open OWinContext

let send _ = Task.Delay 0

let responseHandler ctx = 
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
  | _ -> 
    ctx
    |> withStatusCode 404
    |> send

let handler storeRequest findResponse setResponse ctx = 
  let request = ctx |> toRequest
  storeRequest request
  let key = ctx |> responseKey
  match findResponse key with
  | Some(Responses(response :: responses)) -> 
    Responses responses |> setResponse key
    response
  | Some(Delegate dlg) -> dlg request
  | Some response -> response
  | _ -> Response.WithStatusCode 404
  |> responseHandler ctx

let app storeRequest findResponse setResponse (app : IAppBuilder) = 
  Func<_, _>(handler storeRequest findResponse setResponse) |> app.Run
