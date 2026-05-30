using ClosedXML.Excel;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Project.Services
{
    public static class ExportService
    {
        // ========================
        // EXPORT TO EXCEL
        // ========================
        public static void ExportToExcel(DataGrid dataGrid, string fileName = "Export.xlsx")
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = fileName
            };

            if (saveFileDialog.ShowDialog() != true) return;

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Data");

            var visibleColumns = dataGrid.Columns.Where(c => c.Visibility == Visibility.Visible).ToList();

            // Headers
            for (int i = 0; i < visibleColumns.Count; i++)
            {
                worksheet.Cell(1, i + 1).Value = visibleColumns[i].Header?.ToString();
            }

            for (int i = 0; i < visibleColumns.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // Rows

            if (dataGrid.ItemsSource == null)
            {
                MessageBox.Show("No data to export.");
                return;
            }

            var view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);

            int row = 2;

            foreach (var item in view)
            {
                if (item == CollectionView.NewItemPlaceholder)
                    continue;

                for (int col = 0; col < visibleColumns.Count; col++)
                {
                    var content = visibleColumns[col].GetCellContent(item);

                    string value = "";

                    if (content is TextBlock tb)
                        value = tb.Text;
                    else if (content is CheckBox cb)
                        value = cb.IsChecked == true ? "Yes" : "No";

                    worksheet.Cell(row, col + 1).Value = value;
                }

                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(saveFileDialog.FileName);
        }

        // ========================
        // EXPORT TO PDF
        // ========================
        public static void ExportToPDF(DataGrid dataGrid, string title = "Report", string fileName = "Export.pdf")
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = fileName
            };

            if (saveFileDialog.ShowDialog() != true) return;

            QuestPDF.Settings.License = LicenseType.Community;

            if (dataGrid.ItemsSource == null)
            {
                MessageBox.Show("No data to export.");
                return;
            }

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);

                    page.Content().Column(col =>
                    {
                        col.Item().Text(title).Bold().FontSize(18);

                        col.Item().Text($"Generated on: {DateTime.Now:dd-MM-yyyy HH:mm}");

                        col.Item().Table(table =>
                        {
                            var visibleColumns = dataGrid.Columns.Where(c => c.Visibility == Visibility.Visible).ToList();
                            int columnCount = visibleColumns.Count;

                            // Define columns
                            table.ColumnsDefinition(columns =>
                            {
                                for (int i = 0; i < columnCount; i++)
                                    columns.RelativeColumn();
                            });

                            // Headers
                            table.Header(header =>
                            {
                                foreach (var column in visibleColumns)
                                {
                                    header.Cell()
                                            .Border(1)
                                            .Padding(5)
                                            .Text(column.Header?.ToString())
                                            .Bold();
                                }
                            });

                            // Rows

                            var view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);

                            foreach (var item in view)
                            {
                                if (item == CollectionView.NewItemPlaceholder)
                                    continue;

                                foreach (var column in visibleColumns)
                                {
                                    var content = column.GetCellContent(item);

                                    string value = "";

                                    if (content is TextBlock tb)
                                        value = tb.Text;
                                    else if (content is CheckBox cb)
                                        value = cb.IsChecked == true ? "Yes" : "No";

                                    table.Cell()
                                        .Border(1)
                                        .Padding(5)
                                        .Text(value);
                                }
                            }
                        });
                    });
                });
            }).GeneratePdf(saveFileDialog.FileName);
        }
    }
}