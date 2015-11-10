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
  | Full of StatusCode * Headers * Body
  static member CreateBody(statusCode, body) = Body(statusCode, body)
  static member CreateStatusCode statusCode = StatusCode statusCode
  static member CreateHeaders(statusCode, headers) = Headers(statusCode, headers)
  static member CreateFull(statusCode, headers, body) = Full(statusCode, headers, body)
