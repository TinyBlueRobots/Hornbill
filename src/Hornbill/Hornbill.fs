namespace Hornbill

open System.Collections.Generic
open System
open System.Text.RegularExpressions

type internal StatusCode = int

type internal Headers = KeyValuePair<string, string> seq

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

type Response = 
  internal
  | Body of StatusCode * Body
  | StatusCode of StatusCode
  | Headers of StatusCode * Headers
  | HeadersAndBody of StatusCode * Headers * Body
  | Raw of string list
  | Responses of Response list
  | Delegate of (Request -> Response)
  static member WithBody(statusCode, body) = Body(statusCode, body)
  static member WithStatusCode statusCode = StatusCode statusCode
  static member WithHeaders(statusCode, headers) = Headers(statusCode, headers)
  static member WithHeadersAndBody(statusCode, headers, body) = HeadersAndBody(statusCode, headers, body)
  static member WithResponses responses = responses |> Array.toList |> Responses
  static member WithDelegate (func : Func<Request, Response>) = Delegate (fun request -> func.Invoke request)
  static member WithRawResponse (response : string) =
    let lines = response.Split([| Environment.NewLine |], StringSplitOptions.None) |> Array.toList
    let (firstLine :: lines) = lines
    let statusCode = Regex.Match(firstLine,"\s\d+\s").Value |> int
    let rec parseHeaders headers (lines : string list) =
      match lines with
      | line :: lines when line.Contains ":" ->
        let values = line.Split ':' |> Array.map (fun x -> x.Trim())
        parseHeaders ((values.[0], values.[1]) :: headers) lines
      | _ -> headers |> dict
    let headers = parseHeaders [] lines
    let body = lines |> List.skipWhile (fun x -> x <> "") |> List.reduce (+)
    HeadersAndBody(statusCode, headers, body)

