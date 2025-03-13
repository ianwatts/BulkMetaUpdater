using CollectionExport.Models;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace NodeExporter
{
    public class NodeExporter
    {
        public String NodeStatus = "No Node Specified";
        OTCS otcs = null;
        String otcsTicket = null;
        String nodeId = null;
        public String folderPath = null;
 //       public List<FileStatus> exportList = null;
        public int totalNumberOfFiles = 0;
        public int exportedNumber;
        public int exported = 0;
        public string fileName = "";
        public Thread thread = null;
        public String collectionName = "";
        OTNode coll = null;
        public List<String> notExported = new List<String>();
        public static Dictionary<String,CategoryDef> Categories = new Dictionary<String,CategoryDef>();
        OTConfig otConfig;
        public NodeExporter(String NodeId, OTConfig config, OTNode rootNode)
        {
            otConfig = config;
            otcs = new OTCS(config);
            otcsTicket = otcs.GetAuthCode();
            nodeId = NodeId;
            folderPath = config.RootExportFolder;
            //            exportList = new List<FileStatus>();
            exportedNumber = 0;
            collectionName = otcs.GetNodeName(nodeId, otcsTicket);
            coll = rootNode;

            totalNumberOfFiles = getNumberOfFiles(coll);


            folderPath = folderPath + "\\" + coll.nodeName;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            ExportNodes(coll, folderPath);
        }


        public NodeExporter(String NodeId, OTConfig config)
        {
            otConfig = config;
            otcs = new OTCS(config);
            otcsTicket = otcs.GetAuthCode();
            nodeId = NodeId;
            folderPath = config.RootExportFolder;
//            exportList = new List<FileStatus>();
            exportedNumber = 0;
            collectionName = otcs.GetNodeName(nodeId, otcsTicket);

            
            coll = new OTNode(config);
            coll = otcs.GetCollection(nodeId, coll);
            totalNumberOfFiles = getNumberOfFiles(coll);


            folderPath = folderPath + "\\" + coll.nodeName;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            ExportNodes(coll, folderPath);
            //thread = new Thread(ExportNodes);
            //thread.Start();
            //thread.Join();
        }

        public int getNumberOfFiles(OTNode node)
        {
            int numFiles = 0;
            numFiles = node.documents.Count;
            foreach(OTNode folder in node.childContainerNodes)
            {
                numFiles += getNumberOfFiles(folder);
            }
            return numFiles;
        }


        public JObject GetDisplayNameJSON(JObject doc)
        {
            JObject displayDoc = new JObject();
            displayDoc.Add(new JProperty("id", doc["data"]["properties"]["id"]));
            displayDoc.Add(new JProperty("name", doc["data"]["properties"]["name"]));
            //displayDoc.Add(new JProperty("file_type", doc["data"]["properties"]["file_type"]));
            JArray categories = new JArray();
            foreach (var cat in doc["data"]["categories"])
            {
                JObject jCat = new JObject();
                int i = 0;
                JArray props = new JArray();
                foreach (var att in cat)
                {

                    //String catID = att["Name"];
                    JProperty prop = (JProperty)att;
                    String cat_id = prop.Name.Split("_")[0];
                    String att_id = prop.Name.Split("_")[1];

                    if (!Categories.ContainsKey(cat_id))
                    {
                        CategoryDef catDef = CategoryDef.GetCategoryDef(cat_id, otcs);
                        Categories.Add(cat_id, catDef);
                    }

                    CategoryDef lcatDef = Categories[cat_id];
                    dynamic jAtt = new JObject();

                    if (i == 0)
                    {
                        jCat.Add(new JProperty("CatName", new JValue(lcatDef.Name)));
                        jCat.Add(new JProperty("CatId", new JValue(cat_id)));
                    }

                    jAtt.AttId = att_id;
                    jAtt.AttName = lcatDef.Attributes.FirstOrDefault(x => x.Value.CatRowId == prop.Name).Key;
                    jAtt.Value = prop.Value;
                    props.Add(jAtt);
                    i++;
                }

                jCat.Add("attributes", props);
                categories.Add(jCat);
            }
            displayDoc.Add(new JProperty("categories", categories));



            return displayDoc;
        }
        public List<Thread> threads = new List<Thread>();

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
            catch (Exception ex)
            {
                runningThreads = otConfig.NumThreads;
            }

            return runningThreads;
        }

        public void ExportNodes(OTNode node, String nodePath)
        {
            foreach(var doc in node.documents)
            {
                while (GetRunningThreadCount() > otConfig.NumThreads)
                {
                    Thread.Sleep(200);
                }
                Thread oThread = new Thread(() => ProcessDocument(nodePath, doc));
                threads.Add(oThread);
                oThread.Start();
                //oThread.Join();
                Thread.Sleep(200);
            }

            //while (threads.Count > 0)
            //{
            //    GetRunningThreadCount();
            //    Thread.Sleep(100);
            //}

            //using (var semaphore = new SemaphoreSlim(otConfig.NumThreads))
            //{

            //    var tasks = node.documents.Select(async (doc) =>
            //    {
            //        await semaphore.WaitAsync();
            //        try
            //        {

            //            await ProcessDocument(nodePath, doc);
            //        }
            //        finally
            //        {
            //            semaphore.Release();
            //        }
            //    }).ToArray();
            //}

            JObject[] folders = node.folders.ToArray();
            for (int i=0;i<node.folders.Count; i++)
            {
                try
                {
                    String newPath = nodePath + "\\" + folders[i]["data"]["properties"]["name"].ToString() ;
                    if (!Directory.Exists(newPath))
                    {
                        Directory.CreateDirectory(newPath);
                    }
                    OTNode childNode = null;
                    for (int j=0;j< node.childContainerNodes.Count; j++)
                    {
                        if(node.childContainerNodes.ToArray()[j].nodeName == folders[i]["data"]["properties"]["name"].ToString())
                        {
                            childNode = node.childContainerNodes.ToArray()[j];
                            break;
                        }
                    }

                    ExportNodes(childNode, newPath);
                }
                catch(Exception ex)
                {

                }
            }

            if (!Directory.EnumerateFileSystemEntries(nodePath).Any())
            {
                Directory.Delete(nodePath);
            }

            async Task ProcessDocument(string nodePath, JObject doc)
            {
                String filePath = nodePath + "\\" + doc["data"]["properties"]["name"].ToString();


                String id = doc["data"]["properties"]["id"].ToString();
                String name = doc["data"]["properties"]["name"].ToString();
                Int64 size = Int64.Parse(doc["data"]["properties"]["size"].ToString());

                if (size < otConfig.MaxFileSizeBytes)
                {
                    foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                    {
                        name = name.Replace(c, '_');
                    }

                    String path = nodePath + "\\" + name;
                    fileName = path;
                    try
                    {
                        if (!File.Exists(path))
                        {
                            otcs.GetFile(Int32.Parse(id), path);
                        }
                        else
                        {
                            ThreadSafeWriteLine(path + " : Already Exists");
                            notExported.Add(path + " : Already Exists");
                        }
                        String fileNoExt = System.IO.Path.GetFileNameWithoutExtension(path);
                        String metaFile = nodePath + "\\" + fileNoExt + "._metadata";

                        if (!File.Exists(metaFile))
                        {
                            JObject displayDoc = GetDisplayNameJSON(doc);
                            using (var fileStream = new FileStream(metaFile, FileMode.Create))
                            {
                                using (var textWriter = new StreamWriter(fileStream))
                                {
                                    using (var jsonWriter = new JsonTextWriter(textWriter))
                                    {
                                        jsonWriter.Formatting = Formatting.Indented;
                                        displayDoc.WriteTo(jsonWriter);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        notExported.Add(path + " : Not Exported reason : " + ex.Message);
                    }
                }
                else
                {
                    notExported.Add(filePath + " : Not Exported reason : File too large");
                }
            }
        }


        private static readonly object consoleLock = new object();

        public void ThreadSafeWriteLine(string message)
        {
            lock (consoleLock)
            {
                Console.WriteLine(message);
            }
        }
    }

    ////public class FileStatus
    ////{
    ////    public JObject doc;
    ////    public Boolean isExported;
    ////}
}
