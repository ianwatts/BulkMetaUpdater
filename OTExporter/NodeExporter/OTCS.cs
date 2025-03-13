using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using CollectionExport.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Extensions;

namespace NodeExporter
{

    public class OTCS
    {
        private readonly string _rest_url = "https://cstest2.scg.cloud.opentext.com/otcs/cs.exe/api/v1";
        private readonly string _rest_url2 = "https://cstest2.scg.cloud.opentext.com/otcs/cs.exe/api/v2";
        private readonly string _userName = "SV-RDMS-ITQA-q@socalgas.com";
        private readonly string _password = "yUgfNdE#!P2oV675ZnF-";
        public AuthResponse authResponse = new AuthResponse();
        public static int _numThreads = 10;
        public OTConfig config;
        public static string otcsTicket = "";

        public OTCS(OTConfig Config)
        {
            config = Config;
            _rest_url = config.rest_url;//ConfigurationManager.AppSettings["rest_url"];
            _rest_url2 = config.rest_url2; //ConfigurationManager.AppSettings["rest_url2"];
            _userName = config.UserName;//ConfigurationManager.AppSettings["cs_user_id"];
            _password = GetBase64Decoded(config.Password);//GetBase64Decoded(ConfigurationManager.AppSettings["cs_user_pwd"]);
            _numThreads = config.NumThreads;
            otcsTicket = GetAuthCode();
        }

        public OTNode ExportNode(string nodeId, string folderPath, string otcsTicket)
        {

            OTNode otNode = new OTNode(config);

            string apiUrl = this._rest_url + "/nodes/" + nodeId + "/nodes";//?where_type=-1";
            //string otcsTicket = GetAuthCode();

            //String NodeName = GetNodeName(nodeId, otcsTicket);

            var client = new RestClient(apiUrl)
            {
                Timeout = -1
            };
            var request = new RestRequest(Method.GET);
            request.AddHeader("otcsticket", otcsTicket.Trim());
            request.AlwaysMultipartFormData = true;
            IRestResponse response = client.Execute(request);

            JObject folderStr = JObject.Parse(response.Content);
            if (folderStr.ContainsKey("error"))
            {
                String errMessage = "An error has occured retrieving specified collection";
                try
                {
                    errMessage = folderStr.SelectToken("error").Value<String>();
                }
                catch { }

                throw new Exception(errMessage);
            }

            otNode.Parse(folderStr.ToString());

            JObject[] docs = otNode.documents.ToArray();
            for (int i = 0; i <= otNode.documents.Count-1; i++)
            {
                String id = docs[i]["id"].ToString();
                String name = docs[i]["name"].ToString();
                String path = folderPath + "/" + name;
                 GetFile(Int32.Parse(id), path);
            }

            return otNode;
        }

        public void ExportCollection(List<JObject> documents, string folderPath, string otcsTicket)
        {
            for (int i = 0; i <= documents.Count - 1; i++)
            {
                String id = documents[i]["id"].ToString();
                String name = documents[i]["name"].ToString();
                String path = folderPath + "/" + name;
                GetFile(Int32.Parse(id), path);
            }

        }

        public List<Thread> threads = null;

