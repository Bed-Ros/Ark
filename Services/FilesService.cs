using Ark.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ark.Services
{
    public enum FileGroup
    {
        Text,
        Word,
        Excel,
    }

    public static class FilesService
    {
        public static readonly Dictionary<FileGroup, string> FileGroupNames = new()
        {
            {FileGroup.Text, "Текстовые файлы" },
            {FileGroup.Word, "Файлы Word" },
            {FileGroup.Excel, "Файлы Excel" },
        };

        public static readonly Dictionary<string, FileGroup> SupportedExtensions = new()
        {
            { ".txt", FileGroup.Text },
            { ".csv", FileGroup.Text },
            { ".html", FileGroup.Text },
            { ".htm", FileGroup.Text },
            { ".css", FileGroup.Text },
            { ".js", FileGroup.Text },
            { ".json", FileGroup.Text },
            { ".xml", FileGroup.Text },
            { ".md", FileGroup.Text },
            { ".log", FileGroup.Text },
            { ".ini", FileGroup.Text },
            { ".cfg", FileGroup.Text },
            { ".docx", FileGroup.Word },
            { ".dotx", FileGroup.Word },
            { ".docm", FileGroup.Word },
            { ".dotm", FileGroup.Word },
            { ".xlsx", FileGroup.Excel },
            { ".xlsm", FileGroup.Excel },
            { ".xltx", FileGroup.Excel },
        };

        public static async Task<DbFile> ReadFile(string filepath)
        {
            string ext = Path.GetExtension(filepath);

            return new DbFile()
            {
                Name = Path.GetFileNameWithoutExtension(filepath),
                Extension = ext,
                Path = filepath,
                Text = await GetAllText(filepath, ext),
            };
        }

        private static async Task<string?> GetAllText(string filepath, string ext)
        {
            if (!SupportedExtensions.TryGetValue(ext, out FileGroup group))
                return null;

            var stream = File.OpenRead(filepath);
            switch (group)
            {
                case FileGroup.Text:
                    using (StreamReader read = new(stream))
                        return await read.ReadToEndAsync();
                case FileGroup.Word:
                    using (WordprocessingDocument wordDocument = WordprocessingDocument.Open(stream, false))
                        return wordDocument.MainDocumentPart?.Document.Body?.InnerText;
                case FileGroup.Excel:
                    StringBuilder sb = new();
                    using (SpreadsheetDocument doc = SpreadsheetDocument.Open(stream, false))
                    {
                        var workbookPart = doc.WorkbookPart;
                        var sstPart = workbookPart?.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
                        var sst = sstPart?.SharedStringTable;

                        if (workbookPart != null)
                        {
                            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
                            {
                                SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
                                foreach (Row row in sheetData.Elements<Row>())
                                {
                                    foreach (Cell cell in row.Elements<Cell>())
                                    {
                                        sb.Append(GetCellText(cell, sst));
                                    }
                                }
                            }
                        }
                    }
                    return sb.ToString();
                default:
                    return null;
            }
        }

        private static string GetCellText(Cell cell, SharedStringTable? sharedStringTable)
        {
            if (cell == null || cell.CellValue == null)
            {
                return string.Empty;
            }

            string value = cell.CellValue.Text;

            if (cell.DataType != null && cell.DataType == CellValues.SharedString)
            {
                if (int.TryParse(value, out int ssid) && sharedStringTable != null && ssid >= 0 && ssid < sharedStringTable.ChildElements.Count)
                    value = sharedStringTable.ChildElements[ssid].InnerText;
                else
                    value = string.Empty;
            }
            else if (cell.DataType != null && cell.DataType == CellValues.InlineString)
            {
                value = cell.InnerText;
            }

            return value;
        }
    }
}
