using DocumentFormat.OpenXml.Drawing.Charts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OTIngestion.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace OTIngestion.Utils
{
    public class CategoryUtils
    {
        public CategoryUtils()
        {
            //Get and ensure ticket
        }

        public static String ApplyCategory(int NodeId, int CatId)
        {
            OTCSUtils otcsUtils = new OTCSUtils();

            string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v1/nodes/" + NodeId.ToString() + "/categories";
            var client = otcsUtils.GenerateRestClient(apiUrl);

            var request = otcsUtils.GenerateRestRequest(Method.Post);
            request.AddParameter("body", " {\"category_id\": " + CatId.ToString() + "}");

            RestResponse response = client.Execute(request);
            return response.StatusDescription;
        }

        public static Root GetCategoryByNodeId(int nodeId)
        {
            var response = new Root();
            try
            {
                var otcsUtils = new OTCSUtils();
                string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v1/nodes/" + nodeId + "/categories";
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("otcsticket", otcsUtils.GetAuthCode());
                var httpresponse = httpClient.GetAsync(apiUrl).Result;
                if (httpresponse.IsSuccessStatusCode)
                {
                    var httpresultresponse = httpresponse.Content.ReadAsStringAsync().Result;
                    response = JsonConvert.DeserializeObject<Root>(httpresultresponse);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return response;
        }


        public static string ApplyCategoriesToNode(int nodeId, Document document)
        {
            var response = "";
            try
            {
                var otcsUtils = new OTCSUtils();
                string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v1/nodes/" + nodeId + "/categories";
                var catergory = GetCategoryByNodeId(7567951); //get the main folder
                var getpropeties = GetCategoryProperties(catergory.data[0].id);


                var request = "{\"category_id\":7363757,\"7363757_2\":\"new value\"}";
                var json = JsonConvert.SerializeObject(request);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("otcsticket", otcsUtils.GetAuthCode());
                var httpresponse = httpClient.PostAsync(apiUrl, httpContent).Result;
                if (httpresponse.IsSuccessStatusCode)
                {
                    return httpresponse.Content.ReadAsStringAsync().Result;
                }
            }
            catch (Exception ex)
            {

                throw;
            }
            return response;
        }


        public static bool UpdateCategoriesToNode(int nodeId, int categoryId, Document document)
        {
            var response = false;
            try
            {
                var otcsUtils = new OTCSUtils();
                string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v1/nodes/" + nodeId + "/categories/" + categoryId;
                var request = GetCatergoryValue(document, categoryId);
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("otcsticket", otcsUtils.GetAuthCode());
                var httpresponse = httpClient.PutAsync(apiUrl, new FormUrlEncodedContent(request)).Result;
                if (httpresponse.IsSuccessStatusCode)
                {
                    return httpresponse.IsSuccessStatusCode;
                }
                return response;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }


        public static Dictionary<string, string> GetCatergoryValue(Document document, int categoryId)
        {
            var request = new Dictionary<string, string>();

            var getpropeties = GetCategoryProperties(categoryId);

            var attributes = document.Attributes;

            foreach (var attribute in attributes)
            {
                var value = attribute.Value.Value;
                var key = getpropeties.FirstOrDefault(x => x.Key.ToLower() == attribute.Key.ToLower());
                if (key.Key != null && value != null)
                {
                    request.Add(key.Value.ID, value?.ToString());

                }
            }
            return request;
        }


        public static Dictionary<string, Property> GetCategoryProperties(int categoryId)
        {
            var result = new Dictionary<string, Property>();
            var otcsUtils = new OTCSUtils();

            string apiUrl = string.Format("{0}/api/v1/forms/nodes/properties/specific?id={1}", OTConfig.GetOTConfig().rest_url, categoryId);

            var request = otcsUtils.GenerateRestRequest(Method.Get);
            var client = otcsUtils.GenerateRestClient(apiUrl);

            RestResponse response = client.Execute(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {

                var nodeStr = JObject.Parse(response.Content);

                dynamic api = JObject.Parse(response.Content);
                dynamic forms = api.forms;

                if (forms.Count == 1)
                {
                    dynamic properties = forms[0].schema.properties;
                    foreach (var property in properties)
                    {

                        var id = property.Name;
                        var title = property.Value.title.Value;
                        var type = property.Value.type.Value;
                        // var readOnly = property.Value..readonly.Value;
                        var required = property.Value.required.Value;
                        // add validation 
                        result.Add(title.ToLower(), new Property(id, title, type, required));
                    }
                }
            }
            else
            {
                // Exception
            }

            return result;
        }
    }
}