using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CollectionExport.Models
{
    public class OTConfig
    {
        public String rest_url { get; set; }
        public String rest_url2 { get; set; }
        public String UserName { get; set; }
        public String Password { get; set; }
        public String RootExportFolder { get; set; }
        public String DocumentTypeNames { get; set; }
        public int NumThreads { get; set; }
        public int MaxFileSizeBytes { get; set; }

        public static OTConfig GetOTConfig()
        {
            var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var configuration = builder.Build();

            // Get values from the config given their key and their target type.
            OTConfig settings = configuration.GetSection("OTConfig").Get<OTConfig>();


            return settings;
        }

        private static string DecodeBase64String(string base64String)
        {
            byte[] decodedBytes = Convert.FromBase64String(base64String);
            string decodedText = Encoding.UTF8.GetString(decodedBytes);

            return decodedText;
        }
    }
}
