using System.Globalization;
using Dashboard.Api.Models;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace Dashboard.Api.Services
{
    public interface ITempestiveService
    {
        IActionResult GenerateAlfaReport(Stream fileStream, int month, int year, User user);
    }

    public class TempestiveService : ITempestiveService
    {
        private const string TemplateFileName = "rapportino_alfa.xlsx";
        private const int StartRow = 8;
        private const int DateColumn = 1;
        private const int NoteColumn = 2;
        private const int HoursColumn = 3;
        private const int RemoteWorkColumn = 4;

        public IActionResult GenerateAlfaReport(Stream fileStream, int month, int year, User user)
        {
            ExcelPackage.License.SetNonCommercialOrganization("Per-ez Software");

            var filteredRows = ExtractAndFilterData(fileStream, month, year);
            var reportStream = CreateAlfaReport(filteredRows, month, year, user);
            var fileName = GenerateFileName(month, year);

            return CreateFileResult(reportStream, fileName);
        }

        private List<(DateTime Date, List<object> Values)> ExtractAndFilterData(Stream fileStream, int month, int year)
        {
            using var package = new ExcelPackage(fileStream);
            var worksheet = package.Workbook.Worksheets[0];
            
            var rows = new List<(DateTime Date, List<object> Values)>();
            var rowCount = worksheet.Dimension?.End.Row ?? 0;

            for (int row = 1; row <= rowCount; row++)
            {
                var dateCell = worksheet.Cells[row, DateColumn].Text;
                if (TryParseDate(dateCell, out DateTime date) && IsTargetMonthYear(date, month, year))
                {
                    var values = ExtractRowValues(worksheet, row);
                    rows.Add((date.Date, values));
                }
            }

            return rows.OrderBy(r => r.Date).ToList();
        }

        private bool TryParseDate(string dateString, out DateTime date)
        {
            return DateTime.TryParse(dateString, new CultureInfo("it-IT"), DateTimeStyles.None, out date);
        }

        private bool IsTargetMonthYear(DateTime date, int month, int year)
        {
            return date.Month == month && date.Year == year;
        }

        private List<object> ExtractRowValues(ExcelWorksheet worksheet, int row)
        {
            var values = new List<object>();
            var columnCount = worksheet.Dimension?.Columns ?? 0;

            for (int col = 1; col <= columnCount; col++)
            {
                if (col != DateColumn) // Skip date column
                {
                    values.Add(worksheet.Cells[row, col].Text);
                }
            }

            return values;
        }

        private Stream CreateAlfaReport(List<(DateTime Date, List<object> Values)> rows, int month, int year, User user)
        {
            ValidateRows(rows);
            
            var templatePath = GetTemplatePath();
            using var templateStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var package = new ExcelPackage(templateStream);
            
            var worksheet = package.Workbook.Worksheets[0];
            
            PopulateHeaderData(worksheet, month, year, user);
            PopulateRowsData(worksheet, rows);
            
            worksheet.Cells.AutoFitColumns();

            return SaveToMemoryStream(package);
        }

        private void ValidateRows(List<(DateTime Date, List<object> Values)> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                throw new ArgumentException("Nessun dato da elaborare per il report.");
            }
        }

        private string GetTemplatePath()
        {
            var projectRoot = Directory.GetCurrentDirectory();
            var templatePath = Path.Combine(projectRoot, "Templates", TemplateFileName);

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException("Template Excel non trovato", templatePath);
            }

            return templatePath;
        }

        private void PopulateHeaderData(ExcelWorksheet worksheet, int month, int year, User user)
        {
            // Set month and year
            worksheet.Cells["B4"].Value = new DateTime(year, month, 1);
            
            // Set employee name
            worksheet.Cells["B5"].Value = $"{user.FirstName} {user.LastName}";
            
            // Set last day of month
            var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            worksheet.Cells["B45"].Value = lastDay.ToString("dd/MM/yyyy");
        }

        private void PopulateRowsData(ExcelWorksheet worksheet, List<(DateTime Date, List<object> Values)> rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                PopulateRow(worksheet, StartRow + i, rows[i]);
            }
        }

        private void PopulateRow(ExcelWorksheet worksheet, int excelRow, (DateTime Date, List<object> Values) rowData)
        {
            worksheet.Cells[excelRow, DateColumn].Value = rowData.Date;

            for (int col = 0; col < rowData.Values.Count; col++)
            {
                ProcessColumnValue(worksheet, excelRow, col, rowData.Values[col]);
            }
        }

        private void ProcessColumnValue(ExcelWorksheet worksheet, int row, int columnIndex, object value)
        {
            switch (columnIndex)
            {
                case 6:
                    worksheet.Cells[row, NoteColumn].Value = value;
                    break;
                case 3:
                    worksheet.Cells[row, HoursColumn].Value = ParseHours(value);
                    break;
                case 5:
                    worksheet.Cells[row, RemoteWorkColumn].Value = IsRemoteWork(value) ? 1 : null;
                    break;
            }
        }

        private int ParseHours(object value)
        {
            return int.TryParse(value?.ToString(), out int hours) ? hours : 0;
        }

        private bool IsRemoteWork(object value)
        {
            return !string.Equals(value?.ToString(), "ufficio", StringComparison.OrdinalIgnoreCase);
        }

        private MemoryStream SaveToMemoryStream(ExcelPackage package)
        {
            var memoryStream = new MemoryStream();
            package.SaveAs(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        private string GenerateFileName(int month, int year)
        {
            return $"rapportino_{month}_{year}.xlsx";
        }

        private FileContentResult CreateFileResult(Stream stream, string fileName)
        {
            var fileContent = ((MemoryStream)stream).ToArray();
            
            return new FileContentResult(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = fileName
            };
        }
    }
}