using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.Text;

namespace OTIngestion
{
    public class OTConfig
    {
        #region Properties

        public Dictionary<string, string> Categories = new();
        public Dictionary<string, string> ConnectionStrings = new();
        public string rest_url { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        //Tried to override the get function on Password but the Configuration class needed it to be pure
        //So here is a DecodedPassword function

        public string DecodedPassword
        {
            get 
            {
                return DecodeBase64String(Password); 
            }
        }

        #endregion Properties 

        #region Methods

        private static string DecodeBase64String(string base64String)
        {
            byte[] decodedBytes = Convert.FromBase64String(base64String);
            string decodedText = Encoding.UTF8.GetString(decodedBytes);

            return decodedText;
        }

        public static OTConfig GetOTConfig()
        {
            var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var configuration = builder.Build();

            // Get values from the config given their key and their target type.
            OTConfig settings = configuration.GetSection("OTConfig").Get<OTConfig>();


            foreach (var item in configuration.GetSection("Categoreies").GetChildren())
            {
                settings.Categories.Add(item.Key, item.Value);
            }

            foreach (var item in configuration.GetSection("ConnectionStrings").GetChildren())
            {
                settings.ConnectionStrings.Add(item.Key, DecodeBase64String(item.Value));
            }

            return settings;
        }

        private static IServiceProvider BuildDi(IConfiguration config)
        {
            return new ServiceCollection()
               //Add DI Classes here
               .AddTransient<OTConfig>() // Runner is the custom class
               .AddLogging(loggingBuilder =>
               {
                   // configure Logging with NLog
                   loggingBuilder.ClearProviders();
                   loggingBuilder.AddNLog(config);
               })
               .BuildServiceProvider();
        }

        #endregion Methods
    }
}