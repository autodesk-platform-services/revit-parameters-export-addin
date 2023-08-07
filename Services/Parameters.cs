using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Autodesk.Revit.DB.Visual;

namespace RevitParametersAddin.Services
{
    public class Parameters
    { 
        public async Task<IEnumerable<dynamic>> GetHubs(string token)
        {
            //var hubs = new List<string>();
            var hubs = new List<Tuple<string, string>>();
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://developer.api.autodesk.com/project/v1/hubs");
            request.Headers.Add("Authorization", "Bearer " + token);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var dynamicObject = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
            var result = new Dictionary<string, string>();
            foreach (var field in dynamicObject.data)
            {
                hubs.Add(new Tuple<string, string>(Convert.ToString(field.attributes.name), Convert.ToString(field.id)));
                //result.Add(Convert.ToString(field.attributes.name), Convert.ToString(field.id));
            }
            return hubs;
        }

        public async Task<IEnumerable<dynamic>> GetAccounts(string token, string hubname)
        {
            var accounts = new List<Tuple<string, string>>();
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://developer.api.autodesk.com//project/v1/hubs?filter[name]-contains=" + hubname);
            request.Headers.Add("Authorization", "Bearer " + token);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var dynamicObject = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
            var result = new Dictionary<string, string>();
            foreach (var field in dynamicObject.data)
            {
                accounts.Add(new Tuple<string, string>(Convert.ToString(field.attributes.name), Convert.ToString(field.id)));
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
        

        public async Task<IEnumerable<dynamic>> GetParameters(string accountId, string groupId, string collectionId, string token)
        {
            var parameters = new List<dynamic>();
            var accId = accountId.Replace("b.", "");
            var gpId = groupId.Replace("b.", "").Replace("-", "");
            var colId = collectionId.Replace("b.", "").Replace("-", "");
            try
            {
                var client1 = new HttpClient();
                var request1 = new HttpRequestMessage(HttpMethod.Get, "https://developer.api.autodesk.com/parameters/v1/accounts/" + accId + "/groups/" + gpId + "/collections/" + colId + "/parameters");
                request1.Headers.Add("Authorization", "Bearer " + token);
                request1.Headers.Add("Cookie", "PF=QJwvb8Hfm5ValfxedgOkRw");
                var response = await client1.SendAsync(request1);                
                if (response.IsSuccessStatusCode)
                {
                    var dynamicObject = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                    string metaType = null;
                    foreach (var field in dynamicObject.results)
                    {
                        foreach (dynamic meta in field.metadata)
                        {
                            if (meta.id == "instanceTypeAssociation")
                            {
                                metaType = meta.value;
                            }
                        }
                        if (!metaType.Equals("NONE")){
                            parameters.Add(new Tuple<string, string, string>(Convert.ToString(field.name), metaType, Convert.ToString(field.id)));
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
            

            var client = new HttpClient
            {
                BaseAddress = new Uri("https://developer.api.autodesk.com/")
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var values=new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("id", Guid.NewGuid().ToString("N")));
            values.Add(new KeyValuePair<string, string>("name", parameterName));
            values.Add(new KeyValuePair<string, string>("dataTypeId", "autodesk.spec:spec.bool-1.0.0"));
            values.Add(new KeyValuePair<string, string>("readOnly", "false"));
            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync("parameters/v1/accounts/" + accId + "/groups/" + gpId + "/collections/" + gpId + "/parameters", content);
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
