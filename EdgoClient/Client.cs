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

		private Pkcs1Encoding encryptEngine = null;

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


		private void cryptInit()
		{
			RsaKeyParameters publicKey = (RsaKeyParameters)PublicKeyFactory.CreateKey(pgpKey);
			encryptEngine = new Pkcs1Encoding(new RsaEngine());
			encryptEngine.Init(true, publicKey);
		}
		private void cryptInitPrivate()
		{
			RsaPrivateCrtKeyParameters privateKey = (RsaPrivateCrtKeyParameters)PrivateKeyFactory.CreateKey(pgpKey);
			encryptEngine = new Pkcs1Encoding(new RsaEngine());
			encryptEngine.Init(true, privateKey); // ????
		}

		private byte[] encryptString(String source)
		{
			
			if (encryptEngine == null) {
				cryptInit();
			}

			byte[] bytesToEncrypt = Encoding.UTF8.GetBytes(source);
			int inLength = encryptEngine.GetInputBlockSize();


			int resultLength = bytesToEncrypt.Length / inLength + (bytesToEncrypt.Length % inLength > 0 ? 1 : 0);
			byte[] cipherbytes = new byte[resultLength * encryptEngine.GetOutputBlockSize()];
			int start = 0;
			int copyIdx = 0;
			//logger.log("Bytes length: " + bytesToEncrypt.Length);
			int length = (bytesToEncrypt.Length - start) > inLength ? inLength : bytesToEncrypt.Length - start;
			while (start < bytesToEncrypt.Length)
			{
				byte[] encBuffer = new byte[length];
				byte[] buffer = encryptEngine.ProcessBlock(bytesToEncrypt, start, length);
				//logger.log("Bytes ctypted: " + buffer.Length);
				//Console.WriteLine("Start: " + start + "\t Length: " + buffer.Length);
				Array.Copy(buffer, 0, cipherbytes, copyIdx, buffer.Length);
				start += inLength;
				copyIdx += buffer.Length;
				length = (bytesToEncrypt.Length - start) > inLength ? inLength : bytesToEncrypt.Length - start;
			}
			return cipherbytes;
		}

		private byte[] decryptString(byte[] source)
		{
			/*
			if (rsa == null)
			{
				cryptInit();
			}
			*/
			byte[] cipherbytes = null; // = rsa.Decrypt(source, false);
			return cipherbytes;
		}


		protected String request(List<KeyValuePair<string, string>> parameters)
		{
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
		public bool httpTest()
		{

			String data = base64Encode(encryptString("HELLO"));

			var pairs = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("userID", userId),
				new KeyValuePair<string, string>("data", data),
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
			lastCommand = "RESUME";

			String data = base64Encode(encryptString("RESUME"));

			var pairs = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("userID", userId),
				new KeyValuePair<string, string>("data", data),
			};

			String stringContent = request(pairs);

			lastResponce = stringContent;
			//Console.WriteLine(stringContent);
			//logger.log("|" + stringContent + "|");

			IDictionary<string, string> result = null;

			try
			{
				IDictionary<string, object> values = JsonConvert.DeserializeObject<Dictionary<string, object>>(stringContent);
				if (values.ContainsKey("result") && "OK".Equals(values["result"]) && values.ContainsKey("data"))
				{

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

			return result;
		}

		public IDictionary<string, object> sendEvent(String events)
		{

			lastCommand = events;

			String data = base64Encode(encryptString(events));

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
