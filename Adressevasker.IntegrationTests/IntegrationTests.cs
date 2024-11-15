using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Diagnostics;
using System.Threading;
using System.Security.Policy;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Adressevasker.IntegrationTests
{
    [TestClass]
    public class IntegrationTests
    {
        private Process _apiProcess;
        private HttpClient _httpClient;

        [TestInitialize]
        public void StartApi()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --project C:/Users/Jonat/source/repos/AddressVasker/LocalAPI/LocalAPI.csproj",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _apiProcess = Process.Start(startInfo);

            Thread.Sleep(10000); 
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://10.102.0.65:5128")
            };
        }

        [TestCleanup]
        public void StopApi()
        {
            if (_apiProcess != null && !_apiProcess.HasExited)
            {
                _apiProcess.Kill();
            }
        }

        [TestMethod]
        public async Task Test_ApiEndpoint_PrintJsonResponse()
        {
            // Act
            var response = await _httpClient.GetAsync("");
            var json = await response.Content.ReadAsStringAsync();

            // Assert
            StringAssert.Contains(json, "0a3f50a1-52f0-32b8-e044-0003ba298018");
        }

    }

    [TestClass]
    public class DatabaseServiceTest
    {
        

    }
}