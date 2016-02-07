module ResponsesParser

open System.Text.RegularExpressions
open System
open Hornbill
open Hornbill.FSharp

let methodAndPath s = 
  let mtch = Regex.Match(s, "^(GET|POST|PUT|OPTIONS|HEAD|DELETE|TRACE)\s(.+)")
  mtch.Groups.[1].Value, mtch.Groups.[2].Value

type State = Empty | MethodAndPath | StatusCode | Headers | Body

type private PartialReqRep = 
  { Path : string option
    StatusCode : int option
    Headers : (string * string) list option
    Body : string option
    Method : string option }

type internal ParsedReqRep = 
  { Path : string
    StatusCode : int
    Headers : (string * string) list option
    Body : string option
    Method : Method }

let private mapToParsedReqRep (partialReqRep : PartialReqRep) : ParsedReqRep = 
  { Body = partialReqRep.Body
    StatusCode = partialReqRep.StatusCode.Value
    Headers = partialReqRep.Headers
    Path = partialReqRep.Path.Value
    Method = Enum.Parse(typeof<Method>, partialReqRep.Method.Value) :?> Method }

let private (|RequestWithBodyAndHeaders|RequestWithBody|RequestWithHeaders|Request|) (parsedReqRep : ParsedReqRep) = 
  match parsedReqRep with
  | { Body = None; Headers = None } -> Request
  | { Body = None } -> RequestWithHeaders
  | { Headers = None } -> RequestWithBody
  | _ -> RequestWithBodyAndHeaders

let internal mapToResponse parsedReqRep = 
  match parsedReqRep with
  | Request -> Response.withStatusCode parsedReqRep.StatusCode
  | RequestWithHeaders -> Response.withHeaders parsedReqRep.StatusCode parsedReqRep.Headers.Value
  | RequestWithBody -> Response.withBody parsedReqRep.StatusCode parsedReqRep.Body.Value
  | RequestWithBodyAndHeaders -> 
    Response.withBodyAndHeaders parsedReqRep.StatusCode parsedReqRep.Body.Value parsedReqRep.Headers.Value

let internal parseApi input = 
  let rec parse (partialReqRep : PartialReqRep) lines = 
    if lines = [] then partialReqRep
    else 
      let request = 
        match lines.Head with
        | "" when partialReqRep.Path = None -> partialReqRep                     //consume empty lines
        | line when partialReqRep.Path = None ->                                //path not set, so line is method and path
          let methd, path = methodAndPath line
          { partialReqRep with Path = Some path
                                      Method = Some methd }
        | line when partialReqRep.StatusCode = None ->                          //next line is status code
          { partialReqRep with StatusCode = Regex.Match(line, "\d{3}").Value |> int |> Some }
        | line when partialReqRep.Body = None && line.Length > 0 ->             //body is empty, line is not empty, so line is header
          let header = Regex.Match(line, "([^\s]+?)\s*:\s*([^\s]+)") |> fun x -> x.Groups.[1].Value, x.Groups.[2].Value
          let headers = 
            match partialReqRep.Headers with
            | None -> [ header ]
            | Some headers -> header :: headers
          { partialReqRep with Headers = Some headers }
        | "" when partialReqRep.Body.IsSome -> partialReqRep                          //consume trailing lines after body
        | line ->                                                         //set body
          let body = 
            match partialReqRep.Body with
            | Some body -> body + line |> Some
            | _ -> Some line
          { partialReqRep with Body = body }
      parse request lines.Tail
  Regex.Split(input, "\r?\n\r?\n\r?\n")
  |> Array.map (fun x -> Regex.Split(x, "\r?\n") |> Array.toList)
  |> Array.map (parse { Path = None
                        StatusCode = None
                        Headers = None
                        Body = None
                        Method = None })
  |> Array.map (fun x -> {x with Body = match x.Body with None | Some "" -> None | _ -> x.Body} )
  |> Array.map mapToParsedReqRep