namespace Hornbill.FSharp

open Hornbill
open System.Collections.Generic

module Response = 
  let withHeaders statusCode (headers : seq<string * string>) = Response.WithHeaders(statusCode, headers |> Seq.map KeyValuePair)
  let withBody statusCode body = Response.WithBody(statusCode, body)
  let withStatusCode = Response.WithStatusCode
  let withBodyAndHeaders statusCode body (headers : seq<string * string>) = Response.BodyAndHeaders(statusCode, body, headers)
  let withResponses responses = Response.WithResponses responses
  let withDelegate = Response.Dlg
  let withRawResponse = Response.WithRawResponse
  let withFile = Response.WithFile