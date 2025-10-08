using AskMeNow.Core.Entities;
using AskMeNow.Core.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using HtmlAgilityPack;
using ClosedXML.Excel;
using System.Text;
using System.Text.RegularExpressions;

namespace AskMeNow.Infrastructure.Services
{
    public class DocumentParserService : IDocumentParserService
    {
        private readonly List<string> _supportedExtensions = new()
    {
        ".txt", ".md", ".json", ".html", ".htm",
        ".docx", ".doc", ".pdf", ".xlsx", ".xls"
    };

        public List<string> GetSupportedExtensions()
        {
            return _supportedExtensions.ToList();
        }

        public async Task<List<FAQDocument>> ParseDocumentsFromFolderAsync(string folderPath)
        {
            var documents = new List<FAQDocument>();

            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
            }

            var allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
            var supportedFiles = allFiles
                .Where(file => _supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                .ToList();

            foreach (var filePath in supportedFiles)
            {
                try
                {
                    var document = await ParseDocumentAsync(filePath);
                    if (document != null && !string.IsNullOrWhiteSpace(document.Content))
                    {
                        documents.Add(document);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing file {filePath}: {ex.Message}");
                }
            }

            return documents;
        }

        public async Task<FileProcessingResult> GetFileProcessingStatsAsync(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
            }

            var result = new FileProcessingResult();

            var allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

            var supportedFiles = new List<string>();
            var unsupportedFiles = new List<string>();

            foreach (var file in allFiles)
            {
                var extension = Path.GetExtension(file).ToLowerInvariant();
                if (_supportedExtensions.Contains(extension))
                {
                    supportedFiles.Add(file);
                    if (!result.SupportedExtensions.Contains(extension))
                    {
                        result.SupportedExtensions.Add(extension);
                    }
                }
                else
                {
                    unsupportedFiles.Add(file);
                    if (!result.UnsupportedExtensions.Contains(extension))
                    {
                        result.UnsupportedExtensions.Add(extension);
                    }
                }
            }

            result.SupportedFilesFound = supportedFiles.Count;
            result.UnsupportedFilesFound = unsupportedFiles.Count;

            foreach (var filePath in supportedFiles)
            {
                try
                {
                    var document = await ParseDocumentAsync(filePath);
                    if (document != null && !string.IsNullOrWhiteSpace(document.Content))
                    {
                        result.SuccessfullyProcessed++;
                    }
                    else
                    {
                        result.FailedToProcess++;
                    }
                }
                catch
                {
                    result.FailedToProcess++;
                }
            }

            return result;
        }

        public async Task<FAQDocument> ParseDocumentAsync(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            string content;

            try
            {
                content = extension switch
                {
                    ".txt" or ".md" => await ParseTextFileAsync(filePath),
                    ".json" => await ParseJsonFileAsync(filePath),
                    ".html" or ".htm" => await ParseHtmlFileAsync(filePath),
                    ".docx" => await ParseDocxFileAsync(filePath),
                    ".doc" => await ParseDocFileAsync(filePath),
                    ".pdf" => await ParsePdfFileAsync(filePath),
                    ".xlsx" or ".xls" => await ParseExcelFileAsync(filePath),
                    _ => throw new NotSupportedException($"File type {extension} is not supported")
                };

                return new FAQDocument
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = fileName,
                    Content = content,
                    FilePath = filePath,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse document {filePath}: {ex.Message}", ex);
            }
        }

        private async Task<string> ParseTextFileAsync(string filePath)
        {
            return await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        }

        private async Task<string> ParseJsonFileAsync(string filePath)
        {
            var jsonContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

            try
            {
                var jsonDocument = System.Text.Json.JsonDocument.Parse(jsonContent);
                var flattenedContent = new StringBuilder();

                FlattenJsonObject(jsonDocument.RootElement, flattenedContent, "");

                return flattenedContent.ToString();
            }
            catch
            {
                return jsonContent;
            }
        }

        private void FlattenJsonObject(System.Text.Json.JsonElement element, StringBuilder sb, string prefix)
        {
            switch (element.ValueKind)
            {
                case System.Text.Json.JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var newPrefix = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                        FlattenJsonObject(property.Value, sb, newPrefix);
                    }
                    break;
                case System.Text.Json.JsonValueKind.Array:
                    var index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        var newPrefix = $"{prefix}[{index}]";
                        FlattenJsonObject(item, sb, newPrefix);
                        index++;
                    }
                    break;
                default:
                    sb.AppendLine($"{prefix}: {element.ToString()}");
                    break;
            }
        }

        private async Task<string> ParseHtmlFileAsync(string filePath)
        {
            var htmlContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            doc.DocumentNode.Descendants()
                .Where(n => n.Name == "script" || n.Name == "style")
                .ToList()
                .ForEach(n => n.Remove());

            var text = doc.DocumentNode.InnerText;

            text = Regex.Replace(text, @"\s+", " ");
            text = Regex.Replace(text, @"\n\s*\n", "\n");

            return text.Trim();
        }

        private async Task<string> ParseDocxFileAsync(string filePath)
        {
            using var document = WordprocessingDocument.Open(filePath, false);
            var body = document.MainDocumentPart?.Document?.Body;

            if (body == null)
                return string.Empty;

            var text = new StringBuilder();

            foreach (var paragraph in body.Elements<Paragraph>())
            {
                var paragraphText = paragraph.InnerText;
                if (!string.IsNullOrWhiteSpace(paragraphText))
                {
                    text.AppendLine(paragraphText);
                }
            }

            return text.ToString();
        }

        private async Task<string> ParseDocFileAsync(string filePath)
        {
            try
            {
                return await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            }
            catch
            {
                return $"[Microsoft Word Document - .doc format not fully supported. Please convert to .docx for better text extraction.]";
            }
        }

        private async Task<string> ParsePdfFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                var text = new StringBuilder();

                using var pdfReader = new PdfReader(filePath);
                using var pdfDocument = new PdfDocument(pdfReader);

                for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
                {
                    var page = pdfDocument.GetPage(pageNum);
                    var pageText = PdfTextExtractor.GetTextFromPage(page);

                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        text.AppendLine(pageText);
                    }
                }

                return text.ToString();
            });
        }

        private async Task<string> ParseExcelFileAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                var text = new StringBuilder();

                using var workbook = new XLWorkbook(filePath);

                foreach (var worksheet in workbook.Worksheets)
                {
                    text.AppendLine($"Worksheet: {worksheet.Name}");

                    var usedRange = worksheet.RangeUsed();
                    if (usedRange != null)
                    {
                        foreach (var row in usedRange.Rows())
                        {
                            var rowText = new StringBuilder();
                            foreach (var cell in row.Cells())
                            {
                                var cellValue = cell.GetString();
                                if (!string.IsNullOrWhiteSpace(cellValue))
                                {
                                    rowText.Append($"{cellValue} ");
                                }
                            }

                            if (rowText.Length > 0)
                            {
                                text.AppendLine(rowText.ToString().Trim());
                            }
                        }
                    }
                }

                return text.ToString();
            });
        }
    }
}