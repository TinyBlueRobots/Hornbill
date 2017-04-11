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
  | PATCH = 7

type Request = 
  { Method : Method
    Path : string
    Body : string
    Headers : IDictionary<string, string array>
    Query : IDictionary<string, string array> }

[<AutoOpen>]
module private ResponseHelpers = 
  let mapHeaders headers = headers |> Seq.map (fun (KeyValue(k, v)) -> k, v)
  let parseHeader header = 
    Regex.Match(header, "([^\s]+?)\s*:\s*([^\s]+)") |> fun x -> x.Groups.[1].Value, x.Groups.[2].Value
  
  let parseResponse (response : string) = 
    let lines = Regex.Split(response, "\r?\n")
    let statusCode = Regex.Match(lines |> Array.head, "\d{3}").Value |> int
    
    let headers = 
      lines
      |> Array.skip 1
      |> Array.takeWhile ((<>) "")
      |> Array.map parseHeader
      |> dict
    
    let body = 
      lines
      |> Array.skipWhile ((<>) "")
      |> Array.skip 1
      |> String.concat Environment.NewLine
    
    statusCode, body, headers

type Response = 
  internal
  | Body of StatusCode * Body
  | Bytes of StatusCode * byte array
  | BytesAndHeaders of StatusCode * byte array * Headers
  | StatusCode of StatusCode
  | Headers of StatusCode * Headers
  | BodyAndHeaders of StatusCode * Body * Headers
  | Responses of Response list
  | Dlg of (Request -> Response)
  static member WithHeaders(statusCode, [<ParamArray>] headers) = Headers(statusCode, headers |> Array.map parseHeader)
  static member WithHeaders(statusCode, headers) = Headers(statusCode, headers |> mapHeaders)
  static member WithStatusCode statusCode = StatusCode statusCode
  static member WithBody(statusCode, body) = Body(statusCode, body)
  static member WithBytes(statusCode, bytes) = Bytes(statusCode, bytes)
  static member WithBytesAndHeaders(statusCode, bytes, headers) = BytesAndHeaders(statusCode, bytes, headers |> mapHeaders)
  static member WithBytesAndHeaders(statusCode, body, [<ParamArray>] headers) = 
    BytesAndHeaders(statusCode, body, headers |> Array.map parseHeader)
  static member WithBodyAndHeaders(statusCode, body, headers) = BodyAndHeaders(statusCode, body, headers |> mapHeaders)
  static member WithBodyAndHeaders(statusCode, body, [<ParamArray>] headers) = 
    BodyAndHeaders(statusCode, body, headers |> Array.map parseHeader)
  
  static member WithResponses ([<ParamArray>] responses) = 
    responses
    |> Array.toList
    |> Responses
  
  static member WithDelegate(func : Func<Request, Response>) = Dlg func.Invoke
  [<Obsolete "Use FakeService.AddResponsesFromText">]
  static member WithRawResponse response = parseResponse response |> Response.WithBodyAndHeaders
  [<Obsolete "Use FakeService.AddResponsesFromFile">]
  static member WithFile path = File.ReadAllText path |> Response.WithRawResponse
  static member WithStaticFile path = Response.WithBytesAndHeaders(200, File.ReadAllBytes path, [| sprintf "Content-Disposition:attachment; filename=%s" (System.IO.Path.GetFileName path) |] )
