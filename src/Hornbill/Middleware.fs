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
  | BodyAndHeaders(statusCode, body, headers) -> 
    ctx
    |> withStatusCode statusCode
    |> withHeaders headers
    |> writeResponseBody body
  | _ -> 
    ctx
    |> withStatusCode 404
    |> send

let handler storeRequest findResponse setResponse requestReceived ctx = 
  let request = ctx |> toRequest
  requestReceived request
  storeRequest request
  let key = ctx |> responseKey
  let rec extractResponse =
    function
    | Some(Dlg dlg) -> dlg request |> Some |> extractResponse 
    | Some(Responses(response :: responses)) ->
      Responses responses |> setResponse key
      Some response |> extractResponse
    | Some response -> response
    | _ -> Response.WithStatusCode 404
  findResponse key |> extractResponse |> responseHandler ctx

let app storeRequest findResponse setResponse requestReceived (app : IAppBuilder) = 
  Func<_, _>(handler storeRequest findResponse setResponse requestReceived) |> app.Run
