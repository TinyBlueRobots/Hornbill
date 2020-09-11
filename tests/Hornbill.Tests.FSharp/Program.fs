module Program

open System
open Hornbill.FSharp
open System.Net.Http
open System.Net
open Hornbill
open System.Threading
open Expecto

type TestService() =
  let fakeService = new FakeService()
  do fakeService.Start() |> ignore

  let httpClient =
    new HttpClient(BaseAddress = fakeService.Uri)
  with
    member __.Service = fakeService

    member __.Client = httpClient

    interface IDisposable with
      member __.Dispose() =
        fakeService.Dispose()
        httpClient.Dispose()

let (==) actual expected = Expect.equal actual expected ""

let body () =
  use testService = new TestService()

  Response.withBody 200 "foo"
  |> testService.Service.AddResponse "/foo" Method.GET

  testService.Client.GetStringAsync("/foo").Result
  == "foo"

let headers () =
  use testService = new TestService()

  Response.withHeaders 200 [ "foo", "bar" ]
  |> testService.Service.AddResponse "/foo" Method.GET

  let response =
    testService.Client.GetAsync("/foo").Result

  response.Headers.GetValues("foo")
  |> Seq.head
  == "bar"

let dlg () =
  use testService = new TestService()

  Response.withDelegate (fun _ -> Response.withStatusCode 200)
  |> testService.Service.AddResponse "/foo" Method.GET

  testService.Client.GetAsync("/foo").Result.StatusCode
  == HttpStatusCode.OK

let evnt () =
  use testService = new TestService()

  Response.WithStatusCode 200
  |> testService.Service.AddResponse "/foo" Method.GET

  let autoResetEvent = new AutoResetEvent false
  testService.Service.OnRequestReceived(fun x -> if x.Path = "/foo" then autoResetEvent.Set() |> ignore)

  testService.Client.GetAsync("/foo").Result.StatusCode
  == HttpStatusCode.OK

  autoResetEvent.WaitOne 1000 == true

let ``parsing responses supports all methods`` methd () =
  let text = """
GET statuscode
200
"""
  let text = text.Replace("GET", methd)
  use testService = new TestService()
  testService.Service.AddResponsesFromText text

  let response =
    (new HttpRequestMessage(HttpMethod methd, "statuscode")
     |> testService.Client.SendAsync)
      .Result

  response.StatusCode == HttpStatusCode.OK

[<Tests>]
let tests =

  let parserTests =
    [ "GET"
      "POST"
      "PUT"
      "OPTIONS"
      "HEAD"
      "DELETE"
      "TRACE"
      "PATCH" ]
    |> List.map (fun name -> name, ``parsing responses supports all methods`` name)
    |> List.map (fun (name, test) -> testCase <| sprintf "parser %s" name <| test)

  [ testCase "body" body
    testCase "headers" headers
    testCase "dlg" dlg
    testCase "evnt" evnt ]
  @ parserTests
  |> testList "tests"

[<EntryPoint>]
let main args = runTestsInAssembly defaultConfig args
