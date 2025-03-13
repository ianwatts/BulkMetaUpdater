using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTIngestion.Models
{
    public class Folder
    {
        public Folder()
        {
            subFolders = new List<Folder>();
        }

        public int Id { get; set; }
        public string Name { get; set; }

        public List<Folder> subFolders { get; set; }
    }
}
