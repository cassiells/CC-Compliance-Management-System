using System.Collections.Generic;
using DoComply.Models;

namespace DoComply.Services
{
    public interface IReportPdfService
    {
        byte[] GenerateComplianceReportPdf(IEnumerable<VwComplianceReport> reports, string? search = null, string? status = null, string? generatedBy = null);
        byte[] GenerateExpiredDocumentsReportPdf(IEnumerable<VwExpiredDocumentsReport> reports, string? search = null, string? generatedBy = null);
        byte[] GenerateExpiringDocumentsReportPdf(IEnumerable<VwExpiringDocumentsReport> reports, string? search = null, string? range = null, string? generatedBy = null);
    }
}
