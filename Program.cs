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

	class Program
	{
		static void Main(string[] args)
		{
			bool debugEnabled = false;
			bool consoleOutputOnly = false;
			bool consoleOutputDemo = false;
			if (args != null)
			{
				for (int i = 0; i < args.Length; i++) // Loop through array
				{
					if (args[i].ToLower() == "setup")
					{
						//Opretter de nødvendige database/tabeller for løsningen på serveren, hvis de ikke findes.
						//afslutter dernæst programmet.
						opretDatabaseArtifakter();
						return;
					}
					if (args[i].ToLower() == "retry")
					{
						//Tager alle de fejlede rækker fra dbo.Output, og lægger dem tilbage i dbo.Input. Rækkerne får dog nyt internt ID.
						//genkør de fejlede hvis der ikke er andre instancer af programmet som kører i forvejen på pc'en.
						if (isOtherInstanceRunning() == false)
						{
							Console.WriteLine("Genkørsel igangsat");
							retryFailed();

						}
					}
					if (args[i].ToLower() == "debug")
					{
						debugEnabled = true;
					}
					if (args[i].ToLower() == "consoleDemo")
					{
						consoleOutputDemo = true;  
					}
					if (args[i].ToLower() == "console")
					{
						consoleOutputOnly = true;
					}
				}

			}


			HttpClient client = new HttpClient();
			client.Timeout = new TimeSpan(0, 20, 0); // 20 minutter til store downloads
													 //client.BaseAddress = new Uri("http://dawa.aws.dk/");
			client.BaseAddress = new Uri("https://api.dataforsyningen.dk/datavask/");
			//GetAdresse(client, "0a3f50a0-73bf-32b8-e044-0003ba298018");
			//GetAdresser(client, "q=ålandsgade 22 3mf 2300 københavn");
			//GetAdresserStreaming(client, "kommunekode=0101");
			//AutocompleteVejnavne(client, "q=rødkildevej");
			//AutocompleteAdresser(client, "q=ålandsgade 22 3mf 2300 københavn s");

			if (consoleOutputDemo== true)
            {
				//quick way to test how the adressevasker will treat a specific address. Needs to modify the address below:
				GetAdresseVask(client, "betegnelse=birkevadsvej 9 4130"); //example of a working address
				Console.ReadLine();
				return;

			}
			if (consoleOutputOnly == true)
			{
				//quick way to test how the adressevasker will treat a specific address based on user inputs in the console window
				while (true)
                {
					Console.Write("Indtast Adresse: ");
					string var = Console.ReadLine();
					GetAdresseVask(client, "betegnelse=" + var); //example of a working address
					Console.WriteLine("\n");
				}


			}
			
			string connetionString = ConfigurationManager.ConnectionStrings["Conn"].ConnectionString; //"Data Source=LBFSQL08\\DATAPLATFORM_DEV; Initial Catalog = Adressevasker;Integrated Security=SSPI";

			


			//create instanace of database connection
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

			
			//husk at lave om for loop
			//int i = 0;
			while (LoopToContinue(connetionString))
			{
				//Continue
				using (SqlConnection conn = new SqlConnection(connetionString))
				{
					string queryString1 = "UPDATE TOP (1) [dbo].[Input] WITH ( readpast, rowlock)" + //(updlock, readpast, rowlock)
											"SET STATUS = '" + BehandlingsStatus.Behandler.ToString() + "', [Dato]=getdate()" +
											"OUTPUT INSERTED.*" +
											"WHERE Status not in ('" + BehandlingsStatus.Behandlet.ToString() + "', '" + BehandlingsStatus.Behandler.ToString() + "', '" + BehandlingsStatus.Fejlet.ToString() + "') " +
											"";
					try
					{
						SqlCommand command = new SqlCommand(queryString1, conn);
						//command.Parameters.AddWithValue("@Status", "Behandlet");

						//Console.WriteLine("Openning Connection ...");
						conn.Open();
						//Console.WriteLine("Connection successful!");
						Console.WriteLine(queryString1);

						SqlDataReader reader = command.ExecuteReader();


						while (reader.Read())
						{
							//Read Rows
							ID = reader["ID"].ToString().Trim();
							KildesystemID = reader["KildesystemID"].ToString().Trim();
							Adresse = reader["Adresse"].ToString().Trim();
							HusNr = reader["HusNr"].ToString().Trim();
							Etage = reader["Etage"].ToString().Trim();
							Doer = reader["Doer"].ToString().Trim();
							Postnr = reader["Postnr"].ToString().Trim();
							By = reader["By"].ToString().Trim();
							Kildesystem = reader["Kildesystem"].ToString().Trim();

							Console.WriteLine("Adresse Information:");
							Console.WriteLine("ID: " + ID);
							Console.WriteLine("KildesystemID: " + KildesystemID);
							Console.WriteLine("Adresse: " + Adresse);
							Console.WriteLine("HusNr: " + HusNr);
							Console.WriteLine("Etage: " + Etage);
							Console.WriteLine("Dør: " + Doer);
							Console.WriteLine("Postnr: " + Postnr);
							Console.WriteLine("By: " + By);
							Console.WriteLine("Kildesystem: " + Kildesystem);
						}
					}
					catch (Exception e)
					{
						Console.WriteLine("Error: " + e.Message);
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

				string requestStatus = BehandlingsStatus.Behandlet.ToString() ;
				AdresseResultat adr = Adresse.ToAlphaNum() != "" ? GetAdresseVask(client, "betegnelse=" + AdresseBetegnelse) : new AdresseResultat(); //avoid api call to address that are invalid (blank)
				if (adr.darID is null || Adresse.ToAlphaNum() == "")
				{
					requestStatus =  BehandlingsStatus.Fejlet.ToString();

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

                InsertRowsAffected = submitQuery2(queryString, paramtere);
				//only delete the original row if data has been inserted to output
				if (InsertRowsAffected > 0)
				{
					//Slet Input adresse
					string deleteQuery = "DELETE FROM [dbo].[input] WHERE ID=@ID";
					SqlParameter param = new SqlParameter("@ID", ID) { SqlDbType = SqlDbType.Int };
					int deleteQueryResultNumber = submitQuery(deleteQuery, param);

				}

				//Console.WriteLine(i);
				//i++;
			}


			//GetAdresseVask(client, "betegnelse=Islands Plads 1Z 1 4 1431"); //example of an adress that can't be found because the postnummer is very wrong

			//GetAdresseVask(client, "betegnelse=Sankt Gertrudsstræde 29 4600 Køge"); //example of a working address
			Console.ReadLine();
		}

		
		static AdresseResultat GetAdresseVask(HttpClient client, string query)
		{
			var i = 4; //4 tries to find a result. remove 1 argument from the address-query request for each try.
			string getResponseAddress = null;
			string getResponseGps = null;
			try
			{
				while (i>0) {
					string url = "adresser" + (query.Length == 0 ? "" : "?") + query;
					Console.WriteLine("GET " + url);

					HttpResponseMessage response = client.GetAsync(url).Result;
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
						getResponseAddress = client.BaseAddress.ToString() + url;

						Console.WriteLine("Kategori: " + kategori);
						Console.WriteLine("DAR-ID: " + DarID);
						Console.WriteLine("vejnavn: " + vejnavn);
						Console.WriteLine("husnr: " + husnr);
						Console.WriteLine("etage: " + etage);
						Console.WriteLine("dør: " + dør);
						Console.WriteLine("postnr: " + postnr);
						Console.WriteLine("postnrnavn: " + postnrnavn);
						Console.WriteLine("status: " + status);
						Console.WriteLine("getResponse: " + getResponseAddress);

						// Lookup geo coordinates
						if ((status != "2" || status != "4") && gpsHref != "")
						{
							getResponseGps = gpsHref + "?srid=25832";
							try
							{
								HttpResponseMessage response1 = client.GetAsync(getResponseGps).Result;
								response1.EnsureSuccessStatusCode();
								string responseBody1 = response1.Content.ReadAsStringAsync().Result;

								DawaDotnetClient2.Root koordinatJson = JsonConvert.DeserializeObject<DawaDotnetClient2.Root>(responseBody1, settings);

								utm_x = Convert.ToDouble(koordinatJson.adgangsadresse.adgangspunkt.koordinater[0]);
								utm_y = Convert.ToDouble(koordinatJson.adgangsadresse.adgangspunkt.koordinater[1]);
							}
							catch (HttpRequestException e)
							{
								Console.WriteLine("Fejlbesked ved hentning af gps koordinater:{0} ", e.Message);
								string fejlbesked = "Fejl ved gps data: " + e.Message;
							}

						}
						Console.WriteLine("lat: " + utm_x);
						Console.WriteLine("long: " + utm_y);

						// Return results
						AdresseResultat o = new AdresseResultat { darID = DarID, vejnavn = vejnavn, husnr = husnr, etage = etage, dør = dør, Kategori = kategori, latitude = utm_x, longitude = utm_y, postnr = postnr, postnrnavn = postnrnavn, kommentar = kommentar, apiCallAddress = getResponseAddress, apiCallGPS = getResponseGps };

						i = 0;
						return o;
					}

					//if no address found from DAWA, remove 1 argument from the address-query request
					query = query.Substring(0, query.LastIndexOf(" ") < 0 ? 0 : query.LastIndexOf(" "));
					i = i - 1;
				}
				return new AdresseResultat { darID = null, vejnavn = null, husnr = null, etage = null, dør = null, Kategori = null, latitude = 0, longitude = 0, postnr = null, postnrnavn = null, kommentar = "Fandt ingen adresse match hos Dawa.", apiCallAddress = getResponseAddress, apiCallGPS = getResponseGps };

			}
			catch (HttpRequestException e)
			{
				Console.WriteLine("Message :{0} ", e.Message);
				string fejlbesked = "Fejl: " + e.Message;

				return new AdresseResultat { darID = null, vejnavn = null, husnr = null, etage = null, dør = null, Kategori = null, latitude = 0, longitude = 0, postnr = null, postnrnavn = null, kommentar = fejlbesked.WithMaxLength(240), apiCallAddress = getResponseAddress, apiCallGPS = getResponseGps };

			}
		}
        public static void opretDatabaseArtifakter()
        {
			string connetionString = ConfigurationManager.ConnectionStrings["Conn"].ConnectionString;

			Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
			String Root = Directory.GetCurrentDirectory();

			Console.WriteLine(Root);
			string script = File.ReadAllText(Root + "\\CreateDatabaseObjects.sql");
			Console.WriteLine(script);

			SqlConnection conn = new SqlConnection(connetionString);
			try
			{
				//Console.WriteLine("Openning Connection ...");
				conn.Open();
				//Console.WriteLine("Connection successful!");
				using (SqlCommand command = new SqlCommand(script, conn)) //pass SQL query created above and connection
				{
					int InsertRowsAffected = command.ExecuteNonQuery(); //execute the Query
					Console.WriteLine(InsertRowsAffected + " row(s) updated");
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Error: " + e.Message);
			}

		}
		public static bool LoopToContinue(string connetionString)
        {
			using (SqlConnection conn = new SqlConnection(connetionString))
			{
				string queryString1 = "SELECT top 1 [ID] FROM [dbo].[Input] WHERE Status not in ('" + BehandlingsStatus.Behandlet.ToString() + "','" + BehandlingsStatus.Behandler.ToString() + "')";
				SqlCommand command = new SqlCommand(queryString1, conn);

				//Console.WriteLine("Openning Connection ...");
				conn.Open();
				//Console.WriteLine("Connection successful!");

				SqlDataReader reader = command.ExecuteReader();

				if (!reader.HasRows)
				{
					Console.WriteLine("No rows!!! Exit");
					Console.Read();
					return false;
				}
				return true;

			}
		}
		public static int submitQuery(string q, SqlParameter param)
		{
			string connetionString = ConfigurationManager.ConnectionStrings["Conn"].ConnectionString;

			using (SqlConnection conn = new SqlConnection(connetionString))
			{
				string queryString = q;
				Console.WriteLine(queryString);
				try
				{
					//Console.WriteLine("Openning Connection ...");
					conn.Open();
					//Console.WriteLine("Connection successful!");
					using (SqlCommand command = new SqlCommand(queryString, conn)) //pass SQL query created above and connection
					{
						if (param != null)
                        {
							command.Parameters.Add(param);

						}
						int InsertRowsAffected = command.ExecuteNonQuery(); //execute the Query

						Console.WriteLine(InsertRowsAffected + " row(s) updated");
						return InsertRowsAffected;
					}

				}
				catch (Exception e)
				{
					Console.WriteLine("Error: " + e.Message);
					return 0;
				}
			}
		}
		public static int submitQuery2(string q, SqlParameter[] paramtere)
		{
			string connetionString = ConfigurationManager.ConnectionStrings["Conn"].ConnectionString;

			using (SqlConnection conn = new SqlConnection(connetionString))
			{
				string queryString = q;
				//Console.WriteLine(queryString);
				try
				{
					//Console.WriteLine("Openning Connection ...");
					conn.Open();
					//Console.WriteLine("Connection successful!");
					using (SqlCommand command = new SqlCommand(queryString, conn)) //pass SQL query created above and connection
					{

						command.Parameters.AddRange(paramtere);

						
						int InsertRowsAffected = command.ExecuteNonQuery(); //execute the Query

						Console.WriteLine(InsertRowsAffected + " row(s) updated");
						return InsertRowsAffected;
					}

				}
				catch (Exception e)
				{
					Console.WriteLine("Error: " + e.Message);
					string queryUpdate = "UPDATE [dbo].[input] SET STATUS='" + BehandlingsStatus.Fejlet.ToString() + "' WHERE ID=@ID";

					foreach (SqlParameter i in paramtere)
					{
						if (i.ParameterName=="@SystemID")
                        {
							SqlParameter param = new SqlParameter("@ID", i.Value.ToString()) { SqlDbType = SqlDbType.Int};
							submitQuery(queryUpdate, param);
						}
					}

					
					return 0;
				}
			}
		}
		public static void retryFailed()
		{
			string queryString = "DELETE FROM dbo.Output " +
								 "OUTPUT " +
								 "DELETED.KildesystemID " +
								", DELETED.Adresse" +
								", DELETED.HusNr" +
								", DELETED.Etage" +
								", DELETED.Doer	 " +
								", DELETED.Postnr" +
								", DELETED.[By]" +
								", DELETED.Kildesystem" +
								", '" + BehandlingsStatus.Genkoersel.ToString() + "' " +
								", getdate()" +
								"INTO [dbo].[input] " +
								" (KildesystemID " +
								", Adresse" +
								", HusNr" +
								", Etage" +
								", Doer	 " +
								", Postnr" +
								", [By]" +
								", Kildesystem" +
								", status" +
								", dato) " +
								"WHERE STATUS = '" + BehandlingsStatus.Fejlet.ToString() + "' ";


			int result1 = submitQuery(queryString, null);

			string restartRowStuckedInProgress = "UPDATE  [dbo].[input]  " +
								" SET [STATUS] =  '" + BehandlingsStatus.Genkoersel.ToString() + "' " +
								", [Dato] = getdate()" +
								" WHERE STATUS in ('" + BehandlingsStatus.Behandler.ToString() + "','" + BehandlingsStatus.Fejlet.ToString() + "') ";

			int result2 = submitQuery(restartRowStuckedInProgress, null);
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
		public enum BehandlingsStatus
		{
			Behandlet,
			Behandler,
			Fejlet,
			Genkoersel
		}
		//static string formatAdresse(dynamic adresse)
		//{
		//	return string.Format("{0} {1} {2} {3}, {4} {5}", adresse.adgangsadresse.vejstykke.navn, adresse.adgangsadresse.husnr, adresse.etage, adresse.dør, adresse.adgangsadresse.postnummer.nr, adresse.adgangsadresse.postnummer.navn);
		//}
		public static bool isOtherInstanceRunning()
        {
			//Check if other instance of this program is running on the system
			return System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1;

		}

	}

}
