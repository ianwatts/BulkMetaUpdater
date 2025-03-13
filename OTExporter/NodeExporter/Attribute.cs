using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeExporter
{
    public class Attribute
    {
        public string TypeName { get; set; }
        public object Value { get; set; }
        public string CatRowId { get; set; }

        public string ValidationSQL { get; set; }
    }
}
