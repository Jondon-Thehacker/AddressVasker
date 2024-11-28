using System;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using DawaDotnetClient1;
using DawaDotnetClient2;
using System.Data.SqlClient;
using System.Configuration;
using Microsoft.SqlServer.Server;
using System.Linq;
using System.Data;
using Client;
using static System.Net.Mime.MediaTypeNames;
using static Client.Program;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;


namespace Client
{
    public class SqlDataUtils
    {
        public SqlParameter Parameter(string name, object value)
        {
            return new SqlParameter(name, value);
        }
    }

    static class nongeneric
    {
        public static string EmptyIfNull(this string self)
        {
            return self ?? "";
        }
        public static string WithMaxLength(this string value, int maxLength)
        {
            return value?.Substring(0, Math.Min(value.Length, maxLength));
        }
        public static string ToAlphaNum(this string str)
        {
            //Fjerner uønskede karakterer i starten og slutningen af en string e.g. ()&*^
            foreach (char ch in str)
            {
                if (!char.IsLetterOrDigit(ch))
                    str = str.Trim(ch);
            }
            return str;

        }
        public static void SetParameters(this SqlCommand command, params SqlParameter[] parameters)
        {
            command.Parameters.AddRange(parameters);
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
                        Logger.LogException(new Exception("Circuit breaker permanently opened"), "Max retry limit reached. Circuit breaker is now permanently open.");
                        return true;
                    }

                    _isOpen = false;
                    _failureCount = 0;
                    _retryCount++;
                }

