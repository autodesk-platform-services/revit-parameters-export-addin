using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace RevitParametersAddin.Services
{
    public class Parameters
    { 
        public async Task<IEnumerable<dynamic>> GetHubs(string token)
        {
            var hubs = new List<Tuple<string, string>>();
            var client = new HttpClient();
            //Filter by extension type. hubs:autodesk.bim360:Account (BIM 360 Docs accounts)
            var request = new HttpRequestMessage(HttpMethod.Get, "https://developer.api.autodesk.com/project/v1/hubs?filter[extension.type]=hubs:autodesk.bim360:Account");
            request.Headers.Add("Authorization", "Bearer " + token);
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var dynamicObject = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                foreach (var field in dynamicObject.data)
                {
                    hubs.Add(new Tuple<string, string>(Convert.ToString(field.attributes.name), Convert.ToString(field.id)));
                }
            }
            return hubs;
        }

        public async Task<IEnumerable<dynamic>> GetAccounts(string token, string hubname)
        {
            var accounts = new List<Tuple<string, string>>();
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://developer.api.autodesk.com/project/v1/hubs?filter[name]-contains=" + hubname);
            request.Headers.Add("Authorization", "Bearer " + token);
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var dynamicObject = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                foreach (var field in dynamicObject.data)
                {
                    accounts.Add(new Tuple<string, string>(Convert.ToString(field.attributes.name), Convert.ToString(field.id)));
                }
            }
            return accounts;
        }

        public async Task<IEnumerable<dynamic>> GetGroups(string hubId, string token)
        {
            var groups = new List<dynamic>();
            var accountId = hubId.Replace("b.", "");
            var client = new HttpClient
            {
                BaseAddress = new Uri("https://developer.api.autodesk.com/")
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync("parameters/v1/accounts/" + accountId + "/groups");
            if (response.IsSuccessStatusCode)
            {
                var dynamicObject = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                foreach (var field in dynamicObject.results)
                {
                    groups.Add(new Tuple<string, string>(Convert.ToString(field.title), Convert.ToString(field.id)));
                }
            }
            return groups;
        }

        public async Task<IEnumerable<dynamic>> GetCollections(string hubId, string groupId, string token)
        {
            var collections = new List<dynamic>();

            var accountId = hubId.Replace("b.", "");
            var gpId = groupId.Replace("b.", "").Replace("-", "");

            var client = new HttpClient
            {
                BaseAddress = new Uri("https://developer.api.autodesk.com/")
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync("parameters/v1/accounts/" + accountId + "/groups/" + gpId + "/collections?offset=0&limit=10");
            if (response.IsSuccessStatusCode)
            {
                var dynamicObject = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                foreach (var field in dynamicObject.results)
                {
                    collections.Add(new Tuple<string, string>(Convert.ToString(field.title), Convert.ToString(field.id)));
                }
            }
            return collections;
        }

        public async Task<List<Models.ParametersViewModel>> GetParameters(string accountId, string groupId, string collectionId, string token)
        {
            List<Models.ParametersViewModel> parameters = new List<Models.ParametersViewModel>();

            var accId = accountId.Replace("b.", "");
            var gpId = groupId.Replace("b.", "").Replace("-", "");
            var colId = collectionId.Replace("b.", "").Replace("-", "");

            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, "https://developer.api.autodesk.com/parameters/v1/accounts/" + accId + "/groups/" + gpId + "/collections/" + colId + "/parameters");
                request.Headers.Add("Authorization", "Bearer " + token);
                var response = await client.SendAsync(request);                
                if (response.IsSuccessStatusCode)
                {
                    var dynamicObject = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                    string metaType = null;
                    
                    foreach (var field in dynamicObject.results)
                    {
                        string category = null;
                        foreach (dynamic meta in field.metadata)
                        {
                            if (meta.id == "instanceTypeAssociation")
                            {
                                metaType = meta.value;
                            }
                            else if(meta.id == "categories")
                            {
                                List<string> contentResults = new List<string>();
                                foreach (dynamic cat in meta.value)
                                {
                                    string catNames = cat.id;
                                    contentResults.Add(catNames.Replace("autodesk.revit.category.local:", "").Replace("autodesk.revit.category.family:", "").Replace("-1.0.0", ""));
                                }
                                category = string.Join(", ", contentResults);
                            }
                        }
                        if (!metaType.Equals("NONE")){
                            parameters.Add(new Models.ParametersViewModel() { IsSelected = false, Name = Convert.ToString(field.name), TypeOrInstance = metaType, Category = category, Id = Convert.ToString(field.id) });
                        }
                    }
                }
                return parameters;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }


        public async Task<bool> CreateParameter(string accountId, string collectionId, string parameterName, string token)
        {
            var accId = accountId.Replace("b.", "");
            var gpId = accountId.Replace("b.", "");
            var colId = collectionId.Replace("b.", "");

            var paramData = new[]
            {
                new {
                    id = Guid.NewGuid().ToString("N"),
                    name = parameterName,
                    dataTypeId = "autodesk.spec.string:url-2.0.0",
                    readOnly = false
                }
            };

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://developer.api.autodesk.com/parameters/v1/accounts/" + accId + "/groups/" + gpId + "/collections/" + colId + "/parameters");
            
            request.Headers.Add("Authorization", "Bearer " + token);

            string jsonStr = JsonConvert.SerializeObject(paramData);
            var content = new StringContent(jsonStr, null, "application/json");
            request.Content = content;            
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
