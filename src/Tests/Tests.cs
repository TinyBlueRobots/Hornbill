using System.Net;
using Hornbill;
using Microsoft.Owin.Testing;
using NUnit.Framework;

namespace Tests
{
    public class Tests
    {
        [Test]
        public void _200()
        {
            var fakeService = new FakeService();
            var testServer = TestServer.Create(fakeService.App);
            fakeService.AddResponse("/foo", Response.Create(200, "foo"));
            Assert.AreEqual("foo", testServer.HttpClient.GetStringAsync("/foo").Result);
        }

        [Test]
        public void _500()
        {
            var fakeService = new FakeService();
            var testServer = TestServer.Create(fakeService.App);
            fakeService.AddResponse("/boom", Response.Create(500));
            Assert.AreEqual(HttpStatusCode.InternalServerError, testServer.HttpClient.GetAsync("/boom").Result.StatusCode);
        }
    }
}
