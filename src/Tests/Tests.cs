using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Hornbill;
using NUnit.Framework;

namespace Tests
{
    public class Tests
    {
        HttpClient HttpClient(string uri)
        {
            return new HttpClient { BaseAddress = new Uri(uri) };
        }

        [Test]
        public void Text()
        {
            using (var fakeService = new FakeService())
            using (var httpClient = HttpClient(fakeService.Host()))
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
            using (var httpClient = HttpClient(fakeService.Host()))
            {
                fakeService.AddResponse("/foo/1234", Method.GET, Response.WithStatusCode(200));
                fakeService.AddResponse("/foo/[\\d]+/boom", Method.GET, Response.WithStatusCode(500));
                Assert.That(httpClient.GetAsync("/foo/1234/boom").Result.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            }
        }

        [Test]
        public void Status()
        {
            using (var fakeService = new FakeService())
            using (var httpClient = HttpClient(fakeService.Host()))
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
            using (var httpClient = HttpClient(fakeService.Host()))
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
            using (var httpClient = HttpClient(fakeService.Host()))
            {
                fakeService.AddResponse("/test", Method.GET, Response.WithDelegate(x => x.Query["foo"].Contains("bar") ? Response.WithStatusCode(200) : Response.WithStatusCode(404)));
                Assert.That(httpClient.GetAsync("/test?foo=bar").Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(httpClient.GetAsync("/test?foo=baz").Result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        [Test]
        public void Headers()
        {
            using (var fakeService = new FakeService())
            using (var httpClient = HttpClient(fakeService.Host()))
            {
                fakeService.AddResponse("/test", Method.GET, Response.WithHeaders(200, new Dictionary<string, string> {["foo"] = "bar" }));
                var result = httpClient.GetAsync("/test").Result;
                Assert.That(result.Headers.First().Key, Is.EqualTo("foo"));
                Assert.That(result.Headers.First().Value.First(), Is.EqualTo("bar"));
            }
        }

        [Test]
        public void NotFound()
        {
            using (var fakeService = new FakeService())
            using (var httpClient = HttpClient(fakeService.Host()))
            {
                Assert.That(httpClient.GetAsync("/boom").Result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(fakeService.Requests.First().Path, Is.EqualTo("/boom"));
            }
        }

        [Test]
        public void Responses()
        {
            using (var fakeService = new FakeService())
            using (var httpClient = HttpClient(fakeService.Host()))
            {
                fakeService.AddResponse("/", Method.GET, Response.WithResponses(new[] { Response.WithStatusCode(200), Response.WithStatusCode(500) }));
                Assert.That(httpClient.GetAsync("/").Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(httpClient.GetAsync("/").Result.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
                Assert.That(httpClient.GetAsync("/").Result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        [Test]
        public void Hosted_Full()
        {
            using (var fakeService = new FakeService())
            using (var httpClient = HttpClient(fakeService.Host()))
            {
                const string link = "http://foo/bar";
                fakeService.AddResponse("/headers", Method.GET, Response.WithHeadersAndBody(200, new Dictionary<string, string> {["Link"] = link }, "body"));
                var host = fakeService.Host();
                httpClient.DefaultRequestHeaders.Add("Foo", "Bar");
                var result = httpClient.GetAsync("/headers").Result;
                Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.AreEqual(link, result.Headers.GetValues("Link").Single());
                var linkHeader = fakeService.Requests.First().Headers.Single(x => x.Key == "Foo").Value;
                Assert.That(linkHeader, Is.EqualTo(new[] { "Bar" }));
            }
        }

        [Test]
        public void Same_path_different_method()
        {
            using (var fakeService = new FakeService())
            using (var httpClient = HttpClient(fakeService.Host()))
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
            using (var httpClient = HttpClient(fakeService.Host()))
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
    }
}
