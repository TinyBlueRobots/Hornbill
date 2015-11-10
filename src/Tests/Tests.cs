using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Hornbill;
using Microsoft.Owin.Hosting;
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
            using (var testServer = WebApp.Start("http://localhost:30099", fakeService.App))
            using (var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:30099") })
            {
                fakeService.AddResponse("/foo", Response.CreateText("foo"));

                Assert.That(httpClient.GetStringAsync("/foo").Result, Is.EqualTo("foo"));
            }


        }

        [Test]
        public void Status()
        {
            var fakeService = new FakeService();
            var testServer = TestServer.Create(fakeService.App);
            fakeService.AddResponse("/boom", Response.CreateCode(500));
            Assert.That(testServer.HttpClient.GetAsync("/boom").Result.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public void Full()
        {
            var fakeService = new FakeService();
            var testServer = TestServer.Create(fakeService.App);
            const string link = "<http://localhost:30910/recons/96d45bab-bdc5-414e-a5cd-d31252dede0a>;rel=\"http://schemas.ctmers.com/quoting/recons\"";
            var links = new KeyValuePair<string, string>("Link", link);
            fakeService.AddResponse("/headers", Response.CreateFull(200, new[] { links }, "body"));

            var result = testServer.HttpClient.GetAsync("/headers").Result;

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.AreEqual(link, result.Headers.GetValues("Link").Single());
        }

        [Test]
        public void Head()
        {
            var fakeService = new FakeService();
            const string link = "<http://localhost:30910/recons/96d45bab-bdc5-414e-a5cd-d31252dede0a>;rel=\"http://schemas.ctmers.com/quoting/recons\"";
            var links = new KeyValuePair<string, string>("Link", link);
            fakeService.AddResponse("/", Response.CreateHeaders(200, new[] { links }));
            using (var testServer = WebApp.Start("http://localhost:30099", fakeService.App))
            using (var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:30099") })
            {
                var result = httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, "/")).Result;
                Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }
        }
    }
}
