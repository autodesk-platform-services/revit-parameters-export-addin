
using Autodesk.Forge;
using Autodesk.Forge.Model;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using RevitParametersAddin.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace RevitParametersAddin.TokenHandlers
{
    public class Forge
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

    public class ForgeConfiguration
    {
        public Forge Forge { get; set; }
    }

    

    public class TokenHandler
    {
        public static string Login()
        {
            try
            {
                //This will get the current WORKING directory(i.e. \bin\Debug)
                string currentUserDirectory = Environment.CurrentDirectory;
                string token = string.Empty;
                var forgeConfiguration = JsonConvert.DeserializeObject<ForgeConfiguration>(File.ReadAllText(Path.Combine(Directory.GetParent(currentUserDirectory).Parent.FullName, @"appsettings.json")));
                if (string.IsNullOrEmpty(forgeConfiguration.Forge.ClientId) || string.IsNullOrEmpty(forgeConfiguration.Forge.ClientSecret))
                {
                    TaskDialog.Show("Login Error", "ClientId or ClientSecret is not set");
                    throw new Exception("ClientId or ClientSecret is not set");
                }
                else
                {
                    var oAuthHandler = OAuthHandler.Create(forgeConfiguration.Forge);

                    //We want to sleep the thread until we get 3L access_token.
                    //https://stackoverflow.com/questions/6306168/how-to-sleep-a-thread-until-callback-for-asynchronous-function-is-received
                    AutoResetEvent stopWaitHandle = new AutoResetEvent(false);
                    oAuthHandler.Invoke3LeggedOAuth(async (bearer) =>
                    {
                        // This is our application delegate. It is called upon success or failure
                        // after the process completed
                        if (bearer == null)
                        {
                            TaskDialog.Show("Login Response", "Sorry, Authentication failed! 3legged test");
                            return;
                        }
                        token = bearer.access_token;
                        // The call returned successfully and you got a valid access_token.                
                        DateTime dt = DateTime.Now;
                        dt.AddSeconds(double.Parse(bearer.expires_in.ToString()));
                        UserProfileApi profileApi = new UserProfileApi();
                        profileApi.Configuration.AccessToken = bearer.access_token;
                        DynamicJsonResponse userResponse = await profileApi.GetUserProfileAsync();
                        UserProfile user = userResponse.ToObject<UserProfile>();
                        TaskDialog.Show("Login Response", $"Hello {user.FirstName} !!, You are Logged in!");
                        stopWaitHandle.Set();
                    });
                    stopWaitHandle.WaitOne();                    
                }
                return token;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
                TaskDialog.Show("Login Error", "Access Denied");
                throw new Exception("Login Error");
            }
        }



        public static string GetAssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }


        public async Task<string> Get2LeggedForgeToken()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("https://developer.api.autodesk.com");
            var request = new HttpRequestMessage(HttpMethod.Post, "/authentication/v1/authenticate");

            // This will get the current WORKING directory (i.e. \bin\Debug)
            string currentUserDirectory = Environment.CurrentDirectory;

            var forgeConfiguration = JsonConvert.DeserializeObject<ForgeConfiguration>(
                File.ReadAllText(Path.Combine(Directory.GetParent(currentUserDirectory).Parent.FullName, @"appsettings.json")));

            var credentials = new Dictionary<string, string>();
            credentials.Add("client_id", forgeConfiguration.Forge.ClientId);//Your client ID from https://aps.autodesk.com/myapps/ here
            credentials.Add("client_secret", forgeConfiguration.Forge.ClientSecret);//Your client ID from https://aps.autodesk.com/myapps/ here
            credentials.Add("grant_type", "client_credentials");
            credentials.Add("scope", "code:all data:create data:write data:read bucket:create bucket:delete");

            var content = new FormUrlEncodedContent(credentials);
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var rep = JsonConvert.DeserializeObject<TokenModel>(await response.Content.ReadAsStringAsync());
            return rep.access_token;
        }
    }
}