        public OTNode GetCollection(string nodeId, OTNode otNode)
        {
            threads = new List<Thread>();
            try
            {
                SetNodeName(nodeId, otcsTicket, otNode);
            }
            catch (Exception ex)
            {

                Console.WriteLine("Assuming ticket is no longer valid, reauth and retry");
                Console.WriteLine(ex.Message);
                otcsTicket = GetAuthCode();
                SetNodeName(nodeId, otcsTicket, otNode);
            }

            string apiUrl = this._rest_url2 + "nodes/" + nodeId + "/nodes?limit=100&fields=properties{id,name,type,type_name}";//?where_type=-1";

            var client = new RestClient(apiUrl)
            {
                Timeout = -1
            };
            var request = new RestRequest(Method.GET);
            request.AddHeader("otcsticket", otcsTicket.Trim());
            request.AlwaysMultipartFormData = true;
            IRestResponse response = client.Execute(request);

            JObject folderStr = JObject.Parse(response.Content);

            int pageTotal = Int32.Parse(folderStr["collection"]["paging"]["page_total"].ToString());


            for (int page = 1; page <= pageTotal; page++)
            {
                while (GetRunningThreadCount() > _numThreads)
                {
                    Thread.Sleep(1000);
                }

                Int32 lPage = new Int32();
                lPage = page;


                Console.WriteLine("Kicking off NodeID: " + nodeId + " Page : " + lPage);
         //       GetCollection(nodeId, otcsTicket, lPage, ref otNode);
                Thread oThread = new Thread(() => GetCollection(nodeId, lPage, ref otNode));
                threads.Add(oThread);
                oThread.Start();
                oThread.Join();
                Thread.Sleep(200);


            }

            while (threads.Count > 0)
            {
                GetRunningThreadCount();
                Thread.Sleep(100);
            }

            return otNode;
        }



        public int GetRunningThreadCount()
        {
            int runningThreads = 0;
            try
            {
                foreach (Thread thread in threads)
                {
                    if (thread.IsAlive)
                        runningThreads++;
                    else
                        threads.Remove(thread);
                }
            }
            catch(Exception ex)
            {
                runningThreads = _numThreads;
            }

            return runningThreads;
        }

        private void SetNodeName(string nodeId, string otcsTicket, OTNode otNode)
        {
            string apiUrl = this._rest_url2 + "nodes/" + nodeId ;//?where_type=-1";

            var client = new RestClient(apiUrl)
            {
                Timeout = -1
            };
            var request = new RestRequest(Method.GET);
            request.AddHeader("otcsticket", otcsTicket.Trim());
            request.AlwaysMultipartFormData = true;
            IRestResponse response = client.Execute(request);

            JObject folderStr = JObject.Parse(response.Content);

            otNode.nodeName = folderStr["results"]["data"]["properties"]["name"].ToString();
        }

        public string GetContent(string NodeId)
        {

            string folderId = "";

            string apiUrl = this._rest_url2 + "nodes/" + NodeId + "/content"; //+ "/nodes?fields=properties{id,name,type,type_name}";

            var request = new RestRequest(Method.GET);
            String otcsTicket = GetAuthCode();

            request.AddHeader("otcsticket", otcsTicket.Trim());
            request.AlwaysMultipartFormData = true;

            var client = new RestClient(apiUrl);

            IRestResponse response = client.Execute(request);

            return response.Content;
        }

        public OTNode GetCollection(string nodeId, int page, ref OTNode otNode)
        {
            Console.WriteLine("GetCollection NodeID: " + nodeId + " Page: " + page);

            try
            {
                //                string apiUrl = this._rest_url2 + "nodes/" + nodeId + "/nodes?limit=100&fields=properties{id,name,type,type_name}&page=" + page;//?where_type=-1";
                string apiUrl = this._rest_url2 + "nodes/" + nodeId + "/nodes?limit=100&page=" + page;
                var client = new RestClient(apiUrl)
                {
                    Timeout = -1
                };
                var request = new RestRequest(Method.GET);
                request.AddHeader("otcsticket", otcsTicket.Trim());
                request.AlwaysMultipartFormData = true;
                Console.WriteLine("After Response GetCollection NodeID: " + nodeId + " Page: " + page);
                IRestResponse response = client.Execute(request);

                Console.WriteLine("After Response GetCollection NodeID: " + nodeId + " Page: " + page);
                JObject folderStr = JObject.Parse(response.Content);
                if (folderStr.ContainsKey("error"))
                {
                    String errMessage = "An error has occured retrieving specified collection";
                    try
                    {
                        errMessage = folderStr.SelectToken("error").Value<String>();
                    }
                    catch { }

                    throw new Exception(errMessage);
                }

                otNode.Parse(folderStr.ToString());
                foreach (JObject folder in otNode.folders)
                {
                    String folderid = folder["data"]["properties"]["id"].ToString();
                    OTNode folderNode = new OTNode(config);
                    otNode.AddChildConatainer(GetCollection(folderid, folderNode));
                    
                }

                //if(page < Int32.Parse(folderStr["page_total"].ToString())){
                //    GetCollection(nodeId, otcsTicket, (int)(page + 1), otNode);
                //}

                Console.WriteLine("End GetCollection NodeID: " + nodeId + " Page: " + page);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error GetCollection NodeID: " + nodeId + " Page: " + page);
                Console.WriteLine(ex.Message);

            }
            return otNode;
        }

