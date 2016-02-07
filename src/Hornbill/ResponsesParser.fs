module ResponsesParser

open System.Text.RegularExpressions
open System
open Hornbill
open Hornbill.FSharp

let private methodAndPath s = 
  let mtch = Regex.Match(s, "^(GET|POST|PUT|OPTIONS|HEAD|DELETE|TRACE)\s(.+)")
  mtch.Groups.[1].Value, mtch.Groups.[2].Value

type private State = 
  | Empty
  | MethodAndPath
  | StatusCode
  | Body

type private PartialReqRep = 
  { Path : string option
    StatusCode : int option
    Headers : (string * string) list option
    Body : string option
    Method : string option
    State : State }

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
      let partialReqRep = 
        match lines.Head with
        | "" when partialReqRep.State = Empty -> partialReqRep //consume empty lines
        | line when partialReqRep.State = Empty -> 
          let methd, path = methodAndPath line
          { partialReqRep with Path = Some path
                               Method = Some methd
                               State = MethodAndPath }
        | line when partialReqRep.State = MethodAndPath -> 
          { partialReqRep with StatusCode = 
                                 Regex.Match(line, "\d{3}").Value
                                 |> int
                                 |> Some
                               State = StatusCode }
        | line when partialReqRep.State = StatusCode && line.Length > 0 -> 
          let header = Regex.Match(line, "([^\s]+?)\s*:\s*([^\s]+)") |> fun x -> x.Groups.[1].Value, x.Groups.[2].Value
          let headers = 
            match partialReqRep.Headers with
            | None -> [ header ]
            | Some headers -> header :: headers
          { partialReqRep with Headers = Some headers }
        | "" when partialReqRep.State = StatusCode -> { partialReqRep with State = Body } //consume empty line before body
        | "" when partialReqRep.State = Body -> partialReqRep //consume trailing lines
        | line -> 
          let body = 
            match partialReqRep.Body with
            | Some body -> body + line |> Some
            | _ -> Some line
          { partialReqRep with Body = body }
      parse partialReqRep lines.Tail
  Regex.Split(input, "\r?\n\r?\n\r?\n")
  |> Array.map (fun x -> Regex.Split(x, "\r?\n") |> Array.toList)
  |> Array.map (parse { Path = None
                        StatusCode = None
                        Headers = None
                        Body = None
                        Method = None
                        State = Empty })
  |> Array.map mapToParsedReqRep
