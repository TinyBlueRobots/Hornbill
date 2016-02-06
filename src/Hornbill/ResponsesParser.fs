module ResponsesParser

open System.Text.RegularExpressions
open System
open Hornbill
open Hornbill.FSharp

let methodAndPath s = 
  let mtch = Regex.Match(s, "^(GET|POST|PUT|OPTIONS|HEAD|DELETE|TRACE)\s(.+)")
  mtch.Groups.[1].Value, mtch.Groups.[2].Value

type PartialRequest = 
  { Path : string option
    StatusCode : int option
    Headers : (string * string) list option
    Body : string option
    Method : string option }

type ParsedRequest = 
  { Path : string
    StatusCode : int
    Headers : (string * string) list option
    Body : string option
    Method : Method }

let map (x : PartialRequest) : ParsedRequest = 
  { Body = x.Body
    StatusCode = x.StatusCode.Value
    Headers = x.Headers
    Path = x.Path.Value
    Method = Enum.Parse(typeof<Method>, x.Method.Value) :?> Method }

let parseApi input = 
  let rec parse (request : PartialRequest) lines = 
    if lines = [] then request
    else 
      let request = 
        match lines.Head with
        | "" when request.Path = None -> request
        | line when request.Path = None -> 
          let methd, path = methodAndPath line
          { request with Path = Some path
                         Method = Some methd }
        | line when request.StatusCode = None -> 
          { request with StatusCode = 
                           Regex.Match(line, "\d{3}").Value
                           |> int
                           |> Some }
        | "" when request.Body = None -> { request with Body = Some "" }
        | line when request.Body = None -> 
          let header = Regex.Match(line, "([^\s]+?)\s*:\s*([^\s]+)") |> fun x -> x.Groups.[1].Value, x.Groups.[2].Value
          
          let headers = 
            match request.Headers with
            | None -> [ header ]
            | Some headers -> header :: headers
          { request with Headers = Some headers }
        | "" when request.Body.IsSome -> request
        | line -> 
          let body = 
            match request.Body with
            | Some body -> body + line |> Some
            | _ -> Some line
          { request with Body = body }
      parse request lines.Tail
  Regex.Split(input, "\r?\n\r?\n\r?\n")
  |> Array.map (fun x -> Regex.Split(x, "\r?\n") |> Array.toList)
  |> Array.map (parse { Path = None
                        StatusCode = None
                        Headers = None
                        Body = None
                        Method = None })
  |> Array.map map

let (|RequestWithBodyAndHeaders|RequestWithBody|RequestWithHeaders|Request|) (parsedRequest : ParsedRequest) = 
  match parsedRequest with
  | { Body = None; Headers = None } -> Request
  | { Body = None } -> RequestWithHeaders
  | { Headers = None } -> RequestWithBody
  | _ -> RequestWithBodyAndHeaders

let mapToResponse parsedRequest = 
  match parsedRequest with
  | Request -> Response.withStatusCode parsedRequest.StatusCode
  | RequestWithHeaders -> Response.withHeaders parsedRequest.StatusCode parsedRequest.Headers.Value
  | RequestWithBody -> Response.withBody parsedRequest.StatusCode parsedRequest.Body.Value
  | RequestWithBodyAndHeaders -> 
    Response.withBodyAndHeaders parsedRequest.StatusCode parsedRequest.Body.Value parsedRequest.Headers.Value
