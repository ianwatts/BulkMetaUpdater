using CollectionExport.Models;
using System;
using Microsoft.Extensions.Options;
using NodeExporter;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Runtime.Serialization.Formatters.Binary;
using DocumentFormat.OpenXml.Office2013.Word;
using System.IO;
using Newtonsoft.Json.Linq;

namespace OTExporter 
{
    internal class Program
    {
        private static OTConfig config;
        public static void Main(string[] args)
        {
            OTNode rootNode;
            config = OTConfig.GetOTConfig();

            String filePath = "D:\\Temp\\LastPush.json";
            bool serialize = true;
            bool export = false;


            // Read the JSON file
            string jsonFilePath = filePath; 
            string jsonContent = File.ReadAllText(jsonFilePath);

            // Parse the JSON
            JObject rootObject = JObject.Parse(jsonContent);

            // Create the exporter and process the documents
            var exporter = new DocumentCsvExporter("OutputFiles");
            var exportedFiles = exporter.ExportDocuments(rootObject);

            Console.WriteLine($"Successfully exported {exportedFiles.Count} CSV files.");

            if (serialize)
            {
                String nodeId = "9173213";
                rootNode = SerializeRoot(filePath, nodeId);
            }

            if (export)
            {

                using (StreamReader r = new StreamReader(filePath))
                {
                    //string json = r.ReadToEnd();
                    //rootNode = JsonConvert.DeserializeObject<OTNode>(json);
                    using (var reader = new JsonTextReader(r))
                    {
                        var serializer = new JsonSerializer();
                        rootNode = serializer.Deserialize<OTNode>(reader);
                    }
                }

                String nodeid = rootNode.jNode["results"][0]["data"]["properties"]["parent_id"].ToString();

                NodeExporter.NodeExporter nodeExporter = new NodeExporter.NodeExporter(nodeid, config, rootNode);
            }
		}

		private static OTNode SerializeRoot(string filePath, string nodeId)
        {
            OTNode rootNode = new OTNode(config);
            OTCS otcs = new OTCS(config);

            rootNode = otcs.GetCollection(nodeId, rootNode);
            try
            {
				using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
				using (var writer = new StreamWriter(stream))
				using (var jsonWriter = new JsonTextWriter(writer))
				{
					var serializer = new JsonSerializer();
					serializer.Serialize(jsonWriter, rootNode);
				}

				//String json = JsonConvert.SerializeObject(rootNode);
    //            File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return rootNode;
        }


    }
}