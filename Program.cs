using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DawaDotnetClient1;
using Newtonsoft.Json;
using static System.Net.Mime.MediaTypeNames;

namespace Client
{
    public class SqlDataUtils
    {
        public SqlParameter Parameter(string name, object value)
        {
            return new SqlParameter(name, value);
        }
    }

    public static class StringExtensions
    {
        public static string EmptyIfNull(this string self) => self ?? "";
        public static string WithMaxLength(this string value, int maxLength) => value?.Substring(0, Math.Min(value.Length, maxLength));
        public static string ToAlphaNum(this string str)
        {
            foreach (char ch in str)
            {
                if (!char.IsLetterOrDigit(ch))
                    str = str.Trim(ch);
            }
            return str;
        }
    }

    public class CircuitBreaker
    {
        private readonly int _failureThreshold;
        private readonly TimeSpan _openTimeout;
        private readonly int _maxRetryLimit;
        private int _failureCount;
        private int _retryCount;
        private DateTime _lastFailureTime;
        private bool _isOpen;
        private bool _isPermanentlyOpen;
        private readonly object _lock = new object();

        public CircuitBreaker(int failureThreshold, TimeSpan openTimeout, int maxRetryLimit)
        {
            _failureThreshold = failureThreshold;
            _openTimeout = openTimeout;
            _maxRetryLimit = maxRetryLimit;
            _failureCount = 0;
            _retryCount = 0;
            _lastFailureTime = DateTime.MinValue;
            _isOpen = false;
            _isPermanentlyOpen = false;
        }

        public bool IsOpen()
        {
            lock (_lock)
            {
                if (_isPermanentlyOpen) return true;
                if (_isOpen && DateTime.Now - _lastFailureTime > _openTimeout)
                {
                    if (_retryCount >= _maxRetryLimit)
                    {
                        _isPermanentlyOpen = true;
                        Logger.LogException(new Exception("Circuit breaker permanently opened"), "Max retry limit reached.");
                        return true;
                    }
                    _isOpen = false;
                    _failureCount = 0;
                    _retryCount++;
                }
                return _isOpen;
            }
        }

        public bool CanRetry() => _retryCount < _maxRetryLimit;

        public void RegisterFailure()
        {
            lock (_lock)
            {
                if (_isPermanentlyOpen) return;
                _failureCount++;
                _lastFailureTime = DateTime.Now;
                if (_failureCount >= _failureThreshold)
                {
                    _isOpen = true;
                    Logger.LogException(new Exception("Circuit breaker opened"), "Too many consecutive failures.");
                }
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                if (!_isPermanentlyOpen)
                {
                    _failureCount = 0;
                    _retryCount = 0;
                    _isOpen = false;
                }
            }
        }

        public TimeSpan GetOpenTimeout() => _openTimeout;
    }

    public static class Logger
    {
        private static readonly StringBuilder _globalLog = new StringBuilder();
        private static readonly object _logLock = new object();

        public static void LogException(Exception ex, string context)
        {
            lock (_logLock)
            {
                string exceptionDetails = $"[{DateTime.Now}] {context}: {ex.Message}\nStack Trace:\n{ex.StackTrace}\n";
                _globalLog.AppendLine("; " + exceptionDetails);
            }
        }

        public static string GetLog() => _globalLog.ToString();
    }

    public class ApiClient
    {
        private readonly HttpClient _httpClient;

