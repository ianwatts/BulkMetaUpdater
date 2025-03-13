using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTIngestion.Models
{
    public class Property
    {
        public string ID { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public bool Required { get; private set; }
        //public bool Readonly { get; private set; }

        public Property(string id, string title, string type, bool required)
        {
            ID = id;
            Title = title;
            Type = type;
            Required = required;
        }
        /*
        public Property(string id, string title, string type, bool required, bool readOnly)
        {
            ID = id;
            Title = title;
            Type = type;
            Required = required;
            Readonly = readOnly;
        }
        */
    }
}