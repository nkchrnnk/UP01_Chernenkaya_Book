using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;
using UP01_Chernenkaya_Book.Helpers;
using UP01_Chernenkaya_Book.Models;

namespace UP01_Chernenkaya_Book.Pages;

public partial class AnalyticsPage : System.Windows.Controls.Page
{
    static AnalyticsPage()
    {
        try
        {
            if (GlobalFontSettings.FontResolver == null)
            {
                GlobalFontSettings.FontResolver = new FileFontResolver();
            }
        }
        catch
        {
            // ignored
        }
    }

    public AnalyticsPage()
    {
        InitializeComponent();
        UpdateChart(null, null);
    }

    // Шаг 12: Построение графиков и диаграмм через LiveCharts
    private void UpdateChart(object? sender, SelectionChangedEventArgs? e)
    {
        if (CmbMetric == null || CmbChartType == null || CartesianChartControl == null || PieChartControl == null)
            return;

        try
        {
            using var db = new BookingDbContext();

            string metric = (CmbMetric.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
            string chartType = (CmbChartType.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";

            List<string> labels = new();
            List<double> values = new();
            string seriesTitle = "";
            string yTitle = "";

            if (metric.Contains("типам жилья"))
            {
                var types = db.AccommodationTypes.ToList();
                foreach (var t in types)
                {
                    labels.Add(t.TypeName);
                    int count = db.Bookings.Count(b => b.Room != null && b.Room.Accommodation != null && b.Room.Accommodation.TypeId == t.TypeId);
                    values.Add(count);
                }
                seriesTitle = "Число бронирований";
                yTitle = "Количество броней (шт.)";
                AxisX.Title = "Типы объектов размещения";
            }
            else if (metric.Contains("Выручка"))
            {
                var cities = db.Cities.ToList();
                foreach (var c in cities)
                {
                    labels.Add(c.CityName);
                    decimal rev = db.Payments
                        .Where(p => p.PaymentStatus == "Оплачено" && p.Booking != null && p.Booking.Room != null && p.Booking.Room.Accommodation != null && p.Booking.Room.Accommodation.CityId == c.CityId)
                        .Sum(p => (decimal?)p.Amount) ?? 0m;
                    values.Add((double)rev);
                }
                seriesTitle = "Выручка (₽)";
                yTitle = "Сумма оплат (руб.)";
                AxisX.Title = "Города расположения";
            }
            else // Бронирования по городам
            {
                var cities = db.Cities.ToList();
                foreach (var c in cities)
                {
                    labels.Add(c.CityName);
                    int count = db.Bookings.Count(b => b.Room != null && b.Room.Accommodation != null && b.Room.Accommodation.CityId == c.CityId);
                    values.Add(count);
                }
                seriesTitle = "Число бронирований";
                yTitle = "Количество броней (шт.)";
                AxisX.Title = "Города расположения";
            }

            if (chartType.Contains("Круговая"))
            {
                CartesianChartControl.Visibility = Visibility.Collapsed;
                PieChartControl.Visibility = Visibility.Visible;

                var pieSeries = new SeriesCollection();
                for (int i = 0; i < labels.Count; i++)
                {
                    if (values[i] > 0 || values.All(v => v == 0))
                    {
                        pieSeries.Add(new PieSeries
                        {
                            Title = labels[i],
                            Values = new ChartValues<double> { values[i] },
                            DataLabels = true,
                            LabelPoint = cp => $"{cp.Participation:P0} ({cp.Y})"
                        });
                    }
                }
                PieChartControl.Series = pieSeries;
            }
            else
            {
                PieChartControl.Visibility = Visibility.Collapsed;
                CartesianChartControl.Visibility = Visibility.Visible;

                AxisX.Labels = labels;
                AxisY.Title = yTitle;

                var seriesCollection = new SeriesCollection();
                var chartValues = new ChartValues<double>(values);

                if (chartType.Contains("Линейная"))
                {
                    seriesCollection.Add(new LineSeries
                    {
                        Title = seriesTitle,
                        Values = chartValues,
                        DataLabels = true
                    });
                }
                else // Столбчатая
                {
                    seriesCollection.Add(new ColumnSeries
                    {
                        Title = seriesTitle,
                        Values = chartValues,
                        DataLabels = true
                    });
                }

                CartesianChartControl.Series = seriesCollection;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка обновления графика: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Шаг 13: Выгрузка в Microsoft Word (.docx)
    private void BtnExportWord_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new BookingDbContext();
            var accommodations = db.Accommodations
                .Include(a => a.City)
                .Include(a => a.Type)
                .Include(a => a.RoomsNavigation).ThenInclude(r => r.Bookings).ThenInclude(b => b.Payments)
                .OrderBy(a => a.Name)
                .ToList();

            string exportDir = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = System.IO.Path.Combine(exportDir, "Hotel_Booking_Report.docx");

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                // Заголовок
                var titlePara = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                var titleRun = titlePara.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
                titleRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("ОТЧЕТ ПО АНАЛИТИКЕ БРОНИРОВАНИЙ И ВЫРУЧКИ ГОСТИНИЧНОГО СЕРВИСА"));
                var titleProps = titleRun.AppendChild(new RunProperties());
                titleProps.AppendChild(new Bold());
                titleProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "28" });
                titleProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Color { Val = "233E76" });

                var subPara = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                var subRun = subPara.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
                subRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm} | Ответственный администратор: Черненькая В.В."));
                var subProps = subRun.AppendChild(new RunProperties());
                subProps.AppendChild(new Italic());
                subProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "18" });
                subProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Color { Val = "5A6578" });

                body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph()); // разделитель

                // Таблица
                DocumentFormat.OpenXml.Wordprocessing.Table table = new();
                TableProperties tblProp = new(
                    new TableBorders(
                        new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 },
                        new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4 }
                    )
                );
                table.AppendChild(tblProp);

                // Шапка таблицы
                TableRow headerRow = new();
                headerRow.Append(
                    CreateWordCell("Объект размещения", true),
                    CreateWordCell("Тип", true),
                    CreateWordCell("Город", true),
                    CreateWordCell("Номеров", true),
                    CreateWordCell("Всего броней", true),
                    CreateWordCell("Выручка (₽)", true)
                );
                table.Append(headerRow);

                int totalAllBookings = 0;
                decimal totalAllRevenue = 0;

                foreach (var acc in accommodations)
                {
                    int bookingsCount = acc.RoomsNavigation.SelectMany(r => r.Bookings).Count();
                    decimal revenue = acc.RoomsNavigation
                        .SelectMany(r => r.Bookings)
                        .SelectMany(b => b.Payments)
                        .Where(p => p.PaymentStatus == "Оплачено")
                        .Sum(p => (decimal?)p.Amount) ?? 0m;

                    totalAllBookings += bookingsCount;
                    totalAllRevenue += revenue;

                    TableRow row = new();
                    row.Append(
                        CreateWordCell(acc.Name, false),
                        CreateWordCell(acc.Type?.TypeName ?? "-", false),
                        CreateWordCell(acc.City?.CityName ?? "-", false),
                        CreateWordCell(acc.Rooms.ToString(), false),
                        CreateWordCell(bookingsCount.ToString(), false),
                        CreateWordCell($"{revenue:N0} ₽", false)
                    );
                    table.Append(row);
                }

                // Итоговая строка
                TableRow totalRow = new();
                totalRow.Append(
                    CreateWordCell("ИТОГО ПО СЕРВИСУ:", true),
                    CreateWordCell("-", true),
                    CreateWordCell("-", true),
                    CreateWordCell(accommodations.Sum(a => a.Rooms).ToString(), true),
                    CreateWordCell(totalAllBookings.ToString(), true),
                    CreateWordCell($"{totalAllRevenue:N0} ₽", true)
                );
                table.Append(totalRow);

                body.Append(table);

                // Подпись
                var footPara = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                footPara.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties(
                    new SpacingBetweenLines { Before = "300" }));
                var footRun = footPara.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
                footRun.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text("Отчет сгенерирован автоматически в модуле аналитики информационной системы UP01_Chernenkaya_Book."));
                footRun.AppendChild(new RunProperties(new Italic(), new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "16" }));

                mainPart.Document.Save();
            }

            MessageBox.Show($"Документ Word успешно сформирован:\n{filePath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при экспорте в Word: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private DocumentFormat.OpenXml.Wordprocessing.TableCell CreateWordCell(string text, bool isHeader)
    {
        var run = new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
        var runProps = new RunProperties();
        if (isHeader)
        {
            runProps.AppendChild(new Bold());
            runProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Color { Val = "233E76" });
        }
        runProps.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.FontSize { Val = "19" });
        run.AppendChild(runProps);

        var para = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(run);
        var cell = new DocumentFormat.OpenXml.Wordprocessing.TableCell(para);

        var cellProps = new TableCellProperties();
        if (isHeader)
        {
            cellProps.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "E8F4F8" });
        }
        cellProps.AppendChild(new TableCellMargin(
            new TopMargin { Width = "120" },
            new BottomMargin { Width = "120" },
            new LeftMargin { Width = "150" },
            new RightMargin { Width = "150" }));

        cell.AppendChild(cellProps);
        return cell;
    }

    // Шаг 14: Выгрузка в Microsoft Excel (.xlsx) с формулами SUM
    private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new BookingDbContext();
            var accommodations = db.Accommodations
                .Include(a => a.City)
                .Include(a => a.Type)
                .Include(a => a.RoomsNavigation).ThenInclude(r => r.Bookings).ThenInclude(b => b.Payments)
                .OrderBy(a => a.Name)
                .ToList();

            var bookings = db.Bookings
                .Include(b => b.User)
                .Include(b => b.Room).ThenInclude(r => r.Accommodation)
                .Include(b => b.Status)
                .Include(b => b.Payments)
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            string exportDir = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = System.IO.Path.Combine(exportDir, "Hotel_Booking_Report.xlsx");

            using (var workbook = new XLWorkbook())
            {
                // Лист 1: Сводная аналитика объектов
                var sumSheet = workbook.Worksheets.Add("Сводная аналитика");

                sumSheet.Cell(1, 1).Value = "СВОДНЫЙ АНАЛИТИЧЕСКИЙ ОТЧЕТ СЕРВИСА БРОНИРОВАНИЯ";
                sumSheet.Cell(1, 1).Style.Font.Bold = true;
                sumSheet.Cell(1, 1).Style.Font.FontSize = 14;
                sumSheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#233E76");

                sumSheet.Cell(2, 1).Value = $"Дата выгрузки: {DateTime.Now:dd.MM.yyyy HH:mm} | Администратор: Черненькая В.В.";
                sumSheet.Cell(2, 1).Style.Font.Italic = true;

                // Заголовки таблицы
                string[] headers = { "№", "Объект размещения", "Тип жилья", "Город", "Адрес", "Номеров", "Бронирований (шт.)", "Выручка (₽)" };
                for (int c = 0; c < headers.Length; c++)
                {
                    var cell = sumSheet.Cell(4, c + 1);
                    cell.Value = headers[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0198AF");
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                int row = 5;
                int itemIndex = 1;
                foreach (var acc in accommodations)
                {
                    int bCount = acc.RoomsNavigation.SelectMany(r => r.Bookings).Count();
                    decimal bRev = acc.RoomsNavigation
                        .SelectMany(r => r.Bookings)
                        .SelectMany(b => b.Payments)
                        .Where(p => p.PaymentStatus == "Оплачено")
                        .Sum(p => (decimal?)p.Amount) ?? 0m;

                    sumSheet.Cell(row, 1).Value = itemIndex++;
                    sumSheet.Cell(row, 2).Value = acc.Name;
                    sumSheet.Cell(row, 3).Value = acc.Type?.TypeName ?? "-";
                    sumSheet.Cell(row, 4).Value = acc.City?.CityName ?? "-";
                    sumSheet.Cell(row, 5).Value = acc.Address;
                    sumSheet.Cell(row, 6).Value = acc.Rooms;
                    sumSheet.Cell(row, 7).Value = bCount;
                    sumSheet.Cell(row, 8).Value = bRev;
                    sumSheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0 ₽";

                    row++;
                }

                // Шаг 14: Формулы SUM
                sumSheet.Cell(row, 2).Value = "ИТОГО ПО СЕРВИСУ:";
                sumSheet.Cell(row, 2).Style.Font.Bold = true;
                sumSheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                sumSheet.Cell(row, 6).FormulaA1 = $"=SUM(F5:F{row - 1})";
                sumSheet.Cell(row, 6).Style.Font.Bold = true;

                sumSheet.Cell(row, 7).FormulaA1 = $"=SUM(G5:G{row - 1})";
                sumSheet.Cell(row, 7).Style.Font.Bold = true;

                sumSheet.Cell(row, 8).FormulaA1 = $"=SUM(H5:H{row - 1})";
                sumSheet.Cell(row, 8).Style.Font.Bold = true;
                sumSheet.Cell(row, 8).Style.Font.FontColor = XLColor.Red;
                sumSheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0 ₽";

                var tableRange = sumSheet.Range(4, 1, row, headers.Length);
                tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                sumSheet.Columns().AdjustToContents();

                // Лист 2: Журнал бронирований
                var bookSheet = workbook.Worksheets.Add("Журнал броней");
                string[] bHeaders = { "№ Брони", "Гость (ФИО)", "Телефон", "Отель", "Номер", "Заезд", "Выезд", "Гостей", "Статус", "Сумма (₽)" };
                for (int c = 0; c < bHeaders.Length; c++)
                {
                    var cell = bookSheet.Cell(1, c + 1);
                    cell.Value = bHeaders[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#233E76");
                    cell.Style.Font.FontColor = XLColor.White;
                }

                int bRow = 2;
                foreach (var b in bookings)
                {
                    decimal bSum = b.Payments.Sum(p => (decimal?)p.Amount) ?? 0m;
                    bookSheet.Cell(bRow, 1).Value = b.BookingId;
                    bookSheet.Cell(bRow, 2).Value = b.User?.Fio ?? "-";
                    bookSheet.Cell(bRow, 3).Value = b.User?.Phone ?? "-";
                    bookSheet.Cell(bRow, 4).Value = b.Room?.Accommodation?.Name ?? "-";
                    bookSheet.Cell(bRow, 5).Value = b.Room?.RoomNumber ?? "-";
                    bookSheet.Cell(bRow, 6).Value = b.CheckIn.ToString("dd.MM.yyyy");
                    bookSheet.Cell(bRow, 7).Value = b.CheckOut.ToString("dd.MM.yyyy");
                    bookSheet.Cell(bRow, 8).Value = b.Guests;
                    bookSheet.Cell(bRow, 9).Value = b.Status?.StatusName ?? "-";
                    bookSheet.Cell(bRow, 10).Value = bSum;
                    bookSheet.Cell(bRow, 10).Style.NumberFormat.Format = "#,##0 ₽";
                    bRow++;
                }

                var bRange = bookSheet.Range(1, 1, bRow - 1, bHeaders.Length);
                bRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                bRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                bookSheet.Columns().AdjustToContents();

                workbook.SaveAs(filePath);
            }

            MessageBox.Show($"Книга Excel успешно сформирована:\n{filePath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при экспорте в Excel: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Шаг 15: Выгрузка аналитического отчета в PDF (.pdf) через PdfSharpCore
    private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new BookingDbContext();
            var accommodations = db.Accommodations
                .Include(a => a.City)
                .Include(a => a.Type)
                .Include(a => a.RoomsNavigation).ThenInclude(r => r.Bookings).ThenInclude(b => b.Payments)
                .OrderBy(a => a.Name)
                .ToList();

            var cities = db.Cities.OrderBy(c => c.CityName).ToList();

            string exportDir = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = System.IO.Path.Combine(exportDir, "Hotel_Booking_Report.pdf");

            using (var doc = new PdfDocument())
            {
                doc.Info.Title = "Аналитический отчет гостиничного сервиса";
                doc.Info.Author = "Черненькая В.В.";
                doc.Info.Subject = "Сводная аналитика объектов размещения, броней и выручки";

                var page = doc.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                page.Orientation = PdfSharpCore.PageOrientation.Portrait;

                var gfx = XGraphics.FromPdfPage(page);

                var titleFont = new XFont("Arial", 13, XFontStyle.Bold);
                var subtitleFont = new XFont("Arial", 8.5, XFontStyle.Regular);
                var sectionFont = new XFont("Arial", 10.5, XFontStyle.Bold);
                var thFont = new XFont("Arial", 8, XFontStyle.Bold);
                var cellFont = new XFont("Arial", 8, XFontStyle.Regular);
                var cellBoldFont = new XFont("Arial", 8, XFontStyle.Bold);
                var footerFont = new XFont("Arial", 7.5, XFontStyle.Italic);

                var primaryColor = XColor.FromArgb(35, 62, 118);
                var secondaryColor = XColor.FromArgb(1, 152, 175);
                var textColor = XColor.FromArgb(34, 34, 34);
                var headerBgColor = XColor.FromArgb(235, 240, 248);
                var totalBgColor = XColor.FromArgb(245, 247, 250);
                var borderColor = XColor.FromArgb(210, 215, 222);

                var primaryBrush = new XSolidBrush(primaryColor);
                var textBrush = new XSolidBrush(textColor);
                var borderPen = new XPen(borderColor, 0.5);
                var headerPen = new XPen(primaryColor, 1);

                double margin = 36;
                double contentWidth = page.Width - 2 * margin;
                double y = margin;

                void CheckNewPage(double neededHeight)
                {
                    if (y + neededHeight > page.Height - 40)
                    {
                        gfx.DrawString($"Стр. {doc.PageCount} | Отчет сформирован: Черненькая В.В. | Сервис бронирования отелей",
                            footerFont, XBrushes.Gray, new XRect(margin, page.Height - 30, contentWidth, 15), XStringFormats.Center);

                        gfx.Dispose();
                        page = doc.AddPage();
                        page.Size = PdfSharpCore.PageSize.A4;
                        page.Orientation = PdfSharpCore.PageOrientation.Portrait;
                        gfx = XGraphics.FromPdfPage(page);
                        y = margin;
                    }
                }

                // Шапка
                gfx.DrawRectangle(new XSolidBrush(primaryColor), margin, y, contentWidth, 4);
                y += 12;

                gfx.DrawString("СЕРВИС БРОНИРОВАНИЯ ГОСТИНИЦ И АПАРТАМЕНТОВ", titleFont, primaryBrush, margin, y);
                y += 17;

                gfx.DrawString($"Сводный аналитический отчет по объектам и выручке | Дата: {DateTime.Now:dd.MM.yyyy HH:mm} | Администратор: Черненькая В.В.", subtitleFont, XBrushes.DarkSlateGray, margin, y);
                y += 14;

                gfx.DrawLine(new XPen(secondaryColor, 1), margin, y, margin + contentWidth, y);
                y += 15;

                // Раздел 1: Объекты размещения
                gfx.DrawString("1. Сводные показатели по объектам размещения", sectionFont, primaryBrush, margin, y);
                y += 13;

                double[] colWidths = { 170, 95, 80, 50, 55, 73 }; // total 523
                string[] colHeaders = { "Объект размещения", "Тип", "Город", "Номеров", "Броней", "Выручка" };

                double rowHeight = 19;
                gfx.DrawRectangle(new XSolidBrush(headerBgColor), margin, y, contentWidth, rowHeight);
                gfx.DrawRectangle(headerPen, margin, y, contentWidth, rowHeight);

                double curX = margin;
                for (int i = 0; i < colWidths.Length; i++)
                {
                    var format = (i >= 3) ? XStringFormats.Center : XStringFormats.CenterLeft;
                    double padLeft = (i < 3) ? 5 : 0;
                    gfx.DrawString(colHeaders[i], thFont, primaryBrush, new XRect(curX + padLeft, y, colWidths[i] - padLeft, rowHeight), format);
                    if (i > 0)
                    {
                        gfx.DrawLine(borderPen, curX, y, curX, y + rowHeight);
                    }
                    curX += colWidths[i];
                }
                y += rowHeight;

                int grandTotalBookings = 0;
                decimal grandTotalRevenue = 0;
                bool isAlternate = false;

                foreach (var acc in accommodations)
                {
                    CheckNewPage(rowHeight);

                    int bCount = acc.RoomsNavigation.SelectMany(r => r.Bookings).Count();
                    decimal rev = acc.RoomsNavigation
                        .SelectMany(r => r.Bookings)
                        .SelectMany(b => b.Payments)
                        .Where(p => p.PaymentStatus == "Оплачено")
                        .Sum(p => (decimal?)p.Amount) ?? 0m;

                    grandTotalBookings += bCount;
                    grandTotalRevenue += rev;

                    if (isAlternate)
                    {
                        gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(249, 250, 252)), margin, y, contentWidth, rowHeight);
                    }
                    gfx.DrawRectangle(borderPen, margin, y, contentWidth, rowHeight);

                    string[] rowValues = {
                        acc.Name,
                        acc.Type?.TypeName ?? "-",
                        acc.City?.CityName ?? "-",
                        acc.Rooms.ToString(),
                        bCount.ToString(),
                        $"{rev:N0} ₽"
                    };

                    curX = margin;
                    for (int i = 0; i < colWidths.Length; i++)
                    {
                        var format = (i >= 3) ? XStringFormats.Center : XStringFormats.CenterLeft;
                        double padLeft = (i < 3) ? 5 : 0;
                        var font = (i == 5) ? cellBoldFont : cellFont;
                        gfx.DrawString(rowValues[i], font, textBrush, new XRect(curX + padLeft, y, colWidths[i] - padLeft, rowHeight), format);
                        if (i > 0)
                        {
                            gfx.DrawLine(borderPen, curX, y, curX, y + rowHeight);
                        }
                        curX += colWidths[i];
                    }

                    y += rowHeight;
                    isAlternate = !isAlternate;
                }

                // Строка Итого
                CheckNewPage(rowHeight);
                gfx.DrawRectangle(new XSolidBrush(totalBgColor), margin, y, contentWidth, rowHeight);
                gfx.DrawRectangle(headerPen, margin, y, contentWidth, rowHeight);

                double totalLabelWidth = colWidths[0] + colWidths[1] + colWidths[2];
                gfx.DrawString("ИТОГО ПО СЕРВИСУ:", cellBoldFont, primaryBrush, new XRect(margin + 5, y, totalLabelWidth - 10, rowHeight), XStringFormats.CenterLeft);

                curX = margin + totalLabelWidth;
                gfx.DrawLine(borderPen, curX, y, curX, y + rowHeight);
                gfx.DrawString(accommodations.Sum(a => a.Rooms).ToString(), cellBoldFont, primaryBrush, new XRect(curX, y, colWidths[3], rowHeight), XStringFormats.Center);

                curX += colWidths[3];
                gfx.DrawLine(borderPen, curX, y, curX, y + rowHeight);
                gfx.DrawString(grandTotalBookings.ToString(), cellBoldFont, primaryBrush, new XRect(curX, y, colWidths[4], rowHeight), XStringFormats.Center);

                curX += colWidths[4];
                gfx.DrawLine(borderPen, curX, y, curX, y + rowHeight);
                gfx.DrawString($"{grandTotalRevenue:N0} ₽", cellBoldFont, new XSolidBrush(XColor.FromArgb(180, 40, 40)), new XRect(curX, y, colWidths[5], rowHeight), XStringFormats.Center);

                y += rowHeight + 18;

                // Раздел 2: Аналитика по городам
                CheckNewPage(80);
                gfx.DrawString("2. Распределение бронирований и выручки по городам", sectionFont, primaryBrush, margin, y);
                y += 13;

                double[] cityColWidths = { 203, 160, 160 }; // total 523
                string[] cityHeaders = { "Город присутствия", "Всего броней (шт.)", "Суммарная выручка (₽)" };

                gfx.DrawRectangle(new XSolidBrush(headerBgColor), margin, y, contentWidth, rowHeight);
                gfx.DrawRectangle(headerPen, margin, y, contentWidth, rowHeight);

                curX = margin;
                for (int i = 0; i < cityColWidths.Length; i++)
                {
                    var format = (i == 0) ? XStringFormats.CenterLeft : XStringFormats.Center;
                    double padLeft = (i == 0) ? 5 : 0;
                    gfx.DrawString(cityHeaders[i], thFont, primaryBrush, new XRect(curX + padLeft, y, cityColWidths[i] - padLeft, rowHeight), format);
                    if (i > 0)
                    {
                        gfx.DrawLine(borderPen, curX, y, curX, y + rowHeight);
                    }
                    curX += cityColWidths[i];
                }
                y += rowHeight;

                isAlternate = false;
                foreach (var c in cities)
                {
                    CheckNewPage(rowHeight);

                    int cBookings = db.Bookings.Count(b => b.Room != null && b.Room.Accommodation != null && b.Room.Accommodation.CityId == c.CityId);
                    decimal cRev = db.Payments
                        .Where(p => p.PaymentStatus == "Оплачено" && p.Booking != null && p.Booking.Room != null && p.Booking.Room.Accommodation != null && p.Booking.Room.Accommodation.CityId == c.CityId)
                        .Sum(p => (decimal?)p.Amount) ?? 0m;

                    if (isAlternate)
                    {
                        gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(249, 250, 252)), margin, y, contentWidth, rowHeight);
                    }
                    gfx.DrawRectangle(borderPen, margin, y, contentWidth, rowHeight);

                    curX = margin;
                    gfx.DrawString(c.CityName, cellFont, textBrush, new XRect(curX + 5, y, cityColWidths[0] - 5, rowHeight), XStringFormats.CenterLeft);
                    curX += cityColWidths[0];
                    gfx.DrawLine(borderPen, curX, y, curX, y + rowHeight);

                    gfx.DrawString(cBookings.ToString(), cellFont, textBrush, new XRect(curX, y, cityColWidths[1], rowHeight), XStringFormats.Center);
                    curX += cityColWidths[1];
                    gfx.DrawLine(borderPen, curX, y, curX, y + rowHeight);

                    gfx.DrawString($"{cRev:N0} ₽", cellFont, textBrush, new XRect(curX, y, cityColWidths[2], rowHeight), XStringFormats.Center);

                    y += rowHeight;
                    isAlternate = !isAlternate;
                }

                gfx.DrawString($"Стр. {doc.PageCount} | Отчет сформирован: Черненькая В.В. | Сервис бронирования отелей",
                    footerFont, XBrushes.Gray, new XRect(margin, page.Height - 30, contentWidth, 15), XStringFormats.Center);

                gfx.Dispose();
                doc.Save(filePath);
            }

            MessageBox.Show($"Аналитический отчет в формате PDF успешно сформирован:\n{filePath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при экспорте в PDF: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (NavigationService.CanGoBack)
        {
            NavigationService.GoBack();
        }
        else
        {
            NavigationService.Navigate(new AdminHubPage());
        }
    }
}
