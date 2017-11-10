using System;
using Hornbill;
using Expecto;
using Expecto.CSharp;
using System.Net.Http;
using System.Net;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace Hornbill.Tests.CSharp
{
  public class Program
  {
    static HttpClient HttpClient(string uri) => new HttpClient { BaseAddress = new Uri(uri) };

    static void Body()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/foo", Method.GET, Response.WithBody(200, "foo"));
        Expect.equal(httpClient.GetStringAsync("/foo").Result, "foo", "GET returns foo");
        Expect.equal(fakeService.Requests.First().Method, Method.GET, "First request is GET");
      }
    }

    static void ReplaceResponse()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/foo", Method.GET, Response.WithBody(200, "foobar"));
        fakeService.AddResponse("/foo", Method.GET, Response.WithBody(200, "foo"));
        Expect.equal(httpClient.GetStringAsync("/foo").Result, "foo", "GET returns foo");
        Expect.equal(fakeService.Requests.First().Method, Method.GET, "First request is GET");
      }
    }

    static void Regex()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/foo/1234", Method.GET, Response.WithStatusCode(200));
        fakeService.AddResponse("/foo/[\\d]+/boom\\?this=that", Method.GET, Response.WithStatusCode(500));
        Expect.equal(httpClient.GetAsync("/foo/1234/boom?this=that").Result.StatusCode, HttpStatusCode.InternalServerError, "InternalServerError is returned");
      }
    }

    static void Status()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/boom", Method.GET, Response.WithStatusCode(500));
        Expect.equal(httpClient.GetAsync("/boom").Result.StatusCode, HttpStatusCode.InternalServerError, "InternalServerError is returned");
        Expect.equal(fakeService.Requests.Count, 1, "Service received one request");
      }
    }

    static void Delegate()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/test\\?foo=bar", Method.GET, Response.WithDelegate( x => x.Query["foo"].Contains("bar") ? Response.WithStatusCode(200) : Response.WithStatusCode(404)));
        Expect.equal(httpClient.GetAsync("/test?foo=bar").Result.StatusCode, HttpStatusCode.OK, "OK is returned");
        Expect.equal(httpClient.GetAsync("/test?foo=baz").Result.StatusCode, HttpStatusCode.NotFound, "NotFound is returned");
      }
    }

    static void Headers()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/test", Method.GET, Response.WithHeaders(200, new Dictionary<string, string> {["foo"] = "bar"}));
        var result = httpClient.GetAsync("/test").Result;
        Expect.equal(result.Headers.GetValues("foo").First(), "bar", "Headers contains foo: bar");
      }
    }

    static void HeadersParamsWithExtraSpaces()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/test", Method.GET, Response.WithHeaders(200, "foo   :   bar", "bing::bong"));
        var result = httpClient.GetAsync("/test").Result;
        Expect.equal(result.Headers.GetValues("foo").First(), "bar", "Headers contains foo: bar");
        Expect.equal(result.Headers.GetValues("bing").First(), ":bong", "Headers contains bing: bong");
      }
    }

    static void ResponsesFromFile()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponsesFromFile("Responses.txt");

        Expect.equal(httpClient.GetAsync("/statuscode").Result.StatusCode, HttpStatusCode.OK, "Statuscode returns OK");

        Expect.equal(httpClient.GetAsync("/headers").Result.Headers.GetValues("foo").First(), "bar", "Headers has foo: bar header");
        Expect.equal(httpClient.GetAsync("/headers").Result.Headers.GetValues("bing").First(), "bong", "Headers has bing: bong header");

        Expect.equal(httpClient.GetAsync("/body").Result.Content.ReadAsStringAsync().Result, "bodytext", "Body text returned");

        Expect.equal(httpClient.GetAsync("/bodyandheaders").Result.Content.ReadAsStringAsync().Result, "body", "BodyAndHeaders has body");
        Expect.equal(httpClient.GetAsync("/bodyandheaders").Result.Headers.GetValues("foo").First(), "bar", "BodyAndHeaders has foo: bar header");
      }
    }

    static void NotFound()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        Expect.equal(httpClient.GetAsync("/boom").Result.StatusCode, HttpStatusCode.NotFound, "NotFound is returned");
        Expect.equal(fakeService.Requests.First().Path, "/boom", "Path is recorded");
      }
    }

    static void Responses()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/", Method.GET, Response.WithResponses(new[] {Response.WithStatusCode(200), Response.WithStatusCode(500)}));
        Expect.equal(httpClient.GetAsync("/").Result.StatusCode, HttpStatusCode.OK, "OK is returned");
        Expect.equal(httpClient.GetAsync("/").Result.StatusCode, HttpStatusCode.InternalServerError, "InternalServerError is returned");
        Expect.equal(httpClient.GetAsync("/").Result.StatusCode, HttpStatusCode.NotFound, "NotFound is returned");
      }
    }

    static void HeadersDictionaryAndBody()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        const string link = "http://foo/bar";
        fakeService.AddResponse("/headers", Method.GET, Response.WithBodyAndHeaders(200, "body", new Dictionary<string, string> {["Link"] = link}));
        httpClient.DefaultRequestHeaders.Add("Foo", "Bar");
        var result = httpClient.GetAsync("/headers").Result;
        Expect.equal(result.StatusCode, HttpStatusCode.OK, "OK is returned");
        Expect.equal(result.Headers.GetValues("Link").Single(), link, "Headers contains Link: http://foo/bar");
        Expect.equal(fakeService.Requests.First().Headers["Foo"].First(), "Bar", "Headers contains Foo: Bar");
        Expect.equal(result.Content.ReadAsStringAsync().Result, "body", "Body = body");
      }
    }

    static void HeadersStringAndBody()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/headers", Method.GET, Response.WithBodyAndHeaders(200, "body", "foo :  bar"));
        var result = httpClient.GetAsync("/headers").Result;
        Expect.equal(result.StatusCode, HttpStatusCode.OK, "OK is returned");
        Expect.equal(result.Content.ReadAsStringAsync().Result, "body", "Body = body");
        Expect.equal(result.Headers.GetValues("foo").First(), "bar", "Headers contains foo: bar");
      }
    }

    static void SamePathWithDifferentMethod()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/", Method.GET, Response.WithStatusCode(200));
        fakeService.AddResponse("/", Method.HEAD, Response.WithStatusCode(404));
        Expect.equal(httpClient.GetAsync("/").Result.StatusCode, HttpStatusCode.OK, "OK is returned");
        var httpRequestMessage = new HttpRequestMessage {Method = HttpMethod.Head};
        Expect.equal(httpClient.SendAsync(httpRequestMessage).Result.StatusCode, HttpStatusCode.NotFound, "NotFound is returned");
      }
    }

    static void File()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponsesFromFile("Responses.txt");

        Expect.equal(httpClient.GetAsync("/statuscode").Result.StatusCode, HttpStatusCode.OK, "Statuscode returns OK");

        Expect.equal(httpClient.GetAsync("/headers").Result.Headers.GetValues("foo").First(), "bar", "Headers has foo: bar header");
        Expect.equal(httpClient.GetAsync("/headers").Result.Headers.GetValues("bing").First(), "bong", "Headers has bing: bong header");

        Expect.equal(httpClient.GetAsync("/body").Result.Content.ReadAsStringAsync().Result, "bodytext", "Body text returned");

        Expect.equal(httpClient.GetAsync("/bodyandheaders").Result.Content.ReadAsStringAsync().Result, "body", "BodyAndHeaders has body");
        Expect.equal(httpClient.GetAsync("/bodyandheaders").Result.Headers.GetValues("foo").First(), "bar", "BodyAndHeaders has foo: bar header");
      }
    }

    static void Host_not_started_exception_thrown_when_getting_uri()
    {
      var fakeService = new FakeService();
      try
      {
        var _ = fakeService.Uri;
      }
      catch (System.Exception ex)
      {
        Expect.equal(ex.Message, "Service not started", "Exception is thrown");
      }
    }

    static void OnRequestReceived()
    {
      var autoResetEvent = new AutoResetEvent(false);
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/foo", Method.GET, Response.WithStatusCode(200));
        fakeService.OnRequestReceived(request =>
        {
          if (request.Path == "/foo")
          {
            autoResetEvent.Set();
          }
        });
        httpClient.GetAsync("/foo").Result.EnsureSuccessStatusCode();
        Expect.equal(autoResetEvent.WaitOne(1000), true, "AutoResetEvent is triggered");
      }
    }

    static void UrlInPath()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/foo/http://ping/pong", Method.GET, Response.WithStatusCode(200));
        Expect.equal(httpClient.GetAsync("/foo/http://ping/pong").Result.StatusCode, HttpStatusCode.OK, "OK is returned");
      }
    }

    static void SetPort()
    {
      const int port = 8889;
      using (var fakeService = new FakeService(port))
      using (var httpClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{port}") })
      {
        fakeService.AddResponse("/foo", Method.GET, Response.WithStatusCode(200));
        fakeService.Start();
        Expect.equal(httpClient.GetAsync("/foo").Result.StatusCode, HttpStatusCode.OK, "OK is returned");
      }
    }

    static void ResponsesParams()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/", Method.GET, Response.WithResponses(Response.WithStatusCode(200), Response.WithStatusCode(500)));
        Expect.equal(httpClient.GetAsync("/").Result.StatusCode, HttpStatusCode.OK, "OK is returned");
        Expect.equal(httpClient.GetAsync("/").Result.StatusCode, HttpStatusCode.InternalServerError, "InternalServerError is returned");
        Expect.equal(httpClient.GetAsync("/").Result.StatusCode, HttpStatusCode.NotFound, "NotFound is returned");
      }
    }

    static void StaticFile(string fileName)
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        var sourceFile = new FileInfo(fileName);
        fakeService.AddResponse($"/Files/{fileName}", Method.GET, Response.WithStaticFile(fileName));
        var httpResponseMessage = httpClient.GetAsync($"/Files/{fileName}").Result;

        Expect.equal(httpResponseMessage.StatusCode, HttpStatusCode.OK, "OK is returned");

        var destinationFileName = Path.GetTempFileName();

        using (var fileStream = new FileStream(destinationFileName, FileMode.Create, FileAccess.Write, FileShare.None) { Position = 0 })
        {
            httpResponseMessage.Content.CopyToAsync(fileStream).Wait();
        }

        var destinationFile = new FileInfo(destinationFileName);
        Expect.isTrue(destinationFile.Exists, "File exists");
        Expect.equal(sourceFile.Length, destinationFile.Length, "File lengths are the same");
      }
    }

    static void Service_can_be_explictly_disposed()
    {
      var fakeService = new FakeService();
      fakeService.Dispose();
    }

    [Tests]
    public static Test tests =
      Runner.TestList("Tests", new Expecto.Test[] {
        Runner.TestCase("Body", () => Body()),
        Runner.TestCase("ReplaceResponse", () => ReplaceResponse()),
        Runner.TestCase("Regex", () => Regex()),
        Runner.TestCase("Status", () => Status()),
        Runner.TestCase("ResponsesFromFile", () => ResponsesFromFile()),
        Runner.TestCase("Delegate", () => Delegate()),
        Runner.TestCase("Headers", () => Headers()),
        Runner.TestCase("HeadersParamsWithExtraSpaces", () => HeadersParamsWithExtraSpaces()),
        Runner.TestCase("NotFound", () => NotFound()),
        Runner.TestCase("Responses", () => Responses()),
        Runner.TestCase("HeadersDictionaryAndBody", () => HeadersDictionaryAndBody()),
        Runner.TestCase("HeadersStringAndBody", () => HeadersStringAndBody()),
        Runner.TestCase("SamePathWithDifferentMethod", () => SamePathWithDifferentMethod()),
        Runner.TestCase("File", () => File()),
        Runner.TestCase("Host_not_started_exception_thrown_when_getting_uri", () => Host_not_started_exception_thrown_when_getting_uri()),
        Runner.TestCase("OnRequestReceived", () => OnRequestReceived()),
        Runner.TestCase("UrlInPath", () => UrlInPath()),
        Runner.TestCase("SetPort", () => SetPort()),
        Runner.TestCase("ResponsesParams", () => ResponsesParams()),
        Runner.TestCase("StaticFile xml", () => StaticFile("XMLFile1.xml")),
        Runner.TestCase("StaticFile zip", () => StaticFile("XMLFile1.zip")),
        Runner.TestCase("Service_can_be_explictly_disposed", () => Service_can_be_explictly_disposed()),
      });

    public static int Main(string[] argv) => Runner.RunTestsInAssembly(Runner.DefaultConfig, argv);
  }
}
