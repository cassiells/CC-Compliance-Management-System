using System;
using System.Collections.Generic;
using System.Linq;
using DoComply.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DoComply.Services
{
    public class ReportPdfService : IReportPdfService
    {
        private const string PrimaryNavy = "#0B2545";
        private const string SecondaryNavy = "#134074";
        private const string SoftBg = "#F8FAFC";
        private const string BorderGray = "#E2E8F0";
        private const string MutedGray = "#64748B";

        public byte[] GenerateComplianceReportPdf(IEnumerable<VwComplianceReport> reports, string? search = null, string? status = null, string? generatedBy = null)
        {
            var list = reports.ToList();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim();
                list = list.Where(r => r.SupplierName.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                list = list.Where(r => r.ComplianceStatus.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            int total = list.Count;
            int compliant = list.Count(r => r.ComplianceStatus.Equals("Compliant", StringComparison.OrdinalIgnoreCase));
            int nonCompliant = total - compliant;
            int rate = total > 0 ? (int)Math.Round((double)compliant / total * 100) : 0;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(32);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor("#1E293B"));

                    page.Header().Column(col =>
                    {
                        col.Item().Element(c => RenderReportHeader(c, "Supplier Compliance Standing Report", "Official assessment of supplier compliance standing and regulatory readiness", generatedBy));
                        
                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Element(c => RenderKpiBox(c, "Total Suppliers", total.ToString(), PrimaryNavy));
                            row.ConstantItem(10);
                            row.RelativeItem().Element(c => RenderKpiBox(c, "Compliant", compliant.ToString(), "#16A34A"));
                            row.ConstantItem(10);
                            row.RelativeItem().Element(c => RenderKpiBox(c, "Non-Compliant", nonCompliant.ToString(), "#DC2626"));
                            row.ConstantItem(10);
                            row.RelativeItem().Element(c => RenderKpiBox(c, "Compliance Rate", $"{rate}%", SecondaryNavy));
                        });

                        col.Item().PaddingBottom(10);
                    });

                    page.Content().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(36);  // #
                            columns.RelativeColumn(3);   // Supplier Name
                            columns.RelativeColumn(2);   // Status
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderStyle).Text("#");
                            h.Cell().Element(HeaderStyle).Text("Supplier Name");
                            h.Cell().Element(HeaderStyle).Text("Compliance Standing");
                        });

                        int idx = 1;
                        foreach (var item in list)
                        {
                            var bg = idx % 2 == 0 ? SoftBg : "#FFFFFF";
                            bool isComp = item.ComplianceStatus.Equals("Compliant", StringComparison.OrdinalIgnoreCase);

                            table.Cell().Element(c => RowStyle(c, bg)).Text(idx.ToString());
                            table.Cell().Element(c => RowStyle(c, bg)).Text(item.SupplierName).SemiBold();
                            table.Cell().Element(c => RowStyle(c, bg)).AlignLeft().Element(badge =>
                            {
                                badge.Background(isComp ? "#DCFCE7" : "#FEE2E2")
                                     .PaddingVertical(2)
                                     .PaddingHorizontal(6)
                                     .Text(item.ComplianceStatus)
                                     .FontColor(isComp ? "#15803D" : "#B91C1C")
                                     .SemiBold()
                                     .FontSize(8);
                            });

                            idx++;
                        }
                    });

                    page.Footer().Element(RenderFooter);
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateExpiredDocumentsReportPdf(IEnumerable<VwExpiredDocumentsReport> reports, string? search = null, string? generatedBy = null)
        {
            var list = reports.ToList();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim();
                list = list.Where(r => r.SupplierName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                       r.Document.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            int totalExpired = list.Count;
            int distinctSuppliers = list.Select(r => r.SupplierName).Distinct(StringComparer.OrdinalIgnoreCase).Count();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(32);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor("#1E293B"));

                    page.Header().Column(col =>
                    {
                        col.Item().Element(c => RenderReportHeader(c, "Expired Compliance Documents Audit", "Listing of expired credentials and certificates requiring immediate renewal", generatedBy));

                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Element(c => RenderKpiBox(c, "Total Expired Records", totalExpired.ToString(), "#DC2626"));
                            row.ConstantItem(12);
                            row.RelativeItem().Element(c => RenderKpiBox(c, "Affected Suppliers", distinctSuppliers.ToString(), PrimaryNavy));
                            row.ConstantItem(12);
                            row.RelativeItem().Element(c => RenderKpiBox(c, "Audit Priority", "URGENT ATTENTION", "#B91C1C"));
                        });

                        col.Item().PaddingBottom(10);
                    });

                    page.Content().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(36);  // #
                            columns.RelativeColumn(3);   // Supplier Name
                            columns.RelativeColumn(3);   // Document
                            columns.RelativeColumn(2);   // Expiry Date
                            columns.RelativeColumn(2);   // Status
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderStyle).Text("#");
                            h.Cell().Element(HeaderStyle).Text("Supplier Name");
                            h.Cell().Element(HeaderStyle).Text("Document Credential");
                            h.Cell().Element(HeaderStyle).Text("Expiration Date");
                            h.Cell().Element(HeaderStyle).Text("Audit Status");
                        });

                        int idx = 1;
                        foreach (var item in list)
                        {
                            var bg = idx % 2 == 0 ? SoftBg : "#FFFFFF";
                            var expStr = item.ExpiryDate.HasValue ? item.ExpiryDate.Value.ToString("MMM dd, yyyy") : "Expired";

                            table.Cell().Element(c => RowStyle(c, bg)).Text(idx.ToString());
                            table.Cell().Element(c => RowStyle(c, bg)).Text(item.SupplierName).SemiBold();
                            table.Cell().Element(c => RowStyle(c, bg)).Text(item.Document);
                            table.Cell().Element(c => RowStyle(c, bg)).Text(expStr).FontColor("#DC2626").SemiBold();
                            table.Cell().Element(c => RowStyle(c, bg)).AlignLeft().Element(badge =>
                            {
                                badge.Background("#FEE2E2")
                                     .PaddingVertical(2)
                                     .PaddingHorizontal(6)
                                     .Text("EXPIRED")
                                     .FontColor("#B91C1C")
                                     .SemiBold()
                                     .FontSize(8);
                            });

                            idx++;
                        }
                    });

                    page.Footer().Element(RenderFooter);
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateExpiringDocumentsReportPdf(IEnumerable<VwExpiringDocumentsReport> reports, string? search = null, string? range = null, string? generatedBy = null)
        {
            var list = reports.ToList();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim();
                list = list.Where(r => r.SupplierName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                       r.Document.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (!string.IsNullOrWhiteSpace(range))
            {
                if (range.Equals("critical", StringComparison.OrdinalIgnoreCase))
                {
                    list = list.Where(r => r.ExpiryDate.HasValue && (r.ExpiryDate.Value.DayNumber - today.DayNumber) <= 15).ToList();
                }
                else if (range.Equals("30", StringComparison.OrdinalIgnoreCase))
                {
                    list = list.Where(r => r.ExpiryDate.HasValue && (r.ExpiryDate.Value.DayNumber - today.DayNumber) <= 30).ToList();
                }
            }

            int totalExpiring = list.Count;
            int criticalCount = list.Count(r => r.ExpiryDate.HasValue && (r.ExpiryDate.Value.DayNumber - today.DayNumber) <= 15);
            int upcomingCount = totalExpiring - criticalCount;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(32);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor("#1E293B"));

                    page.Header().Column(col =>
                    {
                        col.Item().Element(c => RenderReportHeader(c, "Expiring Documents Monitoring Report", "Early warning tracking for supplier credentials nearing required expiration dates", generatedBy));

                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Element(c => RenderKpiBox(c, "Total Expiring", totalExpiring.ToString(), SecondaryNavy));
                            row.ConstantItem(12);
                            row.RelativeItem().Element(c => RenderKpiBox(c, "Critical (≤15 Days)", criticalCount.ToString(), "#DC2626"));
                            row.ConstantItem(12);
                            row.RelativeItem().Element(c => RenderKpiBox(c, "Upcoming (16-60 Days)", upcomingCount.ToString(), "#D97706"));
                        });

                        col.Item().PaddingBottom(10);
                    });

                    page.Content().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(36);  // #
                            columns.RelativeColumn(3);   // Supplier Name
                            columns.RelativeColumn(3);   // Document
                            columns.RelativeColumn(2);   // Expiry Date
                            columns.RelativeColumn(2);   // Days Remaining
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderStyle).Text("#");
                            h.Cell().Element(HeaderStyle).Text("Supplier Name");
                            h.Cell().Element(HeaderStyle).Text("Document Credential");
                            h.Cell().Element(HeaderStyle).Text("Expiration Date");
                            h.Cell().Element(HeaderStyle).Text("Urgency Standing");
                        });

                        int idx = 1;
                        foreach (var item in list)
                        {
                            var bg = idx % 2 == 0 ? SoftBg : "#FFFFFF";
                            var expStr = item.ExpiryDate.HasValue ? item.ExpiryDate.Value.ToString("MMM dd, yyyy") : "N/A";
                            int daysLeft = item.ExpiryDate.HasValue ? Math.Max(0, item.ExpiryDate.Value.DayNumber - today.DayNumber) : 0;
                            bool isCritical = daysLeft <= 15;

                            table.Cell().Element(c => RowStyle(c, bg)).Text(idx.ToString());
                            table.Cell().Element(c => RowStyle(c, bg)).Text(item.SupplierName).SemiBold();
                            table.Cell().Element(c => RowStyle(c, bg)).Text(item.Document);
                            table.Cell().Element(c => RowStyle(c, bg)).Text(expStr);
                            table.Cell().Element(c => RowStyle(c, bg)).AlignLeft().Element(badge =>
                            {
                                badge.Background(isCritical ? "#FEF3C7" : "#EFF6FF")
                                     .PaddingVertical(2)
                                     .PaddingHorizontal(6)
                                     .Text(isCritical ? $"CRITICAL: {daysLeft}d left" : $"{daysLeft} days remaining")
                                     .FontColor(isCritical ? "#B45309" : "#1D4ED8")
                                     .SemiBold()
                                     .FontSize(8);
                            });

                            idx++;
                        }
                    });

                    page.Footer().Element(RenderFooter);
                });
            });

            return document.GeneratePdf();
        }

        private static void RenderReportHeader(IContainer container, string title, string subtitle, string? generatedBy)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    var logoPath = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "images", "cc-logo.png");
                    if (!System.IO.File.Exists(logoPath))
                    {
                        logoPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "cc-logo.png");
                    }

                    if (System.IO.File.Exists(logoPath))
                    {
                        row.ConstantItem(42).Height(32).Image(logoPath);
                        row.ConstantItem(8);
                    }

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("CC COMPLIANCE").FontSize(14).ExtraBold().FontColor(PrimaryNavy);
                        c.Item().Text("Corporate Compliance & Regulatory Oversight System").FontSize(8).FontColor(MutedGray);
                    });

                    row.ConstantItem(180).AlignRight().Column(c =>
                    {
                        c.Item().Text($"Date: {DateTime.UtcNow:MMM dd, yyyy HH:mm} UTC").FontSize(8).FontColor(MutedGray);
                        if (!string.IsNullOrWhiteSpace(generatedBy))
                        {
                            c.Item().Text($"User: {generatedBy}").FontSize(8).FontColor(MutedGray);
                        }
                    });
                });

                col.Item().PaddingTop(6).LineHorizontal(1).LineColor(BorderGray);

                col.Item().PaddingTop(8).Text(title).FontSize(15).Bold().FontColor(PrimaryNavy);
                col.Item().PaddingTop(2).Text(subtitle).FontSize(8.5f).FontColor(MutedGray);
            });
        }

        private static void RenderKpiBox(IContainer container, string label, string value, string color)
        {
            container.Border(1)
                     .BorderColor(BorderGray)
                     .Background(SoftBg)
                     .Padding(8)
                     .Column(col =>
                     {
                         col.Item().Text(label).FontSize(8).SemiBold().FontColor(MutedGray);
                         col.Item().PaddingTop(2).Text(value).FontSize(14).ExtraBold().FontColor(color);
                     });
        }

        private static IContainer HeaderStyle(IContainer container)
        {
            return container.Background(PrimaryNavy)
                            .BorderBottom(1)
                            .BorderColor(BorderGray)
                            .PaddingVertical(6)
                            .PaddingHorizontal(6)
                            .DefaultTextStyle(x => x.FontColor(Colors.White).SemiBold().FontSize(8.5f));
        }

        private static IContainer RowStyle(IContainer container, string bgColor)
        {
            return container.Background(bgColor)
                            .BorderBottom(0.5f)
                            .BorderColor(BorderGray)
                            .PaddingVertical(5)
                            .PaddingHorizontal(6)
                            .DefaultTextStyle(x => x.FontSize(8.5f));
        }

        private static void RenderFooter(IContainer container)
        {
            container.PaddingTop(12)
                     .Column(col =>
                     {
                         col.Item().LineHorizontal(0.5f).LineColor(BorderGray);
                         col.Item().PaddingTop(4).Row(row =>
                         {
                             row.RelativeItem().Text("Generated via QuestPDF • CC Compliance Audit Engine • Strictly Confidential")
                                 .FontSize(7.5f)
                                 .FontColor(MutedGray);

                             row.RelativeItem().AlignRight().Text(x =>
                             {
                                 x.Span("Page ").FontSize(7.5f).FontColor(MutedGray);
                                 x.CurrentPageNumber().FontSize(7.5f).FontColor(MutedGray);
                                 x.Span(" of ").FontSize(7.5f).FontColor(MutedGray);
                                 x.TotalPages().FontSize(7.5f).FontColor(MutedGray);
                             });
                         });
                     });
        }
    }
}
