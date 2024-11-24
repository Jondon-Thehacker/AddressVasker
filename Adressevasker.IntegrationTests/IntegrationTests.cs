using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Diagnostics;
using System.Threading;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Newtonsoft.Json.Linq;
using Client;
using System.Configuration;

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
                BaseAddress = new Uri("http://192.168.1.111:5128")
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
        private Process _apiProcess;
        private HttpClient _httpClient;

        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["Conn"].ConnectionString;


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
                BaseAddress = new Uri("http://192.168.1.111:5128")
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
        public void TestDatabaseDataTransfer()
        {
            // Arrange
            InsertTestData();

            var apiService = new ApiService(_httpClient, new CircuitBreaker(3, TimeSpan.FromSeconds(8), 3));
            var databaseService = new DatabaseService(ConnectionString, apiService, new CircuitBreaker(3, TimeSpan.FromSeconds(8), 3));

            // Act
            int rowsTransferred = databaseService.SettingDatabaseConnection();

            // Assert
            Assert.IsTrue(rowsTransferred > 0, "No rows were transferred!");

            VerifyOutputData();

            // Cleanup
            CleanupTestData();
        }

        private void InsertTestData()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var query = @"
                    INSERT INTO [Adressevasker].[dbo].[Input] (
                        [KildesystemID], [Adresse], [HusNr], [Etage], [Doer], [PostNr], [By], [Kildesystem], [Dato], [Status]
                    ) VALUES (
                        123, 'Tårnblæservej', 11, 4, 'th', 2400, 'København NV', 'test', GETDATE(), 'Ny'
                    )";

                using (var command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private void VerifyOutputData()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var query = "SELECT * FROM [Adressevasker].[dbo].[Output] WHERE KildesystemID = 123";
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        Assert.IsTrue(reader.HasRows, "No data found in the Output table!");

                        while (reader.Read())
                        {
                            Assert.AreEqual("Tårnblæservej", reader["Adresse"].ToString(), "Incorrect address in output!");
                            Assert.AreEqual(2400, Convert.ToInt32(reader["PostNr"]), "Incorrect postal code in output!");
                        }
                    }
                }
            }
        }

        private void CleanupTestData()
        {
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                
                var deleteOutputQuery = "DELETE FROM [Adressevasker].[dbo].[Output] WHERE KildesystemID = 123";

                using (var command = new SqlCommand(deleteOutputQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}