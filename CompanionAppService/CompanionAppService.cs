using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using EdGo;
//using Utilities;

namespace EddiCompanionAppService
{
    public class CompanionAppService
    {
		private static TextLogger logger = TextLogger.instance;

        private static string BASE_URL = "https://companion.orerve.net";
        private static string ROOT_URL = "/";
        private static string LOGIN_URL = "/user/login";
        private static string CONFIRM_URL = "/user/confirm";
        private static string PROFILE_URL = "/profile";

        // We cache the profile to avoid spamming the service
        //private Profile cachedProfile;
        //private DateTime cachedProfileExpires;

        public enum State
        {
            NEEDS_LOGIN,
            NEEDS_CONFIRMATION,
            READY
        };

        public State CurrentState;

        public CompanionAppCredentials Credentials;

        private static CompanionAppService instance;

        private static readonly object instanceLock = new object();

		public string Content { get; private set; } = null;
        public static CompanionAppService Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (instanceLock)
                    {
                        if (instance == null)
                        {
                            logger.log("No companion API instance: creating one");
                            instance = new CompanionAppService();
                        }
                    }
                }
                return instance;
            }
        }
        private CompanionAppService()
        {
            Credentials = CompanionAppCredentials.Load();

            // Need to work out our current state.

            //If we're missing username and password then we need to log in again
			/*
            if (string.IsNullOrEmpty(Credentials.email) || string.IsNullOrEmpty(Credentials.password))
            {
                CurrentState = State.NEEDS_LOGIN;
            }
            else 
			*/
			if (string.IsNullOrEmpty(Credentials.machineId) || string.IsNullOrEmpty(Credentials.machineToken))
            {
                CurrentState = State.NEEDS_LOGIN;
            }
			else if (string.IsNullOrEmpty(Credentials.appId))
			{
				CurrentState = State.NEEDS_CONFIRMATION;
			}
			else
			{
                // Looks like we're ready but test it to find out
                CurrentState = State.READY;
				/*
                try
                {
                    Profile();
                }
                catch (EliteDangerousCompanionAppException ex)
                {
                    logger.log("Failed to obtain profile: " + ex.ToString());
                }
				*/
            }
        }

        ///<summary>Log in.  Throws an exception if it fails</summary>
        public void Login()
        {
            if (CurrentState != State.NEEDS_LOGIN)
            {
                // Shouldn't be here
                throw new EliteDangerousCompanionAppIllegalStateException("Service in incorrect state to login (" + CurrentState + ")");
            }

            HttpWebRequest request = GetRequest(BASE_URL + LOGIN_URL);

            // Send the request
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";
            string encodedUsername = WebUtility.UrlEncode(Credentials.email);
            string encodedPassword = WebUtility.UrlEncode(Credentials.password);
            byte[] data = Encoding.UTF8.GetBytes("email=" + encodedUsername + "&password=" + encodedPassword);
            request.ContentLength = data.Length;
            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(data, 0, data.Length);
            }

            using (HttpWebResponse response = GetResponse(request))
            {
                if (response == null)
                {
                    throw new EliteDangerousCompanionAppException("Failed to contact API server");
                }

				foreach (string key in response.Headers.AllKeys)
				{
					logger.log(key + " ==> " + response.Headers[key]);
				}

				if (response.StatusCode == HttpStatusCode.Found && response.Headers["Location"] == CONFIRM_URL)
                {
                    CurrentState = State.NEEDS_CONFIRMATION;
                }
                else if (response.StatusCode == HttpStatusCode.Found && response.Headers["Location"] == ROOT_URL)
                {
                    CurrentState = State.READY;
                }
                else
                {
                    throw new EliteDangerousCompanionAppAuthenticationException("Username or password incorrect");
                }
            }
        }

        ///<summary>Confirm a login.  Throws an exception if it fails</summary>
        public void Confirm()
        {
            if (CurrentState != State.NEEDS_CONFIRMATION)
            {
                // Shouldn't be here
                throw new EliteDangerousCompanionAppIllegalStateException("Service in incorrect state to confirm login (" + CurrentState + ")");
            }

			string code = CompanionAppCredentials.Instance().code;

			HttpWebRequest request = GetRequest(BASE_URL + CONFIRM_URL);

            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";
            string encodedCode = WebUtility.UrlEncode(code);
            byte[] data = Encoding.UTF8.GetBytes("code=" + encodedCode);
            request.ContentLength = data.Length;
            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(data, 0, data.Length);
            }

            using (HttpWebResponse response = GetResponse(request))
            {
                if (response == null)
                {
                    throw new EliteDangerousCompanionAppException("Failed to contact API server");
                }

				foreach (string key in response.Headers.AllKeys)
				{
					logger.log(key + " ==> " + response.Headers[key]);
				}

                //logger.log(response.StatusCode.ToString());

                if (response.StatusCode == HttpStatusCode.Found && response.Headers["Location"] == ROOT_URL)
                {
                    CurrentState = State.READY;
                }
                else if (response.StatusCode == HttpStatusCode.Found && response.Headers["Location"] == LOGIN_URL)
                {
                    CurrentState = State.NEEDS_LOGIN;
                    throw new EliteDangerousCompanionAppAuthenticationException("Confirmation code incorrect or expired");
                }
                else if (response.StatusCode == HttpStatusCode.OK)
                {
                    //CurrentState = State.NEEDS_LOGIN;
                    throw new EliteDangerousCompanionAppAuthenticationException("Confirmation code incorrect");
                }
            }
        }

        public string Profile()
        {
            logger.log("Entered");
            if (CurrentState != State.READY)
            {
                // Shouldn't be here
                logger.log("Service in incorrect state to provide profile (" + CurrentState + ")");
                logger.log("Leaving");
                throw new EliteDangerousCompanionAppIllegalStateException("Service in incorrect state to provide profile (" + CurrentState + ")");
            }
			/*
            if (cachedProfileExpires > DateTime.Now)
            {
                // return the cached version
                logger.log("Returning cached profile");
                logger.log("Leaving");
                return cachedProfile;
            }
			*/

            string data = obtainProfile();

			//logger.log(data);

            if (data == null || data == "Profile unavailable")
            {
				// Happens if there is a problem with the API.  Logging in again might clear this...
				//relogin();
				//data = obtainProfile();
				CurrentState = State.NEEDS_LOGIN;
			}

			/*
            try
            {
                cachedProfile = ProfileFromJson(data);
            }
            catch (JsonException ex)
            {
                Logging.Error("Failed to parse companion profile", ex);
                cachedProfile = null;
            }

            if (cachedProfile != null)
            {
                cachedProfileExpires = DateTime.Now.AddSeconds(30);
                logger.log("Profile is " + JsonConvert.SerializeObject(cachedProfile));
            }
			*/
			Content = data;
            logger.log("Leaving");
            return data;
        }

        private string obtainProfile()
        {
            HttpWebRequest request = GetRequest(BASE_URL + PROFILE_URL);
            using (HttpWebResponse response = GetResponse(request))
            {
                if (response == null)
                {
                    logger.log("Failed to contact API server");
                    logger.log("Leaving");
                    throw new EliteDangerousCompanionAppException("Failed to contact API server");
                }

				
				/*
				foreach (string key in response.Headers.AllKeys)
				{
					logger.log(key + " ==> " + response.Headers[key]);
				}
				*/

                if (response.StatusCode == HttpStatusCode.Found && response.Headers["Location"] == LOGIN_URL)
                {
                    return null;
                }

                return getResponseData(response);
            }
        }

        /**
         * Try to relogin if there is some issue that requires it.
         * Throws an exception if it failed to log in.
         */
        private void relogin()
        {
            // Need to log in again.
            CurrentState = State.NEEDS_LOGIN;
            Login();
            if (CurrentState != State.READY)
            {
                logger.log("Service in incorrect state to provide profile (" + CurrentState + ")");
                logger.log("Leaving");
                throw new EliteDangerousCompanionAppIllegalStateException("Service in incorrect state to provide profile (" + CurrentState + ")");
            }
        }

        /**
         * Obtain the response data from an HTTP web response
         */
        private string getResponseData(HttpWebResponse response)
        {
            // Obtain and parse our response
            var encoding = response.CharacterSet == ""
                    ? Encoding.UTF8
                    : Encoding.GetEncoding(response.CharacterSet);

            logger.log("Reading response");
            using (var stream = response.GetResponseStream())
            {
                var reader = new StreamReader(stream, encoding);
                string data = reader.ReadToEnd();
                if (data == null || data.Trim() == "")
                {
                    logger.log("No data returned");
                    return null;
                }
                //logger.log("Data is " + data);
                return data;
            }
        }

        // Set up a request with the correct parameters for talking to the companion app
        private HttpWebRequest GetRequest(string url)
        {
            logger.log("Entered");

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            CookieContainer cookieContainer = new CookieContainer();
            AddCompanionAppCookie(cookieContainer, Credentials);
            AddMachineIdCookie(cookieContainer, Credentials);
            AddMachineTokenCookie(cookieContainer, Credentials);
            request.CookieContainer = cookieContainer;
            request.AllowAutoRedirect = false;
            request.Timeout = 10000;
            request.ReadWriteTimeout = 10000;
            request.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 7_1_2 like Mac OS X) AppleWebKit/537.51.2 (KHTML, like Gecko) Mobile/11D257";

            logger.log("Leaving");
            return request;
        }

        // Obtain a response, ensuring that we obtain the response's cookies
        private HttpWebResponse GetResponse(HttpWebRequest request)
        {
            logger.log("Entered");
            logger.log("Requesting " + request.RequestUri);

            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException wex)
            {
                logger.log("Failed to obtain response, error code " + wex.Status);
                return null;
            }
            logger.log("Response is " + JsonConvert.SerializeObject(response));
            UpdateCredentials(response);

            logger.log("Leaving");
            return response;
        }

        private void UpdateCredentials(HttpWebResponse response)
        {
            logger.log("Entered");

            // Obtain the cookies from the raw information available to us
            string cookieHeader = response.Headers[HttpResponseHeader.SetCookie];
            if (cookieHeader != null)
            {
                Match companionAppMatch = Regex.Match(cookieHeader, @"CompanionApp=([^;]+)");
                if (companionAppMatch.Success)
                {
                    Credentials.appId = companionAppMatch.Groups[1].Value;
                }
                Match machineIdMatch = Regex.Match(cookieHeader, @"mid=([^;]+)");
                if (machineIdMatch.Success)
                {
                    Credentials.machineId = machineIdMatch.Groups[1].Value;
                }
                Match machineTokenMatch = Regex.Match(cookieHeader, @"mtk=([^;]+)");
                if (machineTokenMatch.Success)
                {
                    Credentials.machineToken = machineTokenMatch.Groups[1].Value;
                }
            }

            logger.log("Leaving");
        }

        private static void AddCompanionAppCookie(CookieContainer cookies, CompanionAppCredentials credentials)
        {
            if (cookies != null && credentials.appId != null)
            {
                var appCookie = new Cookie();
                appCookie.Domain = "companion.orerve.net";
                appCookie.Path = "/";
                appCookie.Name = "CompanionApp";
                appCookie.Value = credentials.appId;
                appCookie.Secure = false;
                cookies.Add(appCookie);
            }
        }

        private static void AddMachineIdCookie(CookieContainer cookies, CompanionAppCredentials credentials)
        {
            if (cookies != null && credentials.machineId != null)
            {
                var machineIdCookie = new Cookie();
                machineIdCookie.Domain = "companion.orerve.net";
                machineIdCookie.Path = "/";
                machineIdCookie.Name = "mid";
                machineIdCookie.Value = credentials.machineId;
                machineIdCookie.Secure = true;
                // The expiry is embedded in the cookie value
                if (credentials.machineId.IndexOf("%7C") == -1)
                {
                    machineIdCookie.Expires = DateTime.Now.AddDays(7);
                }
                else
                {
                    string expiryseconds = credentials.machineId.Substring(0, credentials.machineId.IndexOf("%7C"));
                    DateTime expiryDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    try
                    {
                        expiryDateTime = expiryDateTime.AddSeconds(Convert.ToInt64(expiryseconds));
                        machineIdCookie.Expires = expiryDateTime;
                    }
                    catch (Exception)
                    {
                        logger.log("Failed to handle machine id expiry seconds " + expiryseconds);
                        machineIdCookie.Expires = DateTime.Now.AddDays(7);
                    }
                }
                cookies.Add(machineIdCookie);
            }
        }

        private static void AddMachineTokenCookie(CookieContainer cookies, CompanionAppCredentials credentials)
        {
            if (cookies != null && credentials.machineToken != null)
            {
                var machineTokenCookie = new Cookie();
                machineTokenCookie.Domain = "companion.orerve.net";
                machineTokenCookie.Path = "/";
                machineTokenCookie.Name = "mtk";
                machineTokenCookie.Value = credentials.machineToken;
                machineTokenCookie.Secure = true;
                // The expiry is embedded in the cookie value
                if (credentials.machineToken.IndexOf("%7C") == -1)
                {
                    machineTokenCookie.Expires = DateTime.Now.AddDays(7);
                }
                else
                {
                    string expiryseconds = credentials.machineToken.Substring(0, credentials.machineToken.IndexOf("%7C"));
                    DateTime expiryDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    try
                    {
                        expiryDateTime = expiryDateTime.AddSeconds(Convert.ToInt64(expiryseconds));
                        machineTokenCookie.Expires = expiryDateTime;
                    }
                    catch (Exception)
                    {
                        logger.log("Failed to handle machine token expiry seconds " + expiryseconds);
                        machineTokenCookie.Expires = DateTime.Now.AddDays(7);
                    }
                }
                cookies.Add(machineTokenCookie);
            }
        }

        /// <summary>Create a  profile given the results from a /profile call</summary>

    }
}
