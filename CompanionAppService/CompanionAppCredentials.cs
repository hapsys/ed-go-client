namespace EddiCompanionAppService
{
    /// <summary>Storage of credentials for a single Elite: Dangerous user to access the Companion App</summary>
    public class CompanionAppCredentials
    {
        public string email { get; set; }
        public string password { get; set; }
		public string code { get; set; }
		public string appId { get; set; }
        public string machineId { get; set; }
        public string machineToken { get; set; }

		private static CompanionAppCredentials credentials = null;

		//private string dataPath;

		/// <summary>
		/// Obtain credentials from a file.  If the file name is not supplied the the default
		/// path of Constants.Data_DIR\credentials.json is used
		/// </summary>
		public static CompanionAppCredentials Instance()
		{
			if (credentials == null)
			{
				credentials = new CompanionAppCredentials();
			}
			return credentials;
		}
		public static CompanionAppCredentials Load()
        {
			/*
            if (filename == null)
            {
                filename = Constants.DATA_DIR + @"\credentials.json";
            }

            CompanionAppCredentials credentials = new CompanionAppCredentials();
            try
            {
                string credentialsData = File.ReadAllText(filename);
                credentials = JsonConvert.DeserializeObject<CompanionAppCredentials>(credentialsData);
            }
            catch {}

            credentials.dataPath = filename;
            return credentials;
			*/
			credentials = Instance();

			credentials.appId = EdGo.Properties.Settings.Default.CompanionAppId;
			credentials.machineId = EdGo.Properties.Settings.Default.CompanionMachineId;
			credentials.machineToken = EdGo.Properties.Settings.Default.CompanionMachineToken;


			return credentials;
        }

        /// <summary>
        /// Clear the information held by credentials.
        /// </summary>
        public void Clear()
        {
            appId = null;
            machineId = null;
            machineToken = null;
        }

        /// <summary>
        /// Obtain credentials to a file.  If the filename is not supplied then the path used
        /// when reading in the credentials will be used, or the default path of 
        /// Constants.Data_DIR\credentials.json will be used
        /// </summary>
        public void Save()
        {
			EdGo.Properties.Settings.Default.CompanionAppId = credentials.appId;
			EdGo.Properties.Settings.Default.CompanionMachineId = credentials.machineId;
			EdGo.Properties.Settings.Default.CompanionMachineToken = credentials.machineToken;
			EdGo.AppDispatcher.instance.saveProperties();
		}
	}
}
