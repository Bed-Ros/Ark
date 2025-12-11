using Ark.Models;
using DocumentFormat.OpenXml.Packaging;
using System.IO;

namespace Ark.Services
{
    public static class FilesService
    {
        public static DbFile ReadFile(string filepath)
        {
            string ext = Path.GetExtension(filepath);
            byte[] bytes = File.ReadAllBytes(filepath);
            return new DbFile()
            {
                Bytes = bytes,
                Name = Path.GetFileNameWithoutExtension(filepath),
                Extension = ext,
                Text = GetAllText(bytes, ext),
            };
        }

        private static string? GetAllText(byte[] bytes, string ext)
        {
            switch (ext)
            {
                //Word
                case ".docx":
                case ".dotx":
                case ".docm":
                case ".dotm":
                    var stream = new MemoryStream(bytes);
                    using (WordprocessingDocument wordDocument = WordprocessingDocument.Open(stream, false))
                        return wordDocument.MainDocumentPart?.Document.Body?.InnerText;
                //Для всех остальных null
                default:
                    return null;
            }
        }
    }
}