        public ApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            return await _httpClient.GetAsync(url);
        }
    }

    public class ApiDataHandler
    {
        private readonly ApiClient _apiClient;

        public ApiDataHandler(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<AdresseResultat> GetAdresseDataAsync(string query)
        {
            string url = "adresser" + (string.IsNullOrEmpty(query) ? "" : $"?{query}");
            HttpResponseMessage response = await _apiClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            // Deserialize JSON and process as before
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore };
            DawaDotnetClient1.Root adresseJson = JsonConvert.DeserializeObject<DawaDotnetClient1.Root>(responseBody, settings);

            // Convert `adresseJson` to `AdresseResultat`
            var result = new AdresseResultat
            {
                darID = adresseJson.resultater[0].aktueladresse.id.EmptyIfNull().ToString(),
                vejnavn = adresseJson.resultater[0].aktueladresse.vejnavn.EmptyIfNull().ToString(),
                husnr = adresseJson.resultater[0].aktueladresse.husnr.EmptyIfNull().ToString(),
                etage = adresseJson.resultater[0].aktueladresse.etage.EmptyIfNull().ToString(),
                dør = adresseJson.resultater[0].aktueladresse.dør.EmptyIfNull().ToString(),
                postnr = adresseJson.resultater[0].aktueladresse.postnr.EmptyIfNull().ToString(),
                postnrnavn = adresseJson.resultater[0].aktueladresse.postnrnavn.EmptyIfNull().ToString(),
                status = adresseJson.resultater[0].aktueladresse.status.EmptyIfNull().ToString(),
                gpsHref = adresseJson.resultater[0].aktueladresse.href.EmptyIfNull().ToString()
            };

            return result;
        }
    }



    public class DatabaseRepository
    {
        private readonly string _connectionString;

        public DatabaseRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> HasPendingInputAsync()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var command = new SqlCommand("SELECT top 1 [ID] FROM [dbo].[Input] WHERE Status not in ('Behandlet', 'Behandler')", conn);
                await conn.OpenAsync();
                var reader = await command.ExecuteReaderAsync();
                return reader.HasRows;
            }
        }

        public async Task<List<Record>> FetchPendingRecordsAsync()
        {
            var records = new List<Record>();
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM [dbo].[Input] WHERE Status NOT IN ('Behandlet', 'Behandler')";
                using (var command = new SqlCommand(query, conn))
                {
                    await conn.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            records.Add(new Record
                            {
                                ID = reader["ID"].ToString().Trim(),
                                KildesystemID = reader["KildesystemID"].ToString().Trim(),
                                Adresse = reader["Adresse"].ToString().Trim(),
                                HusNr = reader["HusNr"].ToString().Trim(),
                                Etage = reader["Etage"].ToString().Trim(),
                                Doer = reader["Doer"].ToString().Trim(),
                                Postnr = reader["Postnr"].ToString().Trim(),
                                By = reader["By"].ToString().Trim(),
                                Kildesystem = reader["Kildesystem"].ToString().Trim(),
                            });
                        }
                    }
                }
            }
            return records;
        }

        public async Task<int> InsertTransformedDataAsync(Record record, AdresseResultat adr)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "INSERT INTO [dbo].[Output] (  " +
                                        "ID " +
                                        ", KildesystemID " +
                                        ", Adresse" +
                                        ", HusNr" +
                                        ", Etage" +
                                        ", Doer	 " +
                                        ", Postnr" +
                                        ", [By]" +
                                        ", Kildesystem" +
                                        ", status" +
                                        ", dato	" +

                                        ", DarID" +
                                        ", DawaAdresse" +
                                        ", DawaHusNr" +
                                        ", DawaEtage" +
                                        ", DawaDoer	 " +
                                        ", DawaPostnr" +
                                        ", DawaBy" +
                                        ", DawaKategori" +
                                        ", DawaLaengdegrad" +
                                        ", DawaBreddegrad" +
                                        ", Kommentar" +
                                        ", ApiCallAddress" +
                                        ", ApiCallGPS" +
                                        ") " +
                                        " SELECT " +
                                        "  @SytemID" +
                                        ", @KildesystemID" +
                                        ", @IDITAdresse" +
                                        ", @IDITHusNr" +
                                        ", @IDITEtage" +
                                        ", @IDITDoer" +
                                        ", @IDITpostnr" +
                                        ", @IDITBy" +
                                        ", @Kildesystem" +
                                        ", @requestStatus" +
                                        ", getdate()" +

                                        ", @darID" +
                                        ", @vejnavn" +
                                        ", @husnr" +
                                        ", @etage" +
                                        ", @doer" +
                                        ", @postnr" +
                                        ", @postnrnavn" +
                                        ", @Kategori" +
                                        ", @latitude" +
                                        ", @longitude" +
                                        ", @kommentar" +
                                        ", @apiCallAddress" +
                                        ", @apiCallGPS"; // Define your insert query here
                using (var command = new SqlCommand(query, conn))
                {
                    command.Parameters.AddRange(new[]
                    {
            new SqlParameter("@ID", record.ID),
            new SqlParameter("@KildesystemID", record.KildesystemID),
            new SqlParameter("@Address", record.Adresse),
            new SqlParameter("@HouseNumber", record.HusNr),
            new SqlParameter("@Floor", record.Etage),
            new SqlParameter("@Door", record.Doer),
            new SqlParameter("@PostalCode", record.Postnr),
            new SqlParameter("@City", record.By),
            new SqlParameter("@SourceSystem", record.Kildesystem),
            new SqlParameter("@Status", "Behandlet"),  // Setting status to "Behandlet" after processing
            new SqlParameter("@Date", DateTime.Now),
            
            // Mapped properties from AdresseResultat
            new SqlParameter("@DarID", adr.darID),
            new SqlParameter("@DawaAddress", adr.vejnavn),
            new SqlParameter("@DawaHouseNumber", adr.husnr),
            new SqlParameter("@DawaFloor", adr.etage),
            new SqlParameter("@DawaDoor", adr.dør),
            new SqlParameter("@DawaPostalCode", adr.postnr),
            new SqlParameter("@DawaCity", adr.postnrnavn),
            new SqlParameter("@DawaCategory", adr.Kategori),
            new SqlParameter("@DawaLatitude", adr.latitude),
            new SqlParameter("@DawaLongitude", adr.longitude),
            new SqlParameter("@Comment", adr.kommentar),
            new SqlParameter("@ApiCallAddress", adr.apiCallAddress),
            new SqlParameter("@ApiCallGPS", adr.apiCallGPS)
        });

                    await conn.OpenAsync();
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<int> DeleteProcessedRecordAsync(string id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                string query = "DELETE FROM [dbo].[Input] WHERE ID = @ID";
                var command = new SqlCommand(query, conn);
                command.Parameters.Add(new SqlParameter("@ID", id));
                await conn.OpenAsync();
                return await command.ExecuteNonQueryAsync();
            }
        }
    }

    public class DataTransformationService
    {
        private readonly ApiDataHandler _apiDataHandler;

        public DataTransformationService(ApiDataHandler apiDataHandler)
        {
            _apiDataHandler = apiDataHandler;
        }

        public async Task<AdresseResultat> ProcessAddressAsync(string address)
        {
            string formattedAddress = FormatAddress(address);
            return !string.IsNullOrEmpty(formattedAddress) ? await _apiDataHandler.GetAdresseDataAsync($"betegnelse={formattedAddress}") : null;
        }

        private string FormatAddress(string address)
        {
            // Format and sanitize the address string
            return address.Trim();
        }
    }

    public class DatabaseService
    {
        private readonly DatabaseRepository _dbRepository;
        private readonly DataTransformationService _dataTransformer;
        private readonly CircuitBreaker _circuitBreaker;

        public DatabaseService(DatabaseRepository dbRepository, DataTransformationService dataTransformer, CircuitBreaker circuitBreaker)
        {
            _dbRepository = dbRepository;
            _dataTransformer = dataTransformer;
            _circuitBreaker = circuitBreaker;
        }

        public async Task<int> SettingDatabaseConnectionParallelAsync()
        {
            int rowsTransferred = 0;
            var semaphore = new SemaphoreSlim(10);  // Limit to 10 concurrent tasks
            var tasks = new List<Task<int>>();

            while (await _dbRepository.HasPendingInputAsync() && _circuitBreaker.CanRetry())
            {
                if (_circuitBreaker.IsOpen())
                {
                    await Task.Delay(_circuitBreaker.GetOpenTimeout());
                    continue;
                }

                var records = await _dbRepository.FetchPendingRecordsAsync();

                foreach (var record in records)
                {
                    await semaphore.WaitAsync();  // Wait until a slot is available
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            return await ProcessRecordAsync(record);
                        }
                        finally
                        {
                            semaphore.Release();  // Release the slot
                        }
                    }));
                }

                int[] results = await Task.WhenAll(tasks);
                rowsTransferred += results.Sum();
            }

            return rowsTransferred;
        }


        private async Task<int> ProcessRecordAsync(Record record)
        {
            var adr = await _dataTransformer.ProcessAddressAsync(record.Adresse);
            if (adr == null) return 0;

            int insertResult = await _dbRepository.InsertTransformedDataAsync(record, adr);
            if (insertResult > 0)
            {
                await _dbRepository.DeleteProcessedRecordAsync(record.ID);
                return 1;  // Returns 1 to indicate successful processing
            }

            return 0;  // Returns 0 if the record wasn't processed successfully
        }

    }

    public class Program
    {
        static async Task Main(string[] args)
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5), BaseAddress = new Uri("https://api.dataforsyningen.dk/datavask/") };
            var apiClient = new ApiClient(client);
            var apiDataHandler = new ApiDataHandler(apiClient);
            var dataTransformer = new DataTransformationService(apiDataHandler);

            string connectionString = ConfigurationManager.ConnectionStrings["Conn"].ConnectionString;
            var dbRepository = new DatabaseRepository(connectionString);
            var circuitBreaker = new CircuitBreaker(3, TimeSpan.FromSeconds(8), 3);
            var databaseService = new DatabaseService(dbRepository, dataTransformer, circuitBreaker);

            int rowsTransferred = await databaseService.SettingDatabaseConnectionParallelAsync();
            Console.WriteLine($"Total Rows Transferred: {rowsTransferred}");
        }
    }

    public class AdresseResultat
    {
        public string Kategori { get; set; }
        public string darID { get; set; }
        public string vejnavn { get; set; }
        public string husnr { get; set; }
        public string etage { get; set; }
        public string dør { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string postnrnavn { get; set; }
        public string postnr { get; set; }
        public string kommentar { get; set; }
        public string apiCallAddress { get; set; }
        public string status { get; set; }
        public string apiCallGPS { get; set; }
        public string gpsHref { get; set; }
    }
    
    public class Record
    {
        public string ID { get; set; }
        public string KildesystemID { get; set; }
        public string Adresse { get; set; }
        public string HusNr { get; set; }
        public string Etage { get; set; }
        public string Doer { get; set; }
        public string Postnr { get; set; }
        public string By { get; set; }
        public string Kildesystem { get; set; }
        public string Status { get; set; }
        public DateTime Dato { get; set; }
    }

}

