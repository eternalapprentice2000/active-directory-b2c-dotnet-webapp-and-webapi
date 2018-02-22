using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace TaskWebApp.Business
{
    public class B2CGraphClient
    {
        private string Tenant { get; }

        private readonly AuthenticationContext _authContext;
        private readonly ClientCredential _credential;

        public class AdB2CGroups
        {
            [JsonProperty("objectId")]
            public string ObjectId { get; set; }

            [JsonProperty("displayName")]
            public string DisplayName { get; set; }
        }

        public class GetAllGroupsResponse
        {
            [JsonProperty("value")]
            public List<AdB2CGroups> Groups { get; set; }
        }

        public class GetUserGroupsResponse
        {
            [JsonProperty("value")]
            public List<string> GroupIds { get; set; }
        }

        public B2CGraphClient(string clientId, string clientSecret, string tenant)
        {
            // The tenant are pulled in from the web.config file
            this.Tenant = tenant;

            // The AuthenticationContext is ADAL's primary class, in which you indicate the direcotry to use.
            this._authContext = new AuthenticationContext("https://login.microsoftonline.com/" + tenant);

            // The ClientCredential is where you pass in your client_id and client_secret, which are 
            // provided to Azure AD in order to receive an access_token using the app's identity.
            this._credential = new ClientCredential(clientId, clientSecret);
        }

        public B2CGraphClient()
            : this(ConfigurationManager.AppSettings["b2c:ClientId"],
                    ConfigurationManager.AppSettings["b2c:ClientSecret"], 
                    ConfigurationManager.AppSettings["b2c:Tenant"]
            ) {}

        public string GetAllGroups()
        {
            //todo: probably should cache and convert this result here
            return SendGraphGetRequest("/groups", null);
        }

        public async Task<List<AdB2CGroups>> GetUserGroups(string userResourceId)
        {
            var groups = this.GetAllGroups();
            var groupsObj = JsonConvert.DeserializeObject<GetAllGroupsResponse>(groups);

            var url = $"/users/{userResourceId}/getMemberGroups";
            var postData = "{\"securityEnabledOnly\":true}";
            var response = await SendGraphPostRequest(url, postData);

            var userObj = JsonConvert.DeserializeObject<GetUserGroupsResponse>(response);

            var combinedResult = new List<AdB2CGroups>();

            foreach (var groupId in userObj.GroupIds)
            {
                var group = groupsObj.Groups.FirstOrDefault(x => x.ObjectId == groupId);
                if (group != null)
                {
                    combinedResult.Add(group);
                }
            }

            return combinedResult;
        }

        private async Task<string> SendGraphDeleteRequest(string api)
        {
            // NOTE: This client uses ADAL v2, not ADAL v4

            AuthenticationResult result = await _authContext.AcquireTokenAsync(Globals.aadGraphResourceId, _credential);  //authContext.AcquireToken(Globals.aadGraphResourceId, credential);
            HttpClient http = new HttpClient();
            string url = Globals.aadGraphEndpoint + Tenant + api + "?" + Globals.aadGraphVersion;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            HttpResponseMessage response = await http.SendAsync(request);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("DELETE " + url);
            Console.WriteLine("Authorization: Bearer " + result.AccessToken.Substring(0, 80) + "...");
            Console.WriteLine("");

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                object formatted = JsonConvert.DeserializeObject(error);
                throw new WebException("Error Calling the Graph API: \n" + JsonConvert.SerializeObject(formatted, Formatting.Indented));
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine((int)response.StatusCode + ": " + response.ReasonPhrase);
            Console.WriteLine("");

            return await response.Content.ReadAsStringAsync();
        }

        private async Task<string> SendGraphPatchRequest(string api, string json)
        {
            // NOTE: This client uses ADAL v2, not ADAL v4
            AuthenticationResult result = await _authContext.AcquireTokenAsync(Globals.aadGraphResourceId, _credential);  //authContext.AcquireToken(Globals.aadGraphResourceId, credential);
            HttpClient http = new HttpClient();
            string url = Globals.aadGraphEndpoint + Tenant + api + "?" + Globals.aadGraphVersion;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("PATCH " + url);
            Console.WriteLine("Authorization: Bearer " + result.AccessToken.Substring(0, 80) + "...");
            Console.WriteLine("Content-Type: application/json");
            Console.WriteLine("");
            Console.WriteLine(json);
            Console.WriteLine("");

            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("PATCH"), url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                object formatted = JsonConvert.DeserializeObject(error);
                throw new WebException("Error Calling the Graph API: \n" + JsonConvert.SerializeObject(formatted, Formatting.Indented));
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine((int)response.StatusCode + ": " + response.ReasonPhrase);
            Console.WriteLine("");

            return await response.Content.ReadAsStringAsync();
        }

        private async Task<string> SendGraphPostRequest(string api, string json)
        {
            // NOTE: This client uses ADAL v2, not ADAL v4
            AuthenticationResult result = await _authContext.AcquireTokenAsync(Globals.aadGraphResourceId, _credential);  //authContext.AcquireToken(Globals.aadGraphResourceId, credential);
            HttpClient http = new HttpClient();
            string url = Globals.aadGraphEndpoint + Tenant + api + "?" + Globals.aadGraphVersion;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("POST " + url);
            Console.WriteLine("Authorization: Bearer " + result.AccessToken.Substring(0, 80) + "...");
            Console.WriteLine("Content-Type: application/json");
            Console.WriteLine("");
            Console.WriteLine(json);
            Console.WriteLine("");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                object formatted = JsonConvert.DeserializeObject(error);
                throw new WebException("Error Calling the Graph API: \n" + JsonConvert.SerializeObject(formatted, Formatting.Indented));
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine((int)response.StatusCode + ": " + response.ReasonPhrase);
            Console.WriteLine("");

            return await response.Content.ReadAsStringAsync();
        }

        public string SendGraphGetRequest(string api, string query)
        {
            // First, use ADAL to acquire a token using the app's identity (the credential)
            // The first parameter is the resource we want an access_token for; in this case, the Graph API.
            AuthenticationResult result = _authContext.AcquireTokenAsync(Globals.aadGraphResourceId, _credential).Result;  //authContext.AcquireToken("https://graph.windows.net", credential);

            // For B2C user managment, be sure to use the 1.6 Graph API version.
            HttpClient http = new HttpClient();
            string url = "https://graph.windows.net/" + Tenant + api + "?" + Globals.aadGraphVersion;
            if (!string.IsNullOrEmpty(query))
            {
                url += "&" + query;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("GET " + url);
            Console.WriteLine("Authorization: Bearer " + result.AccessToken.Substring(0, 80) + "...");
            Console.WriteLine("");

            // Append the access token for the Graph API to the Authorization header of the request, using the Bearer scheme.
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            HttpResponseMessage response = http.SendAsync(request).Result;

            if (!response.IsSuccessStatusCode)
            {
                string error = response.Content.ReadAsStringAsync().Result;
                object formatted = JsonConvert.DeserializeObject(error);
                throw new WebException("Error Calling the Graph API: \n" + JsonConvert.SerializeObject(formatted, Formatting.Indented));
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine((int)response.StatusCode + ": " + response.ReasonPhrase);
            Console.WriteLine("");

            return response.Content.ReadAsStringAsync().Result;
        }
    }


}