                return _isOpen;
            }
        }

        public bool CanRetry()
        {
            lock (_lock)
            {
                return _retryCount < _maxRetryLimit;
            }
        }

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
                    Logger.LogException(new Exception("Circuit breaker opened"), "Too many consecutive failures. Circuit breaker opened.");
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

        public TimeSpan GetopenTimeout()
        {
            return _openTimeout;
        }
    }

    public static class Logger
    {
        private static readonly ConcurrentQueue<string> _globalLog = new ConcurrentQueue<string>();

        public static void LogException(Exception ex, string context)
        {
            string exceptionDetails = $"[{DateTime.Now}] {context}: {ex.Message}\nStack Trace:\n{ex.StackTrace}\n";
            _globalLog.Enqueue(exceptionDetails);
        }

        public static string GetLog()
        {
            StringBuilder logBuilder = new StringBuilder();
            foreach (var logEntry in _globalLog)
            {
                logBuilder.AppendLine(logEntry);
            }
            return logBuilder.ToString();
        }
    }

    public class ApiService
    {
        private readonly HttpClient _client;
        private readonly CircuitBreaker _circuitBreaker;

        public ApiService(HttpClient client, CircuitBreaker circuitBreaker)
        {
            _client = client;
            _circuitBreaker = circuitBreaker;
        }

        public AdresseResultat GetAdresseVask(string query)
        {
            var i = 4;
            string getResponseAddress = null;
            string getResponseGps = null;
                while (i > 0)
                {

                    string url = "adresser" + (query.Length == 0 ? "" : "?") + query;
                    //Console.WriteLine("GET " + url);

                    try
                    {


                        HttpResponseMessage response = _client.GetAsync(url).Result;
                        response.EnsureSuccessStatusCode();
                        string responseBody = response.Content.ReadAsStringAsync().Result;

                        //Console.WriteLine(responseBody);
                        var settings = new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            MissingMemberHandling = MissingMemberHandling.Ignore
                        };

                        DawaDotnetClient1.Root adresseJson = JsonConvert.DeserializeObject<DawaDotnetClient1.Root>(responseBody, settings);

                        if (adresseJson.resultater.Count != 0)
                        {
                            string kategori = adresseJson.kategori.EmptyIfNull().ToString();

                            string status = "0";
                            string DarID = null;
                            string vejnavn = null;
                            string husnr = null;
                            string etage = null;
                            string dør = null;
                            string postnr = null;
                            string postnrnavn = null;
                            string gpsHref = null;
                            double utm_x = 0;
                            double utm_y = 0;
                            string kommentar = null;

                            // Get address information from aktueladresse or adresse json tags
                            if (adresseJson.resultater[0].aktueladresse != null)
                            {
                                DarID = adresseJson.resultater[0].aktueladresse.id.EmptyIfNull().ToString();
                                vejnavn = adresseJson.resultater[0].aktueladresse.vejnavn.EmptyIfNull().ToString();
                                husnr = adresseJson.resultater[0].aktueladresse.husnr.EmptyIfNull().ToString();
                                etage = adresseJson.resultater[0].aktueladresse.etage.EmptyIfNull().ToString();
                                dør = adresseJson.resultater[0].aktueladresse.dør.EmptyIfNull().ToString();
                                postnr = adresseJson.resultater[0].aktueladresse.postnr.EmptyIfNull().ToString();
                                postnrnavn = adresseJson.resultater[0].aktueladresse.postnrnavn.EmptyIfNull().ToString();
                                status = adresseJson.resultater[0].aktueladresse.status.EmptyIfNull().ToString();
                                gpsHref = adresseJson.resultater[0].aktueladresse.href.EmptyIfNull().ToString();
                            }
                            else
                            {
                                DarID = adresseJson.resultater[0].adresse.id.EmptyIfNull().ToString();
                                vejnavn = adresseJson.resultater[0].adresse.vejnavn.EmptyIfNull().ToString();
                                husnr = adresseJson.resultater[0].adresse.husnr.EmptyIfNull().ToString();
                                etage = adresseJson.resultater[0].adresse.etage.EmptyIfNull().ToString();
                                dør = adresseJson.resultater[0].adresse.dør.EmptyIfNull().ToString();
                                postnr = adresseJson.resultater[0].adresse.postnr.EmptyIfNull().ToString();
                                postnrnavn = adresseJson.resultater[0].adresse.postnrnavn.EmptyIfNull().ToString();
                                status = adresseJson.resultater[0].adresse.status.ToString();
                                gpsHref = adresseJson.resultater[0].adresse.href.EmptyIfNull().ToString();
                                kommentar = "Ingen aktuel adresse";
                            }
                            getResponseAddress = _client.BaseAddress.ToString() + url;

                           /* Console.WriteLine("Kategori: " + kategori);
                            Console.WriteLine("DAR-ID: " + DarID);
                            Console.WriteLine("vejnavn: " + vejnavn);
                            Console.WriteLine("husnr: " + husnr);
                            Console.WriteLine("etage: " + etage);
                            Console.WriteLine("dør: " + dør);
                            Console.WriteLine("postnr: " + postnr);
                            Console.WriteLine("postnrnavn: " + postnrnavn);
                            Console.WriteLine("status: " + status);
                            Console.WriteLine("getResponse: " + getResponseAddress);

                            */

                            if ((status != "2" || status != "4") && gpsHref != "")
                            {
                                getResponseGps = gpsHref + "?srid=25832";
                                try
                                {
                                    HttpResponseMessage response1 = _client.GetAsync(getResponseGps).Result;
                                    response1.EnsureSuccessStatusCode();
                                    string responseBody1 = response1.Content.ReadAsStringAsync().Result;

                                    DawaDotnetClient2.Root koordinatJson = JsonConvert.DeserializeObject<DawaDotnetClient2.Root>(responseBody1, settings);

                                    utm_x = Convert.ToDouble(koordinatJson.adgangsadresse.adgangspunkt.koordinater[0]);
                                    utm_y = Convert.ToDouble(koordinatJson.adgangsadresse.adgangspunkt.koordinater[1]);
                                }
                                catch (TaskCanceledException e)
                                {
                                    Logger.LogException(e, $"Timeout error when requesting GPS data in GetAdresseVask() for {getResponseGps}");
                                    kommentar = "Timeout occurred while fetching GPS data.";
                                }
                                catch (HttpRequestException e)
                                {
                                    Logger.LogException(e, $"Error with GPS data in GetAdresseVask() for {getResponseGps}");
                                }

                            }

                            AdresseResultat o = new AdresseResultat { darID = DarID, vejnavn = vejnavn, husnr = husnr, etage = etage, dør = dør, Kategori = kategori, latitude = utm_x, longitude = utm_y, postnr = postnr, postnrnavn = postnrnavn, kommentar = kommentar, apiCallAddress = getResponseAddress, apiCallGPS = getResponseGps };

                            i = 0;
                            return o;
                        }
                    }
                    catch (HttpRequestException e)
                    {
                        Logger.LogException(e, $"HTTP error when requesting {url}");
                        return new AdresseResultat
                        {
                            kommentar = $"HTTP request failed: {e.Message}",
                            apiCallAddress = getResponseAddress
                        };
                    }
                    catch (TaskCanceledException e)
                    {
                        Logger.LogException(e, $"Request to {url} timed out.");
                        return new AdresseResultat
                        {
                            kommentar = "Request timed out.",
                            apiCallAddress = getResponseAddress
                        };
                    }
                    catch (Exception e)
                    {
                        Logger.LogException(e, $"Unexpected error when requesting {url}");
                        _circuitBreaker.RegisterFailure();
                        return new AdresseResultat
                        {
                            kommentar = "An unexpected error occurred in GetAdresseVask",
                            apiCallAddress = getResponseAddress
                        };
                    }

                    query = query.Substring(0, query.LastIndexOf(" ") < 0 ? 0 : query.LastIndexOf(" "));
                    i = i - 1;
                }
                return new AdresseResultat { darID = null, vejnavn = null, husnr = null, etage = null, dør = null, Kategori = null, latitude = 0, longitude = 0, postnr = null, postnrnavn = null, kommentar = "Fandt ingen adresse match hos Dawa.", apiCallAddress = getResponseAddress, apiCallGPS = getResponseGps };
        }
    }

    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly ApiService _apiService;
        private readonly CircuitBreaker _circuitBreaker;

        public DatabaseService(string connectionString, ApiService apiService, CircuitBreaker circuitBreaker)
        {
            _connectionString = connectionString;
            _apiService = apiService;
            _circuitBreaker = circuitBreaker;

        }

        private bool LoopToContinue(string connetionString)
        {
            using (SqlConnection conn = new SqlConnection(connetionString))
            {
                string queryString1 = "SELECT top 1 [ID] FROM [dbo].[Input] WHERE Status not in ('" + BehandlingsStatus.Behandlet.ToString() + "','" + BehandlingsStatus.Behandler.ToString() + "')";
                SqlCommand command = new SqlCommand(queryString1, conn);

                try
                {
                    conn.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (!reader.HasRows)
                    {
                        Console.WriteLine("No rows!!! Exit");
                        Console.WriteLine($"Thread ID: {Thread.CurrentThread.ManagedThreadId}");
                        return false;
                    }
                    return true;
                }
                catch (SqlException sqlEx)
                {
                    Logger.LogException(sqlEx, "Error opening SQL connection in LoopToContinue()");
                    return false;
                }
            }
        }

        private int ExecuteNonQuery(string query, params SqlParameter[] parameters)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["Conn"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    using (SqlCommand command = new SqlCommand(query, conn))
                    {
                        if (parameters != null && parameters.Length > 0)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        int rowsAffected = command.ExecuteNonQuery();
                        //Console.WriteLine($"{rowsAffected} row(s) updated");
                        return rowsAffected;
                    }
                }
                catch (Exception e)
                {
                    Logger.LogException(e, "Error executing non-query command in ExecuteNonQuery()");
                    return 0;
                }
            }
        }

        public int SettingDatabaseConnection()
        {
            int rowsTransferred = 0;

            string ID = null;
            string KildesystemID = null;
            string Adresse = null;
            string HusNr = null;
            string Etage = null;
            string Doer = null;
            string Postnr = null;
            string By = null;
            string Kildesystem = null;
            string status = null;
            DateTime dato = DateTime.Now;



            while (LoopToContinue(_connectionString) && _circuitBreaker.CanRetry())
            {

                if (_circuitBreaker.IsOpen())
                {
                    Logger.LogException(new Exception("Circuit breaker open"), "Waiting for the open timeout to retry...");
                    Thread.Sleep(_circuitBreaker.GetopenTimeout());  // Wait for the open timeout
                    continue;  // After waiting, retry the loop
                }
                //Continue
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string queryString1 = "UPDATE TOP (1) [dbo].[Input] WITH ( readpast, rowlock)" + //(updlock, readpast, rowlock)
                                            "SET STATUS = '" + BehandlingsStatus.Behandler.ToString() + "', [Dato]=getdate()" +
                                            "OUTPUT INSERTED.*" +
                                            "WHERE Status not in ('" + BehandlingsStatus.Behandlet.ToString() + "', '" + BehandlingsStatus.Behandler.ToString() + "', '" + BehandlingsStatus.Fejlet.ToString() + "') " +
                                            "";
                    try
                    {
                        SqlCommand command = new SqlCommand(queryString1, conn);

                        conn.Open();

                        SqlDataReader reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            ID = reader["ID"].ToString().Trim();
                            KildesystemID = reader["KildesystemID"].ToString().Trim();
                            Adresse = reader["Adresse"].ToString().Trim();
                            HusNr = reader["HusNr"].ToString().Trim();
                            Etage = reader["Etage"].ToString().Trim();
                            Doer = reader["Doer"].ToString().Trim();
                            Postnr = reader["Postnr"].ToString().Trim();
                            By = reader["By"].ToString().Trim();
                            Kildesystem = reader["Kildesystem"].ToString().Trim();

                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogException(e, "Error establishing SQL-connection in SettingDatabaseConnection()");
                    }

                }

                string AdresseBetegnelse =
                    Adresse.ToAlphaNum()
+ (string.IsNullOrEmpty(HusNr) ? "" : " " + HusNr)
+ (string.IsNullOrEmpty(Etage) ? "" : " " + Etage)
                    + (string.IsNullOrEmpty(Doer) ? "" : " " + Doer)
                    + ","
                    + (string.IsNullOrEmpty(Postnr) ? "" : " " + Postnr)
                    + (string.IsNullOrEmpty(By) ? "" : " " + By);

                int InsertRowsAffected = 0;
                //Hent adresse

                string requestStatus = BehandlingsStatus.Behandlet.ToString();
                AdresseResultat adr = Adresse.ToAlphaNum() != "" ? _apiService.GetAdresseVask("betegnelse=" + AdresseBetegnelse) : new AdresseResultat(); //avoid api call to address that are invalid (blank)
                if (adr.darID is null || Adresse.ToAlphaNum() == "")
                {
                    requestStatus = BehandlingsStatus.Fejlet.ToString();

                }

                //Indsæt vasket adresse
                string queryString = "INSERT INTO [dbo].[Output] (  " +
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
                                        ", @apiCallGPS";



                SqlParameter[] paramtere = new[]
                {
                    new SqlParameter("@SytemID",ID) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@KildesystemID",KildesystemID) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@IDITAdresse",Adresse) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@IDITHusNr",HusNr) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@IDITEtage",Etage) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@IDITDoer",Doer) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@IDITpostnr",Postnr) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@IDITBy",By) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@Kildesystem",Kildesystem) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@requestStatus",requestStatus) { SqlDbType = SqlDbType.VarChar },

                    new SqlParameter("@darID",adr.darID.EmptyIfNull()) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@vejnavn",adr.vejnavn.EmptyIfNull()) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@husnr",adr.husnr.EmptyIfNull()) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@etage",adr.etage.EmptyIfNull()) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@doer",adr.dør.EmptyIfNull()) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@postnr",adr.postnr.EmptyIfNull()) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@postnrnavn",adr.postnrnavn.EmptyIfNull()) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@Kategori",adr.Kategori.EmptyIfNull()) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@latitude",adr.latitude.ToString().Replace(",", ".").EmptyIfNull()) { SqlDbType = SqlDbType.Float },
                    new SqlParameter("@longitude",adr.longitude.ToString().Replace(",", ".").EmptyIfNull()) { SqlDbType = SqlDbType.Float },
                    new SqlParameter("@kommentar",adr.kommentar.EmptyIfNull()) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@apiCallAddress",adr.apiCallAddress.EmptyIfNull()) { SqlDbType = SqlDbType.VarChar },
                    new SqlParameter("@apiCallGPS",adr.apiCallGPS.EmptyIfNull()) { SqlDbType = SqlDbType.VarChar },
                };

                InsertRowsAffected = ExecuteNonQuery(queryString, paramtere);
                //only delete the original row if data has been inserted to output
                if (InsertRowsAffected > 0)
                {
                    Interlocked.Add(ref rowsTransferred, InsertRowsAffected);

                    // Delete input address
                    string deleteQuery = "DELETE FROM [dbo].[input] WHERE ID=@ID";
                    SqlParameter param = new SqlParameter("@ID", ID) { SqlDbType = SqlDbType.Int };
                    int deleteQueryResultNumber = ExecuteNonQuery(deleteQuery, param);
                }

            }

            return rowsTransferred;
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
        public string apiCallGPS { get; set; }
    }

    public class Program
    {   
        static void Main(string[] args)
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.BaseAddress = new Uri("https://api.dataforsyningen.dk/datavask/");
            //client.BaseAddress = new Uri("http://192.168.1.111:5128"); //Testing
            string connectionString = ConfigurationManager.ConnectionStrings["Conn"].ConnectionString;

            int failureThreshold = 3;
            TimeSpan openTimeout = TimeSpan.FromSeconds(8);
            int maxRetryLimit = 3;

            CircuitBreaker circuitBreaker = new CircuitBreaker(failureThreshold, openTimeout, maxRetryLimit);
            ApiService apiService = new ApiService(client, circuitBreaker);

            DateTime startTime = DateTime.Now;
            int logId = LogStartOfRun(connectionString, startTime);

            int numberOfThreads = 10; // Adjust as needed
            int totalRowsTransferred = 0;

            Parallel.For(0, numberOfThreads, i =>
            {
                var databaseService = new DatabaseService(connectionString, apiService, circuitBreaker);
                int rowsTransferred = databaseService.SettingDatabaseConnection();
                Interlocked.Add(ref totalRowsTransferred, rowsTransferred);
            });

            DateTime endTime = DateTime.Now;
            TimeSpan totalRuntime = endTime - startTime;

            LogEndOfRun(connectionString, logId, totalRowsTransferred, endTime, totalRuntime, Logger.GetLog());

            Console.ReadLine();
        }

        private static int LogStartOfRun(string connectionString, DateTime startTime)
        {
            string query = "INSERT INTO [dbo].[ExecutionLog] (StartTime) OUTPUT INSERTED.LogID VALUES (@StartTime)";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {

                    using (SqlCommand command = new SqlCommand(query, conn))
                    {
                        command.Parameters.Add(new SqlParameter("@StartTime", startTime));
                        conn.Open();
                        return (int)command.ExecuteScalar();
                    }

            }
        }

        private static void LogEndOfRun(string connectionString, int logId, int rowsTransferred, DateTime endTime, TimeSpan totalRuntime, String log)
        {
            string query = @"UPDATE [dbo].[ExecutionLog]
                             SET EndTime = @EndTime, 
                                 RowsTransferred = @RowsTransferred,
                                 TotalRuntime = @TotalRuntime,
                                 Log = @Log   
                             WHERE LogID = @LogID";


            using (SqlConnection conn = new SqlConnection(connectionString))
            {

                using (SqlCommand command = new SqlCommand(query, conn))
                {
                    command.Parameters.Add(new SqlParameter("@EndTime", endTime));
                    command.Parameters.Add(new SqlParameter("@RowsTransferred", rowsTransferred));
                    command.Parameters.Add(new SqlParameter("@TotalRuntime", totalRuntime.ToString()));
                    command.Parameters.Add(new SqlParameter("@Log", log));
                    command.Parameters.Add(new SqlParameter("@LogID", logId));

                    conn.Open();
                    command.ExecuteNonQuery();
                }
            }
        }

        public enum BehandlingsStatus
        {
            Behandlet,
            Behandler,
            Fejlet,
            Genkoersel,
            Timeout
        }

    }
}