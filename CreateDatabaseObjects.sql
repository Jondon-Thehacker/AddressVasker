IF DB_ID('Adressevasker') IS NULL
   BEGIN
		CREATE DATABASE [Adressevasker]
   END
 

if not exists (select * from Adressevasker.dbo.sysobjects where name='input' and xtype='U') 
	BEGIN
		CREATE TABLE [Adressevasker].[dbo].[Input](
			[ID] int IDENTITY(1,1) NOT NULL
			,[KildesystemID] [varchar](50) NULL
			,[Adresse] [varchar](150) NULL
			,[HusNr] [varchar](150) NULL
			,[Etage] [varchar](50) NULL
			,[Doer] [varchar](50) NULL
			,[PostNr] [varchar](50) NULL
			,[By] [varchar](150) NULL
			,[Kildesystem] [varchar](50) NULL
			,[Dato] datetime NULL
			,[Status] [varchar](10) NULL
		)  
	END
	CREATE UNIQUE CLUSTERED INDEX [ClusteredIndex-20210615-224939] ON [dbo].[Input]
	(
		[ID] ASC,
		[Status] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 90) ON [PRIMARY]
	GO
	CREATE UNIQUE NONCLUSTERED INDEX [NonClusteredIndex-20210615-231531] ON [dbo].[Input]
	(
		[ID] ASC,
		[Status] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 90) ON [PRIMARY]
	GO
if not exists (select * from Adressevasker.dbo.sysobjects where name='Output' and xtype='U') 
BEGIN
	CREATE TABLE [Adressevasker].[dbo].[Output](
		[ID] int NOT NULL
		,[KildesystemID] [varchar](50) NULL
		,[Adresse] [varchar](150) NULL
		,[HusNr] [varchar](150) NULL
		,[Etage] [varchar](50) NULL
		,[Doer] [varchar](50) NULL
		,[PostNr] [varchar](50) NULL
		,[By] [varchar](150) NULL
		,[Kildesystem] [varchar](50) NULL
		,[Dato] [varchar](50) NULL
		,[Status] [varchar](10) NULL
		,[DarID] [varchar](36) NULL
		,[DawaAdresse] [varchar](50) NULL
		,[DawaHusNr] [varchar](50) NULL
		,[DawaEtage] [varchar](50) NULL
		,[DawaDoer] [varchar](50) NULL
		,[DawaPostNr] [varchar](50) NULL
		,[DawaBy] [varchar](50) NULL
		,[DawaKategori] [varchar](10) NULL
		,[DawaLaengdegrad] float NULL
		,[DawaBreddegrad] float NULL
		,[Kommentar] [varchar](250) NULL
		,[ApiCallAddress] [varchar](250) NULL
		,[ApiCallGPS] [varchar](250) NULL
	) 
	END
