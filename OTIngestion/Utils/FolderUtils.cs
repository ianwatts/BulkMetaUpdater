using Newtonsoft.Json.Linq;
using NLog;
using OTIngestion.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OTIngestion.Utils
{
    public class FolderUtils
    {
        internal static Dictionary<string, string> ensuredFolders = null;

        //public String otcsTicket;
        public FolderUtils()
        {
            //Get and ensure ticket
        }

        //Ensures the Path is created and returns the NodeId of the Leaf
        public string EnsurePath(string path)
        {
            string leafFolderId = string.Empty;

            if (ensuredFolders == null)
            {
                ensuredFolders = new Dictionary<string, string>();
            }

            if (ensuredFolders.ContainsKey(path))
            {
                leafFolderId = ensuredFolders[path];
            }
            else
            {
                NlogUtils.Logger.Info($"Retrieving folder id '{path}'");

                string[] folders = path.Split(OTIngestion.AutoDocumentPlacement.AutoDocumentPlacement.FOLDER_SEPARATOR);

                //Start in the Enterprise Workspace
                Folder rootFolder = new Folder();
                rootFolder.Name = "Enterprise";
                rootFolder.Id = 2000;

                Folder currentFolder = rootFolder;
                var currFolder = string.Empty;

                for (int i = 1; i < folders.Length; i++)
                {
                    var folder = folders[i];
                    currFolder += $"{OTIngestion.AutoDocumentPlacement.AutoDocumentPlacement.FOLDER_SEPARATOR}{folder}";

                    if (ensuredFolders.ContainsKey(currFolder))
                    {
                        currentFolder = new Folder()
                        {
                            Name = folder,
                            Id = Convert.ToInt32(ensuredFolders[currFolder])
                        };
                    }
                    else
                    {
                        currentFolder = EnsureFolder(currentFolder, folder);
                        if (!ensuredFolders.ContainsKey(currFolder))
                        {
                            ensuredFolders.Add(currFolder, currentFolder.Id.ToString());
                        }
                    }
                }

                leafFolderId = currentFolder.Id.ToString();

                if(!ensuredFolders.ContainsKey(path))
                {
                    ensuredFolders.Add(path, leafFolderId);
                }
            }

            return leafFolderId;
        }

        public Folder EnsureFolder(Folder parent, string childName)
        {
            OTCSUtils otcsUtils = new OTCSUtils();
            Folder folder = new Folder();

            string folderId = FolderExists(parent.Id, childName);

            if(string.IsNullOrEmpty(folderId))
            {
                throw new Exception($"Folder '{childName}' was not available");
            }

            folder.Id = int.Parse(folderId);
            folder.Name = childName;

            parent.subFolders.Add(folder);
            return folder;
        }

        public string FolderExists(int ParentId, string ChildName)
        {
            OTCSUtils otcsUtils = new OTCSUtils();
            string folderId = string.Empty;


            string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v2/nodes/" + ParentId.ToString() + "/nodes?where_name=" + ChildName + "&fields=properties{id,name,type,type_name}";

            //string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v2/nodes/" + otConfig.nodeId + "/nodes?where_name=" + fileName + "&fields=categories";

            var request = otcsUtils.GenerateRestRequest(Method.Get);
            var client = otcsUtils.GenerateRestClient(apiUrl);

            try
            {
                RestResponse response = client.Execute(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    JObject nodeStr = JObject.Parse(response.Content);

                    //http:\\jsonpath.com\
                    JToken folderNode = nodeStr.SelectToken("results[?(@.data.properties.name=='" + ChildName + "')].data.properties.id");

                    if (folderNode != null)
                    {
                        folderId = folderNode.ToString();
                    }
                    else
                    {
                        folderId = CreateFolder(ParentId, ChildName);
                    }
                }
                else
                {
                    NlogUtils.Logger.Error(response.ErrorMessage);
                    throw new Exception(response.ErrorMessage);
                }

                return folderId;
            }
            catch(Exception ex)
            {
                NlogUtils.Logger.Error(ex.Message);
                throw;
            }
        }

        public static String MoveNode(String ParentId, String NodeId)
        {
            OTCSUtils otcsUtils = new OTCSUtils();
            String folderId = "";


            string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v1/nodes/" + NodeId;

            var request = otcsUtils.GenerateRestRequest(Method.Put);
            request.AddParameter("body", "{\"parent_id\": " + ParentId.ToString() + "}");

            var client = otcsUtils.GenerateRestClient(apiUrl);

            RestResponse response = client.Execute(request);
            JObject nodeStr = JObject.Parse(response.Content);

            //http:\\jsonpath.com\
            JToken folderNode = nodeStr.SelectToken("data.properties.parent_id");

            if (folderNode != null)
                folderId = folderNode.ToString();

            return folderId;
        }
        public string CreateFolder(int ParentId, string ChildName)
        {
            OTCSUtils otcsUtils = new OTCSUtils();
            string folderId = string.Empty;

            string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v2/nodes";
            
            var request = otcsUtils.GenerateRestRequest(Method.Post);
            request.AddParameter("body", " {\"type\": 0,\"name\": \"" + ChildName + "\",\"parent_id\": " + ParentId.ToString() + "}");
       

            var client = otcsUtils.GenerateRestClient(apiUrl);

            RestResponse response = client.Execute(request);

            if (response != null && response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                JObject nodeStr = JObject.Parse(response.Content);

                //http:\\jsonpath.com\
                JToken folderNode = nodeStr.SelectToken("results.data.properties.id");

                if (folderNode != null)
                    folderId = folderNode.ToString();
            }
            else
            {
                NlogUtils.Logger.Error($"Can't create folder {ChildName}");
                throw new Exception($"Can't create folder {ChildName}");
            }
            return folderId;
        }
    }
}