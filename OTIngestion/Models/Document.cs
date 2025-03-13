using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using OTIngestion.Utils;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTIngestion.Models
{
    public class Document : ICloneable
    {
        public static Dictionary<string, CategoryDef> categoryDefs = null;
        public static Dictionary<string, string> attrIdsNames = null;


        public Document()
        {
            Attributes = new Dictionary<string, Attribute>();

            if (categoryDefs == null)
            {
                attrIdsNames = new Dictionary<string, string>();
                categoryDefs = new Dictionary<string, CategoryDef>();

                //Get Cat Configs
                var settings = OTConfig.GetOTConfig();
                foreach (var c in settings.Categories)
                {
                    string nodeId = c.Value;
                    CategoryDef categoryDef = CategoryDef.GetCategoryDef(nodeId);
                    foreach(var catAttr in categoryDef.Attributes)
                    {
                        string attrName = catAttr.Key;
                        attrIdsNames.Add(categoryDef.Attributes[attrName].CatRowId, attrName);
                    }

                    categoryDefs.Add(nodeId, categoryDef);
                }
            }

            foreach(var categoryDef in categoryDefs.Values)
            {
                foreach (var catAttr in categoryDef.Attributes)
                {
                    string displayName = catAttr.Key;
                    OTIngestion.Models.Attribute catDefAttr = catAttr.Value;
                    OTIngestion.Models.Attribute attribute = new OTIngestion.Models.Attribute();

                    attribute.CatRowId = catDefAttr.CatRowId;
                    //TODO: Get the types
                    this.Attributes.Add(displayName, attribute);
                }
            }
        }

        // TODO : check if it needed
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] Content { get; set; }
        public string Path { get; set; }

        public Dictionary<string, Attribute> Attributes;

        public string GetAttributeType(string attrName)
        {
            return Attributes[attrName].TypeName;
        }

        //Probably need to abstract this more to handle overrides, etc
        public object GetAttributeValue(string attrName)
        {
            return Attributes[attrName].Value;
        }
        
        public void AddAttribute(string attrName, Attribute attr)
        {
            if (attrIdsNames.ContainsValue(attrName))
            {
                string attrKey = attrIdsNames.FirstOrDefault(x => x.Value == attrName).Key;
                attr.CatRowId = attrKey;
            }
            if (Attributes.ContainsKey(attrName))
            {
                Attributes[attrName] = attr;
            }
            else
            {
                Attributes.Add(attrName, attr);
            }
            
        }

        private string GetTypeByName(string fileName)
        {
            var result = "144";

            if (fileName.ToLower().EndsWith(".msg"))
            {
                result = "749";
            }

            return result;
        }



        public UploadDocumentResponse UploadDocument(int folderId)
        {
            NlogUtils.Logger.Info($"Begin Upload {this.Name}");

            //var success = string.Empty;
            var uploadDocumentResponse = new UploadDocumentResponse();

            if (folderId == 0)
            {
                NlogUtils.Logger.Error("FolderId can not be empty");
                uploadDocumentResponse.Error("FolderId can not be empty");
            }
            else
            {
                try
                {
                    // Temporary Path to save the file
                    var tempFolder = "TmpFiles";
                    var filepath = $"{tempFolder}\\{this.Name}";
                    var otcsUtils = new OTCSUtils();

                    var request = otcsUtils.GenerateRestRequest(Method.Post);
                    request.Timeout = -1;

                    request.AddParameter("type", GetTypeByName(this.Name));

                    request.AddParameter("parent_id", folderId);
                    request.AddParameter("name", this.Name);
                    // Add HTTP Headers
                    request.AddHeader("Content-Type", "multipart/form-data");

                    if (this.Content != null && this.Content.Length > 0)
                    {
                        if (!Directory.Exists(tempFolder))
                        {
                            Directory.CreateDirectory(tempFolder);
                        }
                        File.WriteAllBytes(filepath, this.Content);
                        request.AddFile("file", filepath); // MIME type will be calculated by OT
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(this.Path))
                        {
                            if (File.Exists(this.Path))
                            {
                                request.AddFile("file", this.Path);
                            }
                            else
                            {
                                NlogUtils.Logger.Error($"File was not found: {this.Name}");
                            }
                        }
                        else
                        {
                            NlogUtils.Logger.Error($"Document '{this.Name}' has no content");
                            uploadDocumentResponse.Error($"Document '{this.Name}' has no content");
                            return uploadDocumentResponse;
                        }
                    }

                    string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v1/nodes";
                    var client = otcsUtils.GenerateRestClient(apiUrl);

                    var response = client.Execute(request);
                    if (response.IsSuccessful)
                    {
                        var nodeStr = JObject.Parse(response.Content);
                        try
                        {
                            Id = int.Parse(nodeStr["id"].ToString());
                            NlogUtils.Logger.Info($"Node Id: {Id}");
                            uploadDocumentResponse.Successful(Id);
                        }
                        catch (Exception ex)
                        {
                            NlogUtils.Logger.Error($"Error from reading from Json. {ex.Message}");
                        }
                    }
                    else
                    {
                        var responceError = string.Empty;
                        try
                        {
                            var nodeStr = JObject.Parse(response.Content);
                            responceError = nodeStr["error"].ToString();
                        }
                        catch (Exception exc)
                        {

                        }
                        NlogUtils.Logger.Error($"Error uploading Document. {responceError}");
                        uploadDocumentResponse.Error($"Error uploading Document. {responceError}");
                    }

                    try
                    {
                        if (File.Exists(filepath))
                        {
                            File.Delete(filepath);
                        }
                    }
                    catch (Exception ex)
                    {
                        NlogUtils.Logger.Error($"Error from deleting the file. {ex.Message}");
                    }
                }
                catch(Exception ex)
                {
                    NlogUtils.Logger.Error($"Error uploading Document. {ex.Message}");
                    uploadDocumentResponse.Error($"Error uploading Document. {ex.Message}");
                }
            }

            return uploadDocumentResponse;
        }

        public UploadDocumentResponse UploadDocument(string folderPath)
        {
            //PCMS Folder Path Logic
            FolderUtils folderUtils = new FolderUtils();
            var uploadDocumentResponse = new UploadDocumentResponse();

            var leafFolderId = string.Empty;
            try
            {
                leafFolderId = folderPath.StartsWith(OTIngestion.AutoDocumentPlacement.AutoDocumentPlacement.FOLDER_SEPARATOR)
                    ? folderUtils.EnsurePath(folderPath)
                    : "Search by alias was not implemented";
                leafFolderId = leafFolderId.Trim();
            }
            catch(Exception ex)
            {
                NlogUtils.Logger.Error($"Can't retrieve folder id. {ex.Message}");
                uploadDocumentResponse.Error(ex.Message);
                return uploadDocumentResponse;
            }

            return UploadDocument(Convert.ToInt32(leafFolderId));
        }

        public bool DeleteDocument()
        {
            bool success = false;
            try
            {
                var otcsUtils = new OTCSUtils();
                string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v1/nodes/" + Id;
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("otcsticket", otcsUtils.GetAuthCode());
                var response = httpClient.DeleteAsync(apiUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    NlogUtils.Logger.Info($"Success from delete document {Id}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                NlogUtils.Logger.Error("Error from delete document" + ex.Message);
                throw;// TODO handle correctly
            }
            return success;
        }

        public bool DeleteDocument(int nodeId)
        {
            bool success = false;
            try
            {

                var otcsUtils = new OTCSUtils();
                string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v1/nodes/" + nodeId;
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("otcsticket", otcsUtils.GetAuthCode().Trim());
                var response = httpClient.DeleteAsync(apiUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    NlogUtils.Logger.Info("Success from delete document");
                    return true;
                }
            }
            catch (Exception ex)
            {
                NlogUtils.Logger.Error("Error from delete document" + ex.Message);
                throw;
            }
            return success;
        }

        public string UpdateAttibutes()
        {
            string retVal = string.Empty;

            List<string> processedCats = new List<string>();

            List<string> attrNames = Attributes.Keys.ToList<string>();
            try
            {
                foreach (var attrName in attrNames)
                {
                    if (Attributes[attrName].CatRowId != null && Attributes[attrName].CatRowId.Length > 0)
                    {
                        var catId = Attributes[attrName].CatRowId.Split("_")[0];
                        if (!processedCats.Contains(catId))
                        {
                            processedCats.Add(catId);
                            retVal = UpdateCategory(catId);

                            if (!string.IsNullOrEmpty(retVal))
                            {
                                NlogUtils.Logger.Error($"Error updating category {catId} for the document {Id}. {retVal}");
                                retVal = $"Error updating category {catId} for the document {Id}. {retVal}";

                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NlogUtils.Logger.Error($"Error updating category for the document {Id}. {ex.Message}");
                retVal = $"Error updating category for the document {Id}. {ex.Message}";
            }

            return retVal;
        }

        private bool HasCategory(string catId)
        {
            OTCSUtils otcsUtils = new OTCSUtils();
            string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v1/nodes/" + this.Id + "/categories";

            var request = otcsUtils.GenerateRestRequest(Method.Get);
            var client = otcsUtils.GenerateRestClient(apiUrl);

            RestResponse response = client.Execute(request);
            JObject nodeStr = JObject.Parse(response.Content);

            //http:\\jsonpath.com\
            JToken catNode = nodeStr.SelectToken("$.data[?(@.id==" + catId + ")].id");

            if(catNode == null)
            {
                //Add Category
                //CategoryUtils.ApplyCategory(this.Id, Int32.Parse(catId));
                return false;
            }
            return true;
        }

        private void AddCategories(string categoryId, RestRequest request)
        {
            List<String> attrNames = Attributes.Keys.ToList<String>();
            for (int i = 0; i < attrNames.Count; i++)
            {
                String attrName = attrNames[i];
                if (Attributes[attrName].CatRowId != null && Attributes[attrName].CatRowId.StartsWith(categoryId))
                {

                    try
                    {
                        if (Attributes[attrName].Value != null)
                        {

                            String attrValue = Attributes[attrName].Value.ToString().Replace("\\","");
                            //Check DateTime
                            DateTime attrDate;
                            if (DateTime.TryParse(attrValue, out attrDate))
                            {
                                attrValue = attrDate.ToString("O");
                            }
                            request.AddParameter(Attributes[attrName].CatRowId, attrValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

            }

        }

        private string AddCategoryBody(string categoryId, string body)
        {
            List<String> attrNames = Attributes.Keys.ToList<String>();
            foreach(var attrName in attrNames)
            {
                if (Attributes[attrName].CatRowId != null && Attributes[attrName].CatRowId.StartsWith(categoryId))
                {
                    try
                    {
                        if (Attributes[attrName].Value != null)
                        {
                            var attrValue = Attributes[attrName].Value;
                            if (attrValue != null && !string.IsNullOrEmpty(attrValue.ToString()))
                            {
                                if (!string.IsNullOrEmpty(body))
                                    body += ",";

                                switch (Attributes[attrName].TypeName)
                                {
                                    case "Date":
                                        //Check DateTime
                                        DateTime attrDate;
                                        if (DateTime.TryParse(attrValue.ToString(), out attrDate))
                                        {
                                            attrValue = $"\"{attrDate.ToString("O")}\"";
                                        }
                                        break;
                                    case "Array":
                                        var lists = attrValue.ToString().Split(",");
                                        lists = (from e in lists
                                                 select e.Trim()).ToArray();
                                        attrValue = JsonConvert.SerializeObject(lists);
                                        break;
                                    default:
                                        attrValue = JsonConvert.SerializeObject(attrValue);
                                        break;
                                }

                                body += $"\"{Attributes[attrName].CatRowId}\":{attrValue}";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

            }

            return body;
        }

        internal string UpdateCategoryBody(string categoryId)
        {
            var retVal = string.Empty;
            var body = string.Empty;

            OTCSUtils otcsUtils = new OTCSUtils();
            string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v2/nodes/" + this.Id.ToString() + "/categories";

            var hasCategory = HasCategory(categoryId);
            var request = otcsUtils.GenerateRestRequest(hasCategory ? Method.Put : Method.Post);
            if (!hasCategory)
            {
                request.AddParameter("category_id", categoryId);
            }
            else
            {
                apiUrl += "/" + categoryId;
            }

            foreach (var attribute in Attributes)
            {
                var attr = attribute.Value;

                if (attr.CatRowId != null && attr.CatRowId.StartsWith(categoryId))
                {
                    try
                    {
                        if (attr.Value != null)
                        {
                            var attrValue = attr.Value;
                            if (attrValue != null && !string.IsNullOrEmpty(attrValue.ToString()))
                            {
                                if (!string.IsNullOrEmpty(body))
                                    body += ",";

                                switch (attr.TypeName)
                                {
                                    case "Date":
                                        //Check DateTime
                                        DateTime attrDate;
                                        if (DateTime.TryParse(attrValue.ToString(), out attrDate))
                                        {
                                            attrValue = $"\"{attrDate.ToString("O")}\"";
                                        }
                                        break;
                                    case "Array":
                                        var lists = attrValue.ToString().Split(",");
                                        lists = (from e in lists
                                                 select e.Trim()).ToArray();
                                        attrValue = JsonConvert.SerializeObject(lists);
                                        break;
                                    default:
                                        attrValue = JsonConvert.SerializeObject(attrValue);
                                        break;
                                }

                                body += $"\"{attr.CatRowId}\":{attrValue}";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        retVal = ex.Message;
                        return retVal;
                    }
                }
            }

            try
            {
                request.AddParameter("body", "{" + body + "}");
                var client = otcsUtils.GenerateRestClient(apiUrl);
                RestResponse response = client.Execute(request);

                JObject nodeStr = JObject.Parse(response.Content);

                if (nodeStr.SelectToken("error") == null)
                {
                    retVal = string.Empty;
                }
                else
                {
                    retVal = nodeStr.SelectToken("error").ToString();
                }
            }
            catch (Exception ex)
            {
                retVal= ex.Message;
            }

            return retVal;
        }

        internal string UpdateCategoryParameters(string categoryId)
        {
            var retVal = string.Empty;

            OTCSUtils otcsUtils = new OTCSUtils();
            string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v2/nodes/" + this.Id.ToString() + "/categories";

            var hasCategory = HasCategory(categoryId);
            var request = otcsUtils.GenerateRestRequest(hasCategory ? Method.Put : Method.Post);
            if (!hasCategory)
            {
                request.AddParameter("category_id", categoryId);
            }
            else
            {
                apiUrl += "/" + categoryId;
            }

            foreach (var attribute in Attributes)
            {
                var attr = attribute.Value;

                if (attr.CatRowId != null && attr.CatRowId.StartsWith(categoryId))
                {
                    try
                    {
                        if (attr.Value != null)
                        {
                            var attrValue = attr.Value;
                            if (attrValue != null && !string.IsNullOrEmpty(attrValue.ToString()))
                            {
                                switch (attr.TypeName)
                                {
                                    case "Date":
                                        //Check DateTime
                                        DateTime attrDate;
                                        if (DateTime.TryParse(attrValue.ToString(), out attrDate))
                                        {
                                            //attrValue = $"\"{attrDate.ToString("O")}\"";
                                            request.AddParameter(attr.CatRowId, attrDate.ToString("O"));
                                        }
                                        break;
                                    case "Array":
                                        var lists = attrValue.ToString().Split(",");
                                        foreach(var l in lists)
                                        {
                                            request.AddParameter(attr.CatRowId, l.Trim());
                                        }
                                        attrValue = JsonConvert.SerializeObject(lists);
                                        break;
                                    default:
                                        request.AddParameter(attr.CatRowId, attrValue.ToString());
                                        break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        retVal = ex.Message;
                        return retVal;
                    }
                }
            }

            try
            {
                var client = otcsUtils.GenerateRestClient(apiUrl);
                RestResponse response = client.Execute(request);

                JObject nodeStr = JObject.Parse(response.Content);

                if (nodeStr.SelectToken("error") == null)
                {
                    retVal = string.Empty;
                }
                else
                {
                    retVal = nodeStr.SelectToken("error").ToString();
                }
            }
            catch (Exception ex)
            {
                retVal = ex.Message;
            }

            return retVal;
        }

        public string UpdateCategory(string categoryId)
        {
            return UpdateCategoryBody(categoryId);
        }
        public string UpdateCategory(string categoryId, bool useParams)
        {
            return useParams ? UpdateCategoryParameters(categoryId) : UpdateCategoryBody(categoryId);
        }

        public static bool UpdateCategoriesToNode(int nodeId, int categoryId, Document document)
        {
            var response = false;
            try
            {
                var otcsUtils = new OTCSUtils();
                string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v1/nodes/" + nodeId + "/categories/" + categoryId;
                var request = CategoryUtils.GetCatergoryValue(document, categoryId);
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
    }

    public class UploadDocumentResponse
    {
        public int NodeId { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsSuccess { get; set; }

        public void Error(string message)
        {
            ErrorMessage = message;
            NodeId = 0;
            IsSuccess = false;
        }

        public void Successful(int nodeId) 
        {
            ErrorMessage = string.Empty;
            NodeId = nodeId;
            IsSuccess = true;
        }

    }
}