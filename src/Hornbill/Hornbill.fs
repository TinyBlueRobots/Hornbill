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
  | Text of Body
  | Code of StatusCode
  | Headers of StatusCode * Headers
  | Full of StatusCode * Headers * Body
  static member CreateText body = Text body
  static member CreateCode statusCode = Code statusCode
  static member CreateHeaders(statusCode, headers) = Headers(statusCode, headers)
  static member CreateFull(statusCode, headers, body) = Full(statusCode, headers, body)