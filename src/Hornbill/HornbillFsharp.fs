module HornbillFSharp

open System
open System.Collections.Generic
open Hornbill

let addResponse path methd response (fakeService : FakeService) = fakeService.AddResponse(path, methd, response)

module Response = 
  let private mapHeaders (headers : (string * string) seq) = headers |> Seq.map (fun (k, v) -> KeyValuePair<_, _>(k, v))
  let withStatusCode = Response.WithStatusCode
  let withBody statusCode body = Response.WithBody(statusCode, body)
  let withDelegate dlg = Response.WithDelegate(Func<Request, Response> dlg)
  let withHeaders statusCode headers = Response.WithHeaders(statusCode, headers |> mapHeaders)
  let withHeadersAndBody statusCode headers body = Response.WithHeadersAndBody(statusCode, headers |> mapHeaders, body)
  let withFile path = Response.WithFile path
  let withRawResponse raw = Response.WithRawResponse raw
  
  let withResponses responses = 
    responses
    |> List.toArray
    |> Response.WithResponses