        public string GetNodeName(string nodeId, string otcsTicket)
        {
            String nodeName = null;
            string apiUrl = this._rest_url2 + "/nodes/" + nodeId;

            var client = new RestClient(apiUrl)
            {
                Timeout = -1
            };
            var request = new RestRequest(Method.GET);
            request.AddHeader("otcsticket", otcsTicket.Trim());
            request.AlwaysMultipartFormData = true;
            IRestResponse response = client.Execute(request);
            JObject nodeStr = JObject.Parse(response.Content);

            return nodeStr["results"]["data"]["properties"]["name"].ToString();
        }

        public void GetFile(int NodeId, String filePath)
        {
            try
            {
                string apiUrl = this._rest_url + "nodes/" + NodeId.ToString() + "/content";//?where_type=-1";
                string otcsTicket = GetAuthCode();
                var client = new RestClient(apiUrl)
                {
                    Timeout = -1
                };
                var request = new RestRequest(Method.GET);
                request.AddHeader("otcsticket", otcsTicket.Trim());

                request.AlwaysMultipartFormData = true;
                IRestResponse response = client.Execute(request);

                client.DownloadData(request).SaveAs(filePath);
            }
            catch(Exception ex)
            {
                Console.WriteLine("NodeId : " + NodeId + ", FilePath : " + filePath + " , " + ex.Message);
                throw ex;
            }
                
        }

        public string GetAuthCode()
        {
            if (IsCSTicketExpired(authResponse.OTcsTicket))
            {
                var client = new RestClient(_rest_url + "/auth")
                {
                    Timeout = -1
                };
                var request = new RestRequest(Method.POST)
                {
                    AlwaysMultipartFormData = true
                };
                request.AddParameter("username", _userName);
                request.AddParameter("password", _password);
                IRestResponse response = client.Execute(request);
                // RDMSSyncService.RDMSLogger(response.Content,gEx,"debug");

                dynamic api = JObject.Parse(response.Content);
                string otcsTicket = api.ticket;
                authResponse.OTcsTicket = otcsTicket;
                authResponse.TicketCreationTime = DateTime.Now;
                return otcsTicket;
            }
            else
            {
                return authResponse.OTcsTicket;
            }

        }

        public Boolean IsCSTicketExpired(string otcsTicket)
        {
            Boolean ticketExpired = true;
            if (otcsTicket != null)
            {
                try
                {
                    string strUrl = _rest_url + "/auth";
                    var client = new RestClient(strUrl)
                    {
                        Timeout = -1
                    };
                    //GetAuthCode
                    var request = new RestRequest(Method.GET);
                    request.AddHeader("otcsticket", otcsTicket.Trim());
                    IRestResponse response = client.Execute(request);

                    dynamic apiRespoonse = JObject.Parse(response.Content);
                    if (!string.IsNullOrWhiteSpace(apiRespoonse.data.name))
                    {
                        return false;
                    }


                    Console.WriteLine(response.Content);
                }
                catch (Exception ex)
                {
                    return ticketExpired;
                }
            }
            return ticketExpired;
        }

        public string GetBase64Decoded(string base64Encoded)
        {
            string base64Decoded;
            byte[] data = System.Convert.FromBase64String(base64Encoded);
            base64Decoded = System.Text.ASCIIEncoding.ASCII.GetString(data);
            return base64Decoded;
        }
    }

    public class AuthResponse
    {
        public string OTcsTicket { get; set; }
        public DateTime TicketCreationTime { get; set; }
    }
}
