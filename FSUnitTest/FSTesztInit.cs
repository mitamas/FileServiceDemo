using FluentAssertions;
using FS;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Xunit;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerClass, DisableTestParallelization = true, MaxParallelThreads = 1)]
namespace FSUnitTest
{
    public class FSTeszt : ApiFix
    {
        public FSTeszt(ApiWebApplicationFactory fixture) : base(fixture) { }

        private List<string> _allFiles;
        public async Task<List<string>> GetAllAsync(bool cache)
        {
            if (!cache || _allFiles == null)
            {
                var response = await _client.GetAsync("/api/dokumentumok");
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var res = await response.Content.ReadAsStringAsync();
                _allFiles = (JsonSerializer.Deserialize(res, typeof(string[])) as string[]).ToList();
            }
            return _allFiles;
        }

        protected async Task<HttpResponseMessage> Post(string fileName)
        {
            string body = Convert.ToBase64String(Encoding.ASCII.GetBytes(fileName));
            var content = new StringContent(body);
            var name = HttpUtility.UrlEncode(fileName);
            return await _client.PostAsync($"/api/dokumentumok/{name}", content);
        }
    }
    public class FSTeszt_01 : FSTeszt
    {
        public FSTeszt_01(ApiWebApplicationFactory fixture) : base(fixture) { }

        [Theory]
        [InlineData("t1.txt")]
        [InlineData("f1\\t2.txt")]
        public async Task T01_EnsureDataAsync(string fileName)
        {
            var response = await Post(fileName);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var err = await response.Content.ReadAsStringAsync();
                Assert.True(err == FS.Controllers.DokumentumokController.ErrorExists);
            }
        }
    }
    public class FSTeszt_02_Set : FSTeszt
    {
        public FSTeszt_02_Set(ApiWebApplicationFactory fixture) : base(fixture) { }

        [Fact]
        public async Task T01_Set_ValidFileAsync()
        {
            string fileName;
            var arr = await GetAllAsync(false);
            do
            {
                fileName = $"rnd\\{Path.GetRandomFileName()}";
            } while (arr.Contains(fileName));

            var response = await Post(fileName);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Theory]
        [InlineData("t1.txt:")]
        [InlineData("..\\t2.txt")]
        public async Task T02_Set_InvalidFileAsync(string fileName)
        {
            var response = await Post(fileName);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var err = await response.Content.ReadAsStringAsync();
            Assert.True(err == FS.Controllers.DokumentumokController.ErrorName);
        }
    }

    public class FSTeszt_03_Get : FSTeszt
    {
        public FSTeszt_03_Get(ApiWebApplicationFactory fixture) : base(fixture) { }

        [Fact]
        public async Task T01_Get_AllFileNameAsync()
        {
            var arr = await GetAllAsync(false);
            arr.Should().HaveCountGreaterThan(0);
        }

        [Theory]
        [InlineData("t1.txt")]
        [InlineData("f1\\t2.txt")]
        public async Task T02_Get_ValidFileAsync(string fileName)
        {
            var name = HttpUtility.UrlEncode(fileName);
            var response = await _client.GetAsync($"/api/dokumentumok/{name}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var base64String = await response.Content.ReadAsStringAsync();
            var data = Convert.FromBase64String(base64String);
            var str = Encoding.UTF8.GetString(data);
            str.Should().Equals(fileName);
        }

        [Fact]
        public async Task T03_Get_MissingFileAsync()
        {
            string fileName;
            var arr = await GetAllAsync(false);
            do
            {
                fileName = $"rnd\\{Path.GetRandomFileName()}";
            } while (arr.Contains(fileName));

            var name = HttpUtility.UrlEncode(fileName);
            var response = await _client.GetAsync($"/api/dokumentumok/{name}");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var err = await response.Content.ReadAsStringAsync();
            Assert.True(err == FS.Controllers.DokumentumokController.ErrorMiss);
        }
    }
}
