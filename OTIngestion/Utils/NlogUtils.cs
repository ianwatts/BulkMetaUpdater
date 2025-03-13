using Microsoft.Extensions.Configuration;
using NLog;
using NLog.Extensions.Logging;


namespace OTIngestion.Utils
{
    public class NlogUtils
    {
        private static ILogger logger = null;
        public static ILogger Logger
        {
            get
            {
                if (logger == null)
                {
                    var config = new ConfigurationBuilder().SetBasePath(System.IO.Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();
                    //LogManager.Configuration = new NLogLoggingConfiguration(config.GetSection("NLog"));
                    logger = LogManager.Setup().LoadConfigurationFromSection(config).GetCurrentClassLogger();
                }

                return logger;
            }
            private set
            {
                logger = value;
            }
        }
    }
}