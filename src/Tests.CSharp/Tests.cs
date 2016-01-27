using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Hornbill;
using NUnit.Framework;
using System.Threading;

namespace Tests.CSharp
{
  public class Tests
  {
    HttpClient HttpClient(string uri)
    {
      return new HttpClient { BaseAddress = new Uri(uri) };
    }

    [Test]
    public void Body()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/foo", Method.GET, Response.WithBody(200, "foo"));
        Assert.That(httpClient.GetStringAsync("/foo").Result, Is.EqualTo("foo"));
        Assert.That(fakeService.Requests.First().Method, Is.EqualTo(Method.GET));
      }
    }

    [Test]
    public void Regex()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/foo/1234", Method.GET, Response.WithStatusCode(200));
        fakeService.AddResponse("/foo/[\\d]+/boom?this=that", Method.GET, Response.WithStatusCode(500));
        Assert.That(httpClient.GetAsync("/foo/1234/boom?this=that").Result.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
      }
    }

    [Test]
    public void Status()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/boom", Method.GET, Response.WithStatusCode(500));
        Assert.That(httpClient.GetAsync("/boom").Result.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(fakeService.Requests.Count, Is.EqualTo(1));
      }
    }


    [Test]
    public void RawResponse()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/boom", Method.GET, Response.WithRawResponse(Resources.rawResponse));
        var result = httpClient.GetAsync("/boom").Result;
        var body = result.Content.ReadAsStringAsync().Result;
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var expectedBody = $"Body{Environment.NewLine}Text";
        Assert.That(body, Is.EqualTo(expectedBody));
        Assert.That(result.Headers.GetValues("foo").First(), Is.EqualTo("bar"));
      }
    }

    [Test]
    public void Delegate()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/test?foo=bar", Method.GET, Response.WithDelegate(x => x.Query["foo"].Contains("bar") ? Response.WithStatusCode(200) : Response.WithStatusCode(404)));
        Assert.That(httpClient.GetAsync("/test?foo=bar").Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(httpClient.GetAsync("/test?foo=baz").Result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
      }
    }

    [Test]
    public void Headers()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/test", Method.GET, Response.WithHeaders(200, new Dictionary<string, string> { ["foo"] = "bar" }));
        var result = httpClient.GetAsync("/test").Result;
        Assert.That(result.Headers.First().Key, Is.EqualTo("foo"));
        Assert.That(result.Headers.First().Value.First(), Is.EqualTo("bar"));
      }
    }

    [Test]
    public void Header_params()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/test", Method.GET, Response.WithHeaders(200, "foo   :   bar", "bing::bong"));
        var result = httpClient.GetAsync("/test").Result;
        Assert.That(result.Headers.First().Key, Is.EqualTo("foo"));
        Assert.That(result.Headers.First().Value.First(), Is.EqualTo("bar"));
        Assert.That(result.Headers.ElementAt(1).Key, Is.EqualTo("bing"));
        Assert.That(result.Headers.ElementAt(1).Value.First(), Is.EqualTo(":bong"));
      }
    }

    [Test]
    public void NotFound()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        Assert.That(httpClient.GetAsync("/boom").Result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(fakeService.Requests.First().Path, Is.EqualTo("/boom"));
      }
    }

    [Test]
    public void Responses()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/", Method.GET, Response.WithResponses(new[] { Response.WithStatusCode(200), Response.WithStatusCode(500) }));
        Assert.That(httpClient.GetAsync("/").Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(httpClient.GetAsync("/").Result.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(httpClient.GetAsync("/").Result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
      }
    }

    [Test]
    public void HeadersAndBody()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        const string link = "http://foo/bar";
        fakeService.AddResponse("/headers", Method.GET, Response.WithBodyAndHeaders(200, "body", new Dictionary<string, string> { ["Link"] = link }));
        httpClient.DefaultRequestHeaders.Add("Foo", "Bar");
        var result = httpClient.GetAsync("/headers").Result;
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.AreEqual(link, result.Headers.GetValues("Link").Single());
        var linkHeader = fakeService.Requests.First().Headers.Single(x => x.Key == "Foo").Value;
        Assert.That(linkHeader, Is.EqualTo(new[] { "Bar" }));
      }
    }

    [Test]
    public void HeadersAndBody2()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/headers", Method.GET, Response.WithBodyAndHeaders(200, "body", "foo :  bar"));
        var result = httpClient.GetAsync("/headers").Result;
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(result.Content.ReadAsStringAsync().Result, Is.EqualTo("body"));
        Assert.That(result.Headers.GetValues("foo").First(), Is.EqualTo("bar"));
      }
    }

    [Test]
    public void Same_path_different_method()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/", Method.GET, Response.WithStatusCode(200));
        fakeService.AddResponse("/", Method.HEAD, Response.WithStatusCode(404));
        Assert.That(httpClient.GetAsync("/").Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var httpRequestMessage = new HttpRequestMessage { Method = HttpMethod.Head };
        Assert.That(httpClient.SendAsync(httpRequestMessage).Result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
      }
    }

    [Test]
    public void File()
    {
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/", Method.GET, Response.WithFile(".\\Resources\\rawResponse.txt"));
        var result = httpClient.GetAsync("/").Result;
        var body = result.Content.ReadAsStringAsync().Result;
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var expectedBody = $"Body{Environment.NewLine}Text";
        Assert.That(body, Is.EqualTo(expectedBody));
        Assert.That(result.Headers.GetValues("foo").First(), Is.EqualTo("bar"));
      }
    }

    [Test]
    public void Host_not_started_Exception_thrown_when_getting_url()
    {
      var fakeService = new FakeService();
      var ex = Assert.Throws<Exception>(delegate { var x = fakeService.Url; });
      Assert.That(ex.Message, Is.EqualTo("Service not started"));
    }

    [Test]
    public void Host_not_started_exception_thrown_when_getting_uri()
    {
      var fakeService = new FakeService();
      Assert.Throws<Exception>(delegate { var x = fakeService.Uri; }, "Service not started");
      fakeService.Stop();
    }

    [Test]
    public void Request_received_event()
    {
      var autoResetEvent = new AutoResetEvent(false);
      using (var fakeService = new FakeService())
      using (var httpClient = HttpClient(fakeService.Start()))
      {
        fakeService.AddResponse("/foo", Method.GET, Response.WithStatusCode(200));
        fakeService.RequestReceived += (_, a) =>
        {
          if (a.Request.Path == "/foo") { autoResetEvent.Set(); }
        };
        httpClient.GetAsync("/foo").Result.EnsureSuccessStatusCode();
        Assert.That(autoResetEvent.WaitOne(1000), Is.True);
      }
    }
  }
}
