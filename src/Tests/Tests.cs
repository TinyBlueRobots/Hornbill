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
        public void _Text()
        {
            var fakeService = new FakeService();
            var testServer = TestServer.Create(fakeService.App);
            fakeService.AddResponse("/foo", Response.CreateText("foo"));
            Assert.That(testServer.HttpClient.GetStringAsync("/foo").Result, Is.EqualTo("foo"));
        }

        [Test]
        public void _Status()
        {
            var fakeService = new FakeService();
            var testServer = TestServer.Create(fakeService.App);
            fakeService.AddResponse("/boom", Response.CreateCode(500));
            Assert.That(testServer.HttpClient.GetAsync("/boom").Result.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        }

        [Test]
        public void _Full()
        {
            var fakeService = new FakeService();
            var testServer = TestServer.Create(fakeService.App);
            const string link = "<http://localhost:30910/recons/96d45bab-bdc5-414e-a5cd-d31252dede0a>;rel=\"http://schemas.ctmers.com/quoting/recons\"";
            var links = new KeyValuePair<string, string>("Link", link);
            fakeService.AddResponse("/headers", Response.CreateFull(200, new [] { links }, "body"));

            var result = testServer.HttpClient.GetAsync("/headers").Result;

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.AreEqual(link, result.Headers.GetValues("Link").Single());
        }

    }
}
