using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OTIngestion.Models;

namespace OTIngestion.AutoDocumentPlacement
{
    public interface AutoDocumentPlacement
    {
        public const string FOLDER_SEPARATOR = "//";
        //Create Folder Path logic based on document
        public string GetFolderPath(Document doc);
    }
}