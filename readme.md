# Hornbill

Easy http stubs for integration testing

#### Create a fake service

`var fakeService = new FakeService()`

#### Add some responses

`fakeService.AddResponse` requires a path, a method, and a `Response`

##### Status Code

`fakeService.AddResponse("/foo", Method.GET, Response.WithStatusCode(200))`

##### Body

`fakeService.AddResponse("/foo", Method.GET, Response.WithBody(200, "body"))`

##### Headers

`fakeService.AddResponse("/foo", Method.GET, Response.WithHeaders(200, new Dictionary<string, string> {["foo"] = "bar" }))`

##### Headers and body

`fakeService.AddResponse("/foo", Method.GET, Response.WithHeadersAndBody(200, new Dictionary<string, string> {["foo"] = "bar" }, "body"))`

##### Queue of responses

`fakeService.AddResponse("/foo", Method.GET, Response.WithResponses(new [] { Response.WithStatusCode(200), Response.WithStatusCode(500)}))`

##### Delegate

`fakeService.AddResponse("/foo", Method.GET, Response.WithDelegate(x => x.Query["foo"].Contains("bar") ? Response.WithStatusCode(200) : Response.WithStatusCode(404)))`

##### Raw

Requires a string in this format
```
200
foo: bar

Body
```
`fakeService.AddResponse("/foo", Method.GET, Response.WithRawResponse(Resources.rawResponse))`

##### File

The same as raw, but takes a file name instead of a string

`fakeService.AddResponse("/", Method.GET, Response.WithFile(".\\Resources\\rawResponse.txt"));`

#### Self hosting
Service will be hosted on a random available port

`var address = fakeService.Start()`

#### Requests
You can examine the requests sent to your service via `fakeService.Requests`
