using Autodesk.Authentication;
using Autodesk.Authentication.Model;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace RevitParametersAddin.TokenHandlers
{
    /// <summary>
    /// Defines the <see cref="OAuthHandler" />.
    /// </summary>
    internal class OAuthHandler
    {
        // Initialize the oAuth 2.0 client configuration fron enviroment variables
        // you can also hardcode them in the code if you want in the placeholders below

        private static AuthenticationClient authenticationClient = new AuthenticationClient();

        /// <summary>
        /// Defines the PORT.
        /// </summary>
        private static string PORT = Environment.GetEnvironmentVariable("PORT") ?? "3000";

        /// <summary>
        /// Defines the FORGE_CALLBACK.
        /// </summary>
        private static string FORGE_CALLBACK = Environment.GetEnvironmentVariable("FORGE_CALLBACK") ?? "http://localhost:" + PORT + "/api/aps/callback/oauth";

        /// <summary>
        /// Defines the _Scopes.
        /// </summary>
        private static List<Scopes> _Scopes = new List<Scopes>() { Scopes.AccountRead, Scopes.DataCreate, Scopes.DataWrite, Scopes.DataRead, Scopes.BucketRead };

        // Intialize the 3-legged oAuth 2.0 client.
        /// <summary>
        /// Defines the _threeLeggedApi.
        /// </summary>
        private static ThreeLeggedToken _threeLeggedApi = new ThreeLeggedToken();

        // Declare a local web listener to wait for the oAuth callback on the local machine.
        // Please read this article to configure your local machine properly
        // http://stackoverflow.com/questions/4019466/httplistener-access-denied
        //   ex: netsh http add urlacl url=http://+:3000/auth
        // Embedded webviews are strongly discouraged for oAuth - https://developers.google.com/identity/protocols/OAuth2InstalledApp
        /// <summary>
        /// Defines the _httpListener.
        /// </summary>
        private static HttpListener _httpListener = null;

        /// <summary>
        /// The AccessTokenDelegate.
        /// </summary>
        /// <param name="bearer">The bearer<see cref="dynamic"/>.</param>
        public delegate void AccessTokenDelegate(ThreeLeggedToken bearer);

        /// <summary>
        /// Defines the config.
        /// </summary>
        private static Forge config;

        /// <summary>
        /// The Create.
        /// </summary>
        /// <param name="forgeConfiguration">The forgeConfiguration<see cref="ForgeConfiguration"/>.</param>
        /// <returns>The <see cref="OAuthHandler"/>.</returns>
        public static OAuthHandler Create(Forge forgeConfiguration)
        {
            config = forgeConfiguration;
            return new OAuthHandler();
        }

        /// <summary>
        /// The Invoke3LeggedOAuth.
        /// </summary>
        /// <param name="cb">The cb<see cref="AccessTokenDelegate"/>.</param>
        public void Invoke3LeggedOAuth(AccessTokenDelegate cb)
        {
            _3leggedAsync(cb);
        }

        /// <summary>
        /// Gets or sets the InternalToken.
        /// </summary>
        private static dynamic InternalToken { get; set; }

        /// <summary>
        /// Get the access token from Autodesk.
        /// </summary>
        /// <param name="scopes">The scopes<see cref="Scopes[]"/>.</param>
        /// <returns>The <see cref="Task{dynamic}"/>.</returns>
        static async Task<dynamic> Get2LeggedTokenAsync(List<Scopes> scopes)
        {
            string grantType = "client_credentials";
            dynamic bearer = await authenticationClient.GetTwoLeggedTokenAsync(config.ClientId,
              config.ClientSecret,
              scopes);
            return bearer;
        }

        /// <summary>
        /// The GetInternalAsync.
        /// </summary>
        /// <returns>The <see cref="Task{dynamic}"/>.</returns>
        public async Task<dynamic> GetInternalAsync()
        {
            if (InternalToken == null || InternalToken.ExpiresAt < DateTime.UtcNow)
            {
                InternalToken = await Get2LeggedTokenAsync(new List<Scopes>() { Scopes.BucketCreate,
                                                                        Scopes.BucketRead,
                                                                        Scopes.BucketDelete,
                                                                        Scopes.DataRead,
                                                                        Scopes.DataWrite,
                                                                        Scopes.DataCreate,
                                                                        Scopes.CodeAll });
                InternalToken.ExpiresAt = DateTime.UtcNow.AddSeconds(InternalToken.expires_in);
            }
            return InternalToken;
        }

        /// <summary>
        /// The GetChromeExe.
        /// </summary>
        /// <returns>The <see cref="string"/>.</returns>
        private static string GetChromeExe()
        {
            bool isWindows = System.Runtime.InteropServices.RuntimeInformation
                                               .IsOSPlatform(OSPlatform.Windows);
            if (!isWindows)
            {
                return null;
            }
            const string suffix = @"Google\Chrome\Application\chrome.exe";
            var prefixes = new List<string> { Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) };
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (programFilesx86 != programFiles)
            {
                prefixes.Add(programFiles);
            }
            else
            {
                if (Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion",
                    "ProgramW6432Dir", null) is string programFilesDirFromReg)
                {
                    prefixes.Add(programFilesDirFromReg);
                }

            }

            prefixes.Add(programFilesx86);
            var path = prefixes.Distinct().Select(prefix => Path.Combine(prefix, suffix)).FirstOrDefault(File.Exists);
            return path;
        }

        /// <summary>
        /// The _3leggedAsync.
        /// </summary>
        /// <param name="cb">The cb<see cref="AccessTokenDelegate"/>.</param>
        internal static void _3leggedAsync(AccessTokenDelegate cb)
        {
            if (!HttpListener.IsSupported)
                return;// HttpListener is not supported on this platform. // Initialize our web listener
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(FORGE_CALLBACK.Replace("localhost", "+") + "/");
            try
            {
                _httpListener.Start();
                IAsyncResult result = _httpListener.BeginGetContext(_3leggedAsyncWaitForCode, cb);
                // Generate a URL page that asks for permissions for the specified Scopess, and call our default web browser.
                string oauthUrl = authenticationClient.Authorize(config.ClientId, ResponseType.Code, redirectUri: FORGE_CALLBACK.ToString(), scopes: _Scopes);
                var file = GetChromeExe();
                var userData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "tmp_chrome");
                var args = $"/incognito --chrome-frame --user-data-dir={userData} --window-size=540,540 --app={oauthUrl} --disable-application-cache";

                ProcessStartInfo startInfo = new ProcessStartInfo(file)
                {
                    WindowStyle = ProcessWindowStyle.Minimized,
                    Arguments = args,
                };
                var p = Process.Start(startInfo);
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }


        /// <summary>
        /// The _3leggedAsyncWaitForCode.
        /// </summary>
        /// <param name="ar">The ar<see cref="IAsyncResult"/>.</param>
        internal static async void _3leggedAsyncWaitForCode(IAsyncResult ar)
        {
            try
            {
                // Our local web listener was called back from the Autodesk oAuth server
                // That means the user logged properly and granted our application access
                // for the requested scope.
                // Let's grab the code from the URL and request or final access_token

                //HttpListener listener =(HttpListener)result.AsyncState ;
                var context = _httpListener.EndGetContext(ar);
                string code = context.Request.QueryString["code"];

                // The code is only to tell the user, he can close is web browser and return
                // to this application.
                var responseString = "<html><body><h2>Login Success</h2><p>You can now close this window!</p></body></html>";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                var response = context.Response;
                response.ContentType = "text/html";
                response.ContentLength64 = buffer.Length;
                response.StatusCode = 200;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
                // Now request the final access_token
                if (!string.IsNullOrEmpty(code))
                {
                    // Call the asynchronous version of the 3-legged client with HTTP information
                    // HTTP information will help you to verify if the call was successful as well
                    // as read the HTTP transaction headers.
                    var bearer = await authenticationClient.GetThreeLeggedTokenAsync(config.ClientId, code, FORGE_CALLBACK, config.ClientSecret);

                    ((AccessTokenDelegate)ar.AsyncState)?.Invoke(bearer);
                }
                else
                {
                    ((AccessTokenDelegate)ar.AsyncState)?.Invoke(null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ((AccessTokenDelegate)ar.AsyncState)?.Invoke(null);
            }
            finally
            {
                _httpListener.Stop();
            }
        }
    }
}