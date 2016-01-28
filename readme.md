![logo](https://dl.dropboxusercontent.com/u/11302680/nuget/hornbill.gif)
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

`fakeService.AddResponse("/foo", Method.GET, Response.WithHeaders(200, "foo:bar", "bing:bong")`

##### Body and Headers 

`fakeService.AddResponse("/foo", Method.GET, Response.WithBodyAndHeaders(200, "body", new Dictionary<string, string> {["foo"] = "bar" }))`

`fakeService.AddResponse("/foo", Method.GET, Response.WithBodyAndHeaders(200, "body", "foo:bar", "bing:bong"))`

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

#### Starting the service

Calling `fakeService.Start()` to host on a random available port. The address is returned as a `string`.

#### Requests

You can examine the requests sent to your service via `fakeService.Requests`

You can execute an `Action<Request>` when a request is received by using `fakeService.OnRequestReceived(request => ...)`

#### Address

The address is returned from `fakeService.Start()` and is also available as a `string` `fakeService.Url` or `Uri` `fakeService.Uri`

#### Stopping the service

FakeService implements `IDisposable` but you can also call `fakeService.Stop()`
