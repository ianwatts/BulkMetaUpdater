using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Options;
using CollectionExport.Models;

namespace NodeExporter
{
    public class OTNode
    {
        public JObject jNode;
        public ConcurrentBag<JObject> folders;
        public ConcurrentBag<JObject> documents;
        public ConcurrentBag<OTNode> childContainerNodes; //Even though this is the Node folders, it could be workspaces, etc
        public String nodeName;
        public OTConfig config;
        public OTNode(OTConfig Config)
        {
            folders = new ConcurrentBag<JObject>();
            documents = new ConcurrentBag<JObject>();
            childContainerNodes = new ConcurrentBag<OTNode>();
            config = Config;
        }


        public void AddDocument(JObject item)
        {
                documents.Add(item);
        }

        public void AddFolder(JObject item)
        {
                folders.Add(item);
        }

        public void AddChildConatainer(OTNode item)
        {
                childContainerNodes.Add(item);
        }


        public void SortAllDocuments()
        {
            
            foreach (JObject childNode in jNode["results"].Children())
            {
                String typeName = childNode["data"]["properties"]["type_name"].ToString();
                Console.WriteLine(typeName);
                switch (typeName)
                {
                    case "Folder":
                        AddFolder(childNode);
                        break;
                    case var expression when (config.DocumentTypeNames.Contains(typeName)):
                        
                        AddDocument(childNode);
                        break;
                    default:
                        Console.WriteLine(typeName);
                        break;
                }
            }
        }

        public void Parse(String json)
        {
            jNode = JObject.Parse(json);
            SortAllDocuments();
           
        }


    }
}