using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;

using EdGo.Crypto;

namespace EdGo.EdgoClient
{
	class Client
	{
		private Regex reg = new Regex("^.*\"data\"\\:\\s*(\\{[^\\}]*\\}).*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		public static Client instance { get; } = new Client();

		private byte[] pgpKey = null;
		public String url { get; set; } = null;
		public String userId { get; set; } = null;

		private String __userKey = "";
		public String userKey {
			get
			{
				return __userKey;
			}
			set {
				try
				{
					__userKey = value;
					pgpKey = base64Decode(__userKey);
				} catch
				{
					pgpKey = null;
				}
			}
		}

		public bool isTested { get; set; } = false;

		public String lastCommand { get; private set; } = null;
		public String lastResponce { get; private set; } = null;

		public bool canTest {
			get {
				return url.Trim() != "" && userId.Trim() != "" && pgpKey != null && pgpKey.Length != 0;
			}
		}

        private CryptoAlgorithm coder = null;

        private String desKey = null;

        private CryptoAlgorithm desCoder = null;


        private HttpClient http = new HttpClient();

		private TextLogger logger = TextLogger.instance;
		protected Client()
		{
			retunDefault();
		}

		public void retunDefault()
		{
			url = Properties.Settings.Default.EdgoURL;
			userId = Properties.Settings.Default.UserID;
			userKey = Properties.Settings.Default.UserKey;
			isTested = Properties.Settings.Default.IsTestedEdgoConnection;
		}

		public void saveDefault()
		{
			Properties.Settings.Default.EdgoURL = url;
			Properties.Settings.Default.UserID = userId;
			Properties.Settings.Default.UserKey = userKey;
			Properties.Settings.Default.IsTestedEdgoConnection = isTested;
			Properties.Settings.Default.Save();
		}
		private byte[] base64Decode(String source)
		{
			return Convert.FromBase64String(source);
		}

		private String base64Encode(byte[] source)
		{
			return Convert.ToBase64String(source);
		}


        private void rsaInit()
        {
            if (coder == null) {
                RsaKeyParameters publicKey = (RsaKeyParameters)PublicKeyFactory.CreateKey(pgpKey);
                coder = new RSAAlgorithm(publicKey);
            }

		}

        private void desInit()
        {
            if (desCoder == null)
            {
                desCoder = new DESAlgorithm(desKey);
            }

        }


        protected String request1(List<KeyValuePair<string, string>> parameters)
		{
            http.Timeout = TimeSpan.FromHours(1);
			//var content = new FormUrlEncodedContent(parameters);
			var content = "";
			foreach (KeyValuePair< string, string> pair in parameters)
			{
				string name = WebUtility.UrlEncode(pair.Key);
				string value = WebUtility.UrlEncode(pair.Value);
				content += name + "=" + value + "&";
			}
			String result = null;

			HttpRequestMessage request = new HttpRequestMessage();
			request.Headers.Add("Accept", "application/json");
			request.Headers.Add("X-Requested-With", "XMLHttpRequest");
			request.Method = HttpMethod.Post;
			request.Content = new StringContent(content);
            request.RequestUri = new Uri(url);

			var response = http.SendAsync(request);

			while (!response.IsCompleted && !response.IsCanceled)
			{
				Thread.Sleep(1000);
			}
			try
			{
				if (response.Result.StatusCode == HttpStatusCode.OK)
				{
					result = response.Result.Content.ReadAsStringAsync().Result;
				}
			}
			catch (Exception e)
			{
			}

			return result;
		}

        protected String request(List<KeyValuePair<string, string>> parameters)
        {
            var content = "";
            foreach (KeyValuePair<string, string> pair in parameters)
            {
                string name = WebUtility.UrlEncode(pair.Key);
                string value = WebUtility.UrlEncode(pair.Value);
                content += name + "=" + value + "&";
            }
            String result = null;

            var request = (HttpWebRequest)WebRequest.Create(url);

            var data = Encoding.ASCII.GetBytes(content);

            request.Method = "POST";
            //request.Headers.Add("Accept", "application/json");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;
            request.Timeout = 3600000;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();

            result = new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd();

            return result;
        }


        public bool httpTest()
		{

            rsaInit();

            String data = "HELLO";
            //String secret = base64Encode(encryptString(data));
            String secret = coder.encode(data);

            var pairs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("userID", userId),
                new KeyValuePair<string, string>("data", data),
                new KeyValuePair<string, string>("secretKey", secret),
            };


            bool result = false;

			String stringContent = request(pairs);

			try { 
			IDictionary<string, Object> values = JsonConvert.DeserializeObject<Dictionary<string, Object>>(stringContent);
			if (values.ContainsKey("result") && "OK".Equals(values["result"]))
			{
				isTested = true;
				result = true;
			}
			}
			catch (Exception e)
			{
			}

			return result;
		}


		public IDictionary<string, string> getLastInfo()
		{

            rsaInit();

            lastCommand = "RESUME";

            //String data = base64Encode(encryptString("RESUME"));
            String data = "RESUME";
            String secret = coder.encode(data);

            var pairs = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("userID", userId),
				new KeyValuePair<string, string>("data", data),
                new KeyValuePair<string, string>("secretKey", secret),
            };

			String stringContent = request(pairs);

			lastResponce = stringContent;
			//Console.WriteLine(stringContent);
			//logger.log("|" + stringContent + "|");

			IDictionary<string, string> result = null;

            String secretKey = null;

			try
			{
				IDictionary<string, object> values = JsonConvert.DeserializeObject<Dictionary<string, object>>(stringContent);
				if (values.ContainsKey("result") && "OK".Equals(values["result"]) && values.ContainsKey("data"))
				{

                    secretKey = values["secretKey"].ToString();

                    String str = reg.Replace(stringContent, "$1");
					//Console.WriteLine(str);
					logger.log(str);
					if ("{}".Equals(str))
					{
						result = new Dictionary<string, string>();

					}
					else
					{
						result = JsonConvert.DeserializeObject<Dictionary<string, string>>(str);
					}

				}
			}
			catch (Exception e)
			{
			}

            desKey = coder.decode(secretKey);
            desCoder = null;

            return result;
		}

		public IDictionary<string, object> sendEvent(String events)
		{

			lastCommand = events;


            desInit();

            String data = desCoder.encode(events.Trim());

            Console.WriteLine("1");

            var pairs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("userID", userId),
                new KeyValuePair<string, string>("data", data),
            };

			String stringContent = request(pairs);

			//logger.log("|" + stringContent + "|");
			//logger.log("Send " + data.Length + " byte to server");
			//Console.WriteLine(stringContent);
			lastResponce = stringContent;

			IDictionary<string, object> result = null;

			try
			{
				result = JsonConvert.DeserializeObject<Dictionary<string, object>>(stringContent);
			}
			catch (Exception e)
			{
                logger.log(e.Message);
			}

			return result;
		}

		public static IDictionary<string, object> ToDictionary(JObject @object)
		{
			var result = @object.ToObject<Dictionary<string, object>>();

			var JObjectKeys = (from r in result
							   let key = r.Key
							   let value = r.Value.ToString()
							   where value.GetType() == typeof(JObject)
							   select key).ToList();

			var JArrayKeys = (from r in result
							  let key = r.Key
							  let value = r.Value.ToString()
							  where value.GetType() == typeof(JArray)
							  select key).ToList();

			JArrayKeys.ForEach(key => result[key] = ((JArray)result[key]).Values().Select(x => ((JValue)x).Value).ToArray());
			JObjectKeys.ForEach(key => result[key] = ToDictionary(result[key] as JObject));

			return result;
		}
	}


}
