using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Hornbill;
using Microsoft.Owin.Testing;
using NUnit.Framework;

namespace Tests
{
    public class Tests
    {
        [Test]
        public void Text()
        {
            var fakeService = new FakeService();
            fakeService.AddResponse("/foo", Method.GET, Response.CreateBody(200, "foo"));
            var testServer = TestServer.Create(fakeService.App);
            Assert.That(testServer.HttpClient.GetStringAsync("/foo").Result, Is.EqualTo("foo"));
            Assert.That(fakeService.Requests.First().Method, Is.EqualTo(Method.GET));
        }

        [Test]
        public void Regex()
        {
            var fakeService = new FakeService();
            fakeService.AddResponse("/foo/[\\d]+", Method.GET, Response.CreateStatusCode(200));
            var testServer = TestServer.Create(fakeService.App);
            Assert.That(testServer.HttpClient.GetAsync("/foo/123").Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public void Status()
        {
            var fakeService = new FakeService();
            var testServer = TestServer.Create(fakeService.App);
            fakeService.AddResponse("/boom", Method.GET, Response.CreateStatusCode(500));
            Assert.That(testServer.HttpClient.GetAsync("/boom").Result.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public void NotFound()
        {
            var fakeService = new FakeService();
            var testServer = TestServer.Create(fakeService.App);
            Assert.That(testServer.HttpClient.GetAsync("/boom").Result.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public void Hosted_Full()
        {
            using (var fakeService = new FakeService())
            {
                const string link = "http://foo/bar";
                var links = new KeyValuePair<string, string>("Link", link);
                fakeService.AddResponse("/headers", Method.GET, Response.CreateFull(200, new[] { links }, "body"));
                var host = fakeService.Host();
                var httpClient = new HttpClient { BaseAddress = new Uri(host) };
                httpClient.DefaultRequestHeaders.Add("Foo", "Bar");
                var result = httpClient.GetAsync("/headers").Result;
                Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.AreEqual(link, result.Headers.GetValues("Link").Single());
                var linkHeader = fakeService.Requests.First().Headers.Single(x => x.Key == "Foo").Value;
                Assert.That(linkHeader, Is.EqualTo(new [] {"Bar"}));
            }
        }
    }
}
