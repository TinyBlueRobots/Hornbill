namespace Hornbill.FSharp

open Hornbill
open System.Collections.Generic

module Response = 
  let withHeaders statusCode (headers : seq<string * string>) = Response.WithHeaders(statusCode, headers |> Seq.map KeyValuePair)
  let withBody statusCode body = Response.WithBody(statusCode, body)
  let withStatusCode = Response.WithStatusCode
  let withHeadersAndBody statusCode headers body = Response.WithHeadersAndBody(statusCode, headers, body)
  let withResponses = Response.WithResponses
  let withDelegate = Response.Dlg
  let withRawResponse = Response.WithRawResponse
  let withFile = Response.WithFile