using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OTIngestion.Utils
{
    public class SheetUtils
    {
        #region Style

        public static int GetHeaderStyleIndex(SpreadsheetDocument doc)
        {

            Font headerFont = new Font(
                new Bold(),
                new FontSize() { Val = 12 },
                new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                new FontName() { Val = "Calibri" }
                );

            return GetStyleIndexFromFont(doc, headerFont);
        }

        public static int GetNormalStyleIndex(SpreadsheetDocument doc)
        {
            Font normalFont = new Font(
                new FontSize() { Val = 11 },
                new Color() { Rgb = new HexBinaryValue() { Value = "000000" } },
                new FontName() { Val = "Calibri" }
                );

            return GetStyleIndexFromFont(doc, normalFont);
        }

        public static int GetErrorStyleIndex(SpreadsheetDocument doc)
        {
            Font normalFont = new Font(
                new FontSize() { Val = 11 },
                new Color() { Rgb = new HexBinaryValue() { Value = "ff0000" } },
                new FontName() { Val = "Calibri" }
                );

            return GetStyleIndexFromFont(doc, normalFont);
        }

        public static int GetSuccessStyleIndex(SpreadsheetDocument doc)
        {
            Font normalFont = new Font(
                new FontSize() { Val = 11 },
                new Color() { Rgb = new HexBinaryValue() { Value = "00B140" } },
                new FontName() { Val = "Calibri" }
                );

            return GetStyleIndexFromFont(doc, normalFont);
        }

        public static int GetStyleIndexFromFont(SpreadsheetDocument doc, Font font)
        {
            if (doc.WorkbookPart.WorkbookStylesPart == null) { 
                doc.WorkbookPart.AddNewPart<WorkbookStylesPart>();
                doc.WorkbookPart.WorkbookStylesPart.Stylesheet = GenerateStylesheet();
            }

            WorkbookStylesPart stylesPart = doc.WorkbookPart.WorkbookStylesPart;

            stylesPart.Stylesheet.Fonts.Append(font);
            
            UInt32Value fontId = Convert.ToUInt32(stylesPart.Stylesheet.Fonts.ChildElements.Count - 1);
            CellFormat cf = new CellFormat() { FontId = fontId, FillId = 0, BorderId = 0, ApplyFont = true };
            stylesPart.Stylesheet.CellFormats.Append(cf);
            stylesPart.Stylesheet.Save();

            int fontIdx = stylesPart.Stylesheet.CellFormats.ChildElements.Count - 1;
            return fontIdx;
        }

        private static Stylesheet GenerateStylesheet()
        {
            var fonts = new Fonts(
                new Font(new FontSize { Val = 11 }, new FontName { Val = "Calibri" }) //FontId=0 or default
                );

            var fills = new Fills(
                new Fill(new PatternFill { PatternType = PatternValues.None }) //FillId=0 or default
                );


            var borders = new Borders(
                new Border() // BorderId=0 or Default
                );

            var cellFormats = new CellFormats(
                
                new CellFormat { FontId = 0, ApplyFont = true }
                );

            return new Stylesheet(fonts, fills, borders, cellFormats);
        }

        #endregion

        public static string GetExcelCellReference(int columnNumber, int rowNumber)
        {
            return $"{GetExcelColumnName(columnNumber)}{rowNumber}";
        }

        //To get the excel column name using column number
        public static string GetExcelColumnName(int columnNumber)
        {
            int dividend = (int)columnNumber;
            string columnName = String.Empty;
            int modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }

            return columnName;
        }

        public static Cell GetReferenceCell(Row row, int columnNumber, int rowNumber)
        {
            String refLoc = SheetUtils.GetExcelCellReference(columnNumber, rowNumber);
            return SheetUtils.GetReferenceCell(row, refLoc);
        }

        public static Cell GetReferenceCell(Row row, String RefLocation)
        {
            Cell refCell = null;

            foreach (Cell cell in row.Elements<Cell>())
            {
                if (cell.CellReference.Value == RefLocation)
                {
                    refCell = cell;
                    break;
                }
            }

            return refCell;
        }

        public static SheetData AddValidationSheet(SpreadsheetDocument doc)
        {
            WorksheetPart newWorksheetPart = doc.WorkbookPart.AddNewPart<WorksheetPart>();
            newWorksheetPart.Worksheet = new Worksheet(new SheetData());

            Sheets sheets = doc.WorkbookPart.Workbook.GetFirstChild<Sheets>();
            string relationshipId = doc.WorkbookPart.GetIdOfPart(newWorksheetPart);

            // Get a unique ID for the new worksheet.
            uint sheetId = 1;
            if (sheets.Elements<Sheet>().Count() > 0)
            {
                sheetId =
                    sheets.Elements<Sheet>().Select(s => s.SheetId.Value).Max() + 1;
            }

            // Give the new worksheet a name.
            string sheetName = "Sheet" + sheetId;

            // Append the new worksheet and associate it with the workbook.
            Sheet sheet = new Sheet()
            { Id = relationshipId, SheetId = sheetId, Name = "Validation Values" };
            sheets.Append(sheet);


            // Get the sheetData cell table.
            SheetData sheetData = newWorksheetPart.Worksheet.GetFirstChild<SheetData>();

            return sheetData;
        }

        public static void AddValidationValues(SpreadsheetDocument spreadsheetDocument, SheetData validationSheet, List<string> validationAttributes, List<string> validationSQL, String webReportId)
        {
            Dictionary<string, List<String>> rows = new Dictionary<string, List<String>>();

            //Initialize Dicitionary Lists
            for(int i = 0; i < validationAttributes.Count; i++)
            {
                String attrName = validationAttributes[i];
                List<String> values = GetValuesFromSQL(validationSQL[i], webReportId);
                rows.Add(attrName, values);
            }
            //NlogUtils.Logger.Debug("Keys from AddValidationValues - {0}", string.Join(", ", rows.Keys));
            //NlogUtils.Logger.Debug("Values from AddValidationValues - {0}", string.Join(", ", rows.Values));


            int maxRows = 0;
            for(int i = 0; i < rows.Count; i++)
            {
                int rowLen = rows[rows.Keys.ToList()[i]].Count;
                if(rowLen > maxRows)
                    maxRows = rowLen;
            }

            
            int normalIdx = SheetUtils.GetNormalStyleIndex(spreadsheetDocument);
            int headerIdx = SheetUtils.GetHeaderStyleIndex(spreadsheetDocument);

            //Add ValidationData
            Row headerRow;
            headerRow = new Row() { RowIndex = 1 };
            validationSheet.Append(headerRow);

            Cell headerRefCell = SheetUtils.GetReferenceCell(headerRow, "A1");

            List<String> headers = rows.Keys.ToList();
            for (int i = 0; i < headers.Count; i++)
            {
                //Add NodeId
                Cell headerCell = new Cell();
                headerRow.InsertAfter(headerCell, headerRefCell);

                // Set the cell value to be a numeric value of 100.
                headerCell.CellValue = new CellValue(headers[i]);
                headerCell.DataType = new EnumValue<CellValues>(CellValues.String);
                headerCell.StyleIndex = Convert.ToUInt32(headerIdx);
                headerRefCell = headerCell;
            }

            for(int rowNum=0; rowNum < maxRows; rowNum++)
            {
                // Add a row to the cell table.
                Row row;
                row = new Row(); 
                validationSheet.Append(row);

                Cell rowRefCell = SheetUtils.GetReferenceCell(row, "A1");

                for(int i = 0; i < rows.Count; i++)
                {
                    List<String> validationValues = rows[headers[i]];
                    Cell newCell = new Cell();
                    row.InsertAfter(newCell, rowRefCell);
                    
                    if (validationValues.Count > rowNum)
                    {
                        newCell.CellValue = new CellValue(validationValues[rowNum]);
                        newCell.StyleIndex = Convert.ToUInt32(normalIdx);
                        newCell.DataType = new EnumValue<CellValues>(CellValues.String);
                    }
                    else
                    {
                        //newCell.CellValue = new CellValue("");
                        newCell.StyleIndex = Convert.ToUInt32(normalIdx);
                        newCell.DataType = new EnumValue<CellValues>(CellValues.String);
                    }

                    rowRefCell = newCell;
                }
            }
        }

        public static String GetValidationXML(String formula, String sqRef)
        {
            String validationTemplate = "<x14:dataValidation xmlns:xr=\"http://schemas.microsoft.com/office/spreadsheetml/2014/revision\" type=\"list\" allowBlank=\"1\" showInputMessage=\"1\" showErrorMessage=\"1\" xr:uid=\"{" + Guid.NewGuid().ToString().ToUpper() + "}\"><x14:formula1><xm:f>" + formula + "</xm:f></x14:formula1><xm:sqref>" + sqRef + "</xm:sqref></x14:dataValidation>";
            return validationTemplate;
        }

        public static String GetExtListInnerXML(List<string> AttributeValidationFormulas, List<string> SeqRefs)
        {
            int valNum = AttributeValidationFormulas.Count;

            String validationXML = "";
            for(int i = 0; i < AttributeValidationFormulas.Count; i++)
            {
                //if (validationXML != "")
                //    validationXML += ",";

                validationXML += GetValidationXML(AttributeValidationFormulas[i], SeqRefs[i]);
            }

            String ExtTemplate = "<x:ext xmlns:x14=\"http://schemas.microsoft.com/office/spreadsheetml/2009/9/main\" uri=\"{CCE6A557-97BC-4b89-ADB6-D9C93CAAB3DF}\"><x14:dataValidations xmlns:xm=\"http://schemas.microsoft.com/office/excel/2006/main\" count=\"" + valNum + "\">" + validationXML + "</x14:dataValidations></x:ext>";
            return ExtTemplate;
        }

        public static Row EnsureRow(SheetData sheetData, int rowIndex)
        {
            Row row = null;
            try
            {
                row = sheetData.Elements<Row>().ElementAt(rowIndex);
            }
            catch (Exception ex) { }

            if(row == null)
            {
                row = new Row() { RowIndex = UInt32.Parse(rowIndex.ToString()) };
                sheetData.Append(row);
            }
            return row;
        }

        public static Worksheet GetWorksheetBySheetName(SpreadsheetDocument document, string sheetName)
        {
            var workbookPart = document.WorkbookPart;
            string relationshipId = workbookPart.Workbook.Descendants<Sheet>().FirstOrDefault(s => s.Name.Equals(sheetName))?.Id;

            var worksheet = ((WorksheetPart)workbookPart.GetPartById(relationshipId)).Worksheet;

            return worksheet;
        }

        private static List<string> GetValuesFromSQL(string sqlQuery, string webReportId)
        {
            OTCSUtils otcsUtils = new OTCSUtils();
            List<string> values = new List<string>();

            string apiUrl = OTConfig.GetOTConfig().rest_url + "/api/v1/nodes/" + webReportId + "/output?sqlQuery=" + sqlQuery;

            var request = otcsUtils.GenerateRestRequest(Method.Get);
            var client = otcsUtils.GenerateRestClient(apiUrl);

            RestResponse response = client.Execute(request);
            NlogUtils.Logger.Debug("Response from SQL Query API- {0} ", response.Content);
            var nodeStr = JObject.Parse(response.Content);
            var data = JObject.Parse(nodeStr["data"].ToString());

            foreach (var item in data["myRows"])
            {
                JProperty prop = item.First.Value<JProperty>();
                String value = prop.Value.ToString();
                var val = value == null ? string.Empty : value.ToString().Trim();
                if (!values.Contains(val))
                {
                    values.Add(val);
                }
            }
            //NlogUtils.Logger.Debug("Values from SQL Query - {0} ", values);


            return values;
        }

        public static SheetData AddProcessSheet(SpreadsheetDocument doc)
        {
            WorksheetPart newWorksheetPart = doc.WorkbookPart.AddNewPart<WorksheetPart>();
            newWorksheetPart.Worksheet = new Worksheet(new SheetData());

            Sheets sheets = doc.WorkbookPart.Workbook.GetFirstChild<Sheets>();
            string relationshipId = doc.WorkbookPart.GetIdOfPart(newWorksheetPart);

            // Get a unique ID for the new worksheet.
            uint sheetId = 1;
            if (sheets.Elements<Sheet>().Count() > 0)
            {
                sheetId =
                    sheets.Elements<Sheet>().Select(s => s.SheetId.Value).Max() + 1;
            }

            // Give the new worksheet a name.
            string sheetName = "Sheet" + sheetId;

            // Append the new worksheet and associate it with the workbook.
            Sheet sheet = new Sheet()
            { Id = relationshipId, SheetId = sheetId, Name = "Process Sheet " + sheetId };
            sheets.Append(sheet);


            // Get the sheetData cell table.
            SheetData sheetData = newWorksheetPart.Worksheet.GetFirstChild<SheetData>();

            return sheetData;
        }

        public static string GetCellValue(SpreadsheetDocument doc, Cell cell)
        {
            string value = "";
            if (cell.CellValue != null)
            {
                try
                {
                    value = cell.CellValue.InnerText;
                    if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
                    {
                        return doc.WorkbookPart.SharedStringTablePart.SharedStringTable.ChildElements[int.Parse(value)].InnerText;
                    }
                }
                catch (Exception ex)
                {

                }
            }
            return value;
        }
    }
}