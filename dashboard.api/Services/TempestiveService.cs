using System.Globalization;
using OfficeOpenXml;

namespace Dashboard.Api.Services
{
    public class TempestiveService
    {
        public Stream ProcessExcel(Stream fileStream, int month, int year)
        {
            // Set license context for EPPlus
            ExcelPackage.License.SetNonCommercialOrganization("Per-ez Software");

            using var package = new ExcelPackage(fileStream);
            var ws = package.Workbook.Worksheets[0];

            var rows = new List<(DateTime Data, List<object> Values)>();

            int rowCount = ws.Dimension.Rows;

            for (int row = 1; row <= rowCount; row++)
            {
                var dateCell = ws.Cells[row, 1].Text;
                if (DateTime.TryParse(dateCell, new CultureInfo("it-IT"), DateTimeStyles.None, out DateTime data))
                {
                    var values = new List<object>();
                    for (int col = 1; col <= ws.Dimension.Columns; col++)
                    {
                        if (col == 1) continue; // Skip columb A (data)

                        values.Add(ws.Cells[row, col].Text);
                    }

                    // Format the date in only day/month/year
                    data = new DateTime(data.Year, data.Month, data.Day);

                    // Check if the date matches the specified month and year
                    if (data.Month == month && data.Year == year)
                        rows.Add((data, values));
                }
            }

            rows = rows.OrderBy(r => r.Data).ToList();

            package.Dispose();

            // Create the new Excel using the template
            return CreateAlfaReport(rows, month, year);
        }

        private Stream CreateAlfaReport(List<(DateTime Data, List<object> Values)> rows, int month, int year)
        {

            if (rows == null || rows.Count == 0)
                throw new ArgumentException("Nessun dato da elaborare per il report.");

            // Path to the template Excel file
            var projectRoot = Directory.GetCurrentDirectory();
            var templatePath = Path.Combine(projectRoot, "Templates", "rapportino_alfa.xlsx");

            if (!File.Exists(templatePath))
                throw new FileNotFoundException("Template Excel non trovato", templatePath);

            // Open the template Excel file
            using var templateStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var package = new ExcelPackage(templateStream);

            var ws = package.Workbook.Worksheets[0];

            // Set the month and year in cell B4
            ws.Cells["B4"].Value = new DateTime(year, month, 1).ToString("MM/yyyy");

            // Set the employee name in cell B5
            ws.Cells["B5"].Value = "Nome Dipendente";

            // Set the last day of the month in cell B45
            var lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            ws.Cells["B45"].Value = lastDay.ToString("dd/MM/yyyy");

            for (int i = 0; i < rows.Count; i++)
            {
                createRow(ws, i + 8, rows[i]); // Start inserting from row 8
            }

            // Auto-fit columns for better presentation
            ws.Cells.AutoFitColumns();

            // Save the modified Excel to a MemoryStream
            var outputStream = new MemoryStream();
            package.SaveAs(outputStream);
            outputStream.Position = 0;

            return outputStream;
        }
        
        private void createRow(ExcelWorksheet ws, int excelRow, (DateTime Data, List<object> Values) rowData)
        {
            ws.Cells[excelRow, 1].Value = rowData.Data; // Column A: Date

            for (int col = 0; col < rowData.Values.Count; col++)
            {
                switch (col)
                {
                    case 6:
                        ws.Cells[excelRow, 2].Value = rowData.Values[col]; // Column B: Value
                        break;
                    case 3:
                        ws.Cells[excelRow, 3].Value = rowData.Values[col]; // Column C: Value
                        break;

                    case 5 :
                        if (!string.Equals(rowData.Values[col]?.ToString(), "ufficio", StringComparison.OrdinalIgnoreCase)) ws.Cells[excelRow, 4].Value = 1;
                        break;
                    default:
                        
                        break;
                }
            }
        }
    }
}
