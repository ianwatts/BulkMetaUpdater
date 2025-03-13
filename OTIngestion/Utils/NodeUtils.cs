using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTIngestion.Utils
{
    public class NodeUtils
    {
        public static JObject GetNodeChildren(String NodeId, int page, String additionalProperties = "")
        {
            OTCSUtils otcsUtils = new OTCSUtils();
            String folderId = "";


            string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v2/nodes/" + NodeId + "/nodes?limit=100&fields=properties{id,name,type,type_name" + additionalProperties + "}&page=" + page;

            var request = otcsUtils.GenerateRestRequest(Method.Get);
            var client = new RestClient(apiUrl);

            RestResponse response = client.Execute(request);
            JObject jNode = JObject.Parse(response.Content);


            return jNode;
        }

        public static JObject GetNode(String NodeId)
        {
            OTCSUtils otcsUtils = new OTCSUtils();
            string folderId = "";


            string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v2/nodes/" + NodeId; //+ "/nodes?fields=properties{id,name,type,type_name}";

            var request = otcsUtils.GenerateRestRequest(Method.Get);
            var client = new RestClient(apiUrl);

            RestResponse response = client.Execute(request);
            JObject jNode = JObject.Parse(response.Content);


            return jNode;
        }

        public static string GetContent(string NodeId)
        {
            OTCSUtils otcsUtils = new OTCSUtils();
            string folderId = "";

            string apiUrl = $"{OTConfig.GetOTConfig().rest_url}/api/v2/nodes/{NodeId}/content"; //+ "/nodes?fields=properties{id,name,type,type_name}";

            var request = otcsUtils.GenerateRestRequest(Method.Get);
            var client = new RestClient(apiUrl);

            RestResponse response = client.Execute(request);

            return response.Content;
        }

        public static void EnsureCategories(string nodeId, Dictionary<string, string> categories)
        {
            JObject jDoc = NodeUtils.GetNode(nodeId);

            //populateCategories
            JToken jCats = jDoc["results"]["data"]["categories"];
            JArray jaCats = null;

            if (jCats is JArray)
            {
                jaCats = (JArray)jCats;
            }

            for (int i = 0; i < categories.Keys.Count; i++)
            {
                Console.WriteLine("Cat Id :" + categories.Keys.ToList<string>()[i]);
                string catId = categories.Keys.ToList<string>()[i].Trim();
                bool hasCat = false;
                //Does Cat Exist on Doc
                if (jaCats != null)
                {


                    for (int j = 0; j < jaCats.Count; j++)
                    {
                        IList<object> categoriesText = jaCats[j].Select(c => (object)c).ToList();
                        //Get Item category id
                        string itemCatId = categoriesText[0].ToString().Replace("\"", "").Split('_')[0].Trim();

                        if (catId == itemCatId)
                        {
                            hasCat = true;
                            break;
                        }
                    }
                }

                if (!hasCat)
                {
                    //Add Category to document

                }

            }


        }
    }
}