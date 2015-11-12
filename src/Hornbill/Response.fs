module Response

open Hornbill
open System
open System.Text.RegularExpressions
open System.IO

let private mapHeaders = Seq.map (fun (KeyValue(k, v)) -> k, v)
let WithStatusCode statusCode = StatusCode statusCode
let WithBody statusCode body = Body(statusCode, body)
let WithHeaders statusCode headers = Headers(statusCode, headers |> mapHeaders)
let WithHeadersAndBody statusCode headers body = HeadersAndBody(statusCode, headers |> mapHeaders, body)

let WithResponses responses = 
  responses
  |> Array.toList
  |> Responses

let WithDelegate(func : Func<Request, Response>) = Dlg func.Invoke

let WithRawResponse(response : string) = 
  let lines = Regex.Split(response, "\r?\n")
  let statusCode = Regex.Match(lines |> Array.head, "\d{3}").Value |> int
  
  let headers = 
    lines
    |> Array.skip 1
    |> Array.takeWhile ((<>) "")
    |> Array.map (fun x -> x.Split ':')
    |> Array.map (fun [| key; value |] -> key.Trim(), value.Trim())
    |> dict
  
  let body = 
    lines
    |> Array.skipWhile ((<>) "")
    |> Array.skip 1
    |> String.concat Environment.NewLine
  
  WithHeadersAndBody statusCode headers body

let WithFile path = File.ReadAllText path |> WithRawResponse
