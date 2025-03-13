using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTIngestion.Models
{
    public class CellMetadata
    {
        public MenuData data { get; set; }
        public Definitions definitions { get; set; }
    }


    public class MenuData
    {
        public string menu { get; set; }
    }

    public class Data
    {
        public string menu { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public CellMetadata cell_metadata { get; set; }
    }

    public class Definitions
    {
        public Menu menu { get; set; }
        public Id id { get; set; }
        public Name name { get; set; }
    }

    public class DefinitionsMap
    {
        public List<string> name { get; set; }
    }

    public class Id
    {
        public string key { get; set; }
        public string name { get; set; }
        public string persona { get; set; }
        public int type { get; set; }
        public int width_weight { get; set; }
    }

    public class Menu
    {
        public string body { get; set; }
        public string content_type { get; set; }
        public string display_hint { get; set; }
        public string display_href { get; set; }
        public string handler { get; set; }
        public string image { get; set; }
        public string method { get; set; }
        public string name { get; set; }
        public Parameters parameters { get; set; }
        public string tab_href { get; set; }
    }

    public class Name
    {
        public string key { get; set; }
        public string name { get; set; }
        public string persona { get; set; }
        public int type { get; set; }
        public int width_weight { get; set; }
    }

    public class Parameters
    {
    }

    public class Root
    {
        public List<Data> data { get; set; }
        public Definitions definitions { get; set; }
        public DefinitionsMap definitions_map { get; set; }
        public List<string> definitions_order { get; set; }
    }

    //public class ApplyCategoryRequest
    //{
    //    public int type { get; set; }
    //    public int parent_id { get; set; } 
    //    public string name { get; set; }
    //    public List<string> roles { get; set; }
    //}

    public class Roles
    {
        public Categories categories { get; set; }
    }

    public class CategoryId
    {
        public List<string> CategoryChildId { get; set; }
    }

    public class Categories
    {
        public CategoryId CategoryId { get; set; }
    }

    public class ApplyCategoryRequest
    {
        public int type { get; set; }
        public string parent_id { get; set; }
        public string name { get; set; }
        public Roles roles { get; set; }
    }
}
