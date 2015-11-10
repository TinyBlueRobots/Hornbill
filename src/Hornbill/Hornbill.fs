namespace Hornbill

open System.Collections.Generic

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
    Headers : Dictionary<string, string array> }

type Response = 
  internal
  | Body of StatusCode * Body
  | StatusCode of StatusCode
  | Headers of StatusCode * Headers
  | HeadersAndBody of StatusCode * Headers * Body
  static member WithBody(statusCode, body) = Body(statusCode, body)
  static member WithStatusCode statusCode = StatusCode statusCode
  static member WithHeaders(statusCode, headers) = Headers(statusCode, headers)
  static member WithHeadersAndBody(statusCode, headers, body) = HeadersAndBody(statusCode, headers, body)
