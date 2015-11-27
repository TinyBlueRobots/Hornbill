namespace Hornbill

open System.Collections.Generic
open System
open System.Text.RegularExpressions
open System.IO

type internal StatusCode = int

type internal Headers = (string * string) seq

type internal Body = string

type Method = 
  | DELETE = 0
  | GET = 1
  | HEAD = 2
  | OPTIONS = 3
  | POST = 4
  | PUT = 5
  | TRACE = 6

type Request = 
  { Method : Method
    Path : string
    Body : string
    Headers : IDictionary<string, string array>
    Query : IDictionary<string, string array> }

[<AutoOpen>]
module private ResponseHelpers = 
  let mapHeaders headers = headers |> Seq.map (fun (KeyValue(k, v)) -> k, v)
  let parseHeader header = Regex.Match(header, "([^\s]+)\s*:\s*([^\s]+)") |> fun x -> x.Groups.[1].Value, x.Groups.[2].Value
  
  let parseResponse (response : string) = 
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
    
    statusCode, headers, body

type Response = 
  internal
  | Body of StatusCode * Body
  | StatusCode of StatusCode
  | Headers of StatusCode * Headers
  | HeadersAndBody of StatusCode * Headers * Body
  | Responses of Response list
  | Dlg of (Request -> Response)
  static member WithHeaders(statusCode, [<ParamArray>] headers) = Headers(statusCode, headers |> Array.map parseHeader)
  static member WithHeaders(statusCode, headers) = Headers(statusCode, headers |> mapHeaders)
  static member WithStatusCode statusCode = StatusCode statusCode
  static member WithBody(statusCode, body) = Body(statusCode, body)
  static member WithHeadersAndBody(statusCode, headers, body) = HeadersAndBody(statusCode, headers |> mapHeaders, body)
  static member WithResponses responses = responses |> Array.toList |> Responses
  static member WithDelegate(func : Func<Request, Response>) = Dlg func.Invoke
  static member WithRawResponse response = parseResponse response |> Response.WithHeadersAndBody
  static member WithFile path = File.ReadAllText path |> Response.WithRawResponse
