using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Exports documents from a JSON structure to CSV files grouped by matching category structures
/// </summary>
public class DocumentCsvExporter
{
    private readonly string _outputFolder;

    /// <summary>
    /// Initializes a new instance of the DocumentCsvExporter class
    /// </summary>
    /// <param name="outputFolder">Directory where CSV files will be saved</param>
    public DocumentCsvExporter(string outputFolder = "CategoryGroups")
    {
        _outputFolder = outputFolder;
        Directory.CreateDirectory(_outputFolder);
    }

    /// <summary>
    /// Exports documents from a JSON object to CSV files grouped by matching category structures
    /// </summary>
    /// <param name="rootObject">The JObject containing the document hierarchy</param>
    /// <returns>A list of paths to the generated CSV files</returns>
    public List<string> ExportDocuments(JObject rootObject)
    {
        // Extract all document nodes
        List<JToken> allDocuments = new List<JToken>();
        ExtractDocuments(rootObject, allDocuments);

        Console.WriteLine($"Found {allDocuments.Count} documents in total");

        // Group documents by their category structure
        var documentGroups = GroupDocumentsByCategories(allDocuments);
        Console.WriteLine($"Documents grouped into {documentGroups.Count} different category structures");

        // Export each group to a separate CSV file
        List<string> exportedFilePaths = new List<string>();

        int groupIndex = 1;
        foreach (var group in documentGroups)
        {
            string filePath = Path.Combine(_outputFolder, $"CategoryGroup_{groupIndex}.csv");
            ExportGroupToCsv(group.Key, group.Value, filePath);
            exportedFilePaths.Add(filePath);

            Console.WriteLine($"Group {groupIndex} with {group.Value.Count} documents exported to {filePath}");
            groupIndex++;
        }

        return exportedFilePaths;
    }

    /// <summary>
    /// Recursively extracts all document nodes from a JSON structure
    /// </summary>
    private void ExtractDocuments(JToken token, List<JToken> documents)
    {
        if (token is JObject obj)
        {
            // Check if this is a document node with categories
            if (obj["data"] != null && obj["data"]["categories"] != null &&
                obj["data"]["properties"] != null)
            {
                documents.Add(obj);
            }

            // Process child nodes
            foreach (var property in obj.Properties())
            {
                ExtractDocuments(property.Value, documents);
            }
        }
        else if (token is JArray array)
        {
            foreach (var item in array)
            {
                ExtractDocuments(item, documents);
            }
        }
    }

    /// <summary>
    /// Groups documents based on their category structure
    /// </summary>
    private Dictionary<string, List<JToken>> GroupDocumentsByCategories(List<JToken> documents)
    {
        var groups = new Dictionary<string, List<JToken>>();

        foreach (var doc in documents)
        {
            string categoriesSignature = GetCategorySignature(doc["data"]["categories"]);

            if (!groups.ContainsKey(categoriesSignature))
            {
                groups[categoriesSignature] = new List<JToken>();
            }

            groups[categoriesSignature].Add(doc);
        }

        return groups;
    }

    /// <summary>
    /// Generates a unique signature for a category structure
    /// </summary>
    private string GetCategorySignature(JToken categoriesToken)
    {
        if (categoriesToken == null || !categoriesToken.HasValues)
            return "no_categories";

        var allCategoryKeys = new HashSet<string>();

        // Extract all unique category keys
        foreach (var category in categoriesToken)
        {
            if (category is JObject categoryObj)
            {
                foreach (var property in categoryObj.Properties())
                {
                    allCategoryKeys.Add(property.Name);
                }
            }
        }

        // Sort keys to ensure consistent signatures
        var sortedKeys = allCategoryKeys.OrderBy(k => k).ToList();
        return string.Join("|", sortedKeys);
    }

    /// <summary>
    /// Exports a group of documents to a CSV file
    /// </summary>
    private void ExportGroupToCsv(string categorySignature, List<JToken> documents, string filePath)
    {
        var csvContent = new StringBuilder();

        // Get all unique category field names for this group
        HashSet<string> allFields = new HashSet<string>();
        allFields.Add("id");
        allFields.Add("name");

        // Add all category fields
        string[] categoryKeys = categorySignature.Split('|');
        foreach (var key in categoryKeys)
        {
            if (key != "no_categories")
            {
                allFields.Add(key);
            }
        }

        // Create header row
        csvContent.AppendLine(string.Join(",", allFields.Select(EscapeCsvField)));

        // Add document rows
        foreach (var doc in documents)
        {
            var rowValues = new Dictionary<string, string>();

            // Add ID and name
            rowValues["id"] = doc["data"]["properties"]["id"]?.ToString() ?? "";
            rowValues["name"] = doc["data"]["properties"]["name"]?.ToString() ?? "";

            // Add category values
            var categories = doc["data"]["categories"];
            if (categories != null && categories.HasValues)
            {
                foreach (var category in categories)
                {
                    if (category is JObject categoryObj)
                    {
                        foreach (var property in categoryObj.Properties())
                        {
                            if (allFields.Contains(property.Name))
                            {
                                rowValues[property.Name] = property.Value?.ToString() ?? "";
                            }
                        }
                    }
                }
            }

            // Build the CSV row
            var rowFields = allFields.Select(field => rowValues.ContainsKey(field) ? EscapeCsvField(rowValues[field]) : "");
            csvContent.AppendLine(string.Join(",", rowFields));
        }

        // Write to file
        File.WriteAllText(filePath, csvContent.ToString());
    }

    /// <summary>
    /// Properly escapes a string for use in a CSV field
    /// </summary>
    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        // Check if the field contains commas, quotes, or newlines
        bool needsQuotes = field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r");

        if (needsQuotes)
        {
            // Replace any double quotes with two double quotes
            field = field.Replace("\"", "\"\"");
            // Wrap the field in quotes
            return $"\"{field}\"";
        }

        return field;
    }
}