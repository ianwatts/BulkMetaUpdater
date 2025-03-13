using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NodeExporter
{
    public class CategoryDef
    {
        public string Name { get; set; }
        public string Id { get; set; }

        public Dictionary<string, Attribute> Attributes;


        public CategoryDef()
        {
            Attributes = new Dictionary<string, Attribute>();
        }

        public static CategoryDef GetCategoryDef(string NodeId, OTCS otcs)
        {
            CategoryDef categoryDef = new CategoryDef();
            
            string CatSerialization = otcs.GetContent(NodeId);
            int startPos = CatSerialization.IndexOf("<1");
            CatSerialization = CatSerialization.Substring(35);

            MatchCollection CatNameIDs = Regex.Matches(CatSerialization, @"'DisplayName'='(.*?)'.*?'ID'=(\d+).*?");
            MatchCollection CatSQL = Regex.Matches(CatSerialization, @"'DisplayName'='(.*?)'.*?'ID'=(\d+).*?'SQL'='(.*?)'");

            for (int i = 0; i < CatNameIDs.Count; i++)
            {
                try
                {
                    Attribute attribute = new Attribute();
                    string rowID = CatNameIDs[i].Groups[2].Value;
                    string displayName = CatNameIDs[i].Groups[1].Value;

                    attribute.CatRowId = NodeId + "_" + rowID;

                    //TODO : Add logic to get SQL

                    if (rowID != "1")
                    {
                        categoryDef.Attributes.Add(displayName, attribute);
                    }
                    else
                    {
                        categoryDef.Name = displayName;
                        categoryDef.Id = NodeId;
                    }

                }
                catch (Exception ex)
                {
                    //There are some repeated attributes in the categorie serialization which causes key errors
                    //So far they are duplicate so we can just handle and move on, but not crazy about the technique
                }

            }
            return categoryDef;
        }
    }
}
