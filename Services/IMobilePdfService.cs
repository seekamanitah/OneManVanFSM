namespace OneManVanFSM.Services;

/// <summary>
/// Mobile PDF/document generation service. Generates HTML documents
/// for estimates, invoices, and reports that can be shared or printed.
/// On mobile, documents are saved as HTML files and opened via the
/// platform share sheet (which allows saving, emailing, or printing).
/// </summary>
public interface IMobilePdfService
{
    /// <summary>Generates an estimate document as an HTML file and returns the file path.</summary>
    Task<string?> GenerateEstimateDocumentAsync(int estimateId);

    /// <summary>Generates an invoice document as an HTML file and returns the file path.</summary>
    Task<string?> GenerateInvoiceDocumentAsync(int invoiceId);

    /// <summary>Generates a tech performance report as an HTML file and returns the file path.</summary>
    Task<string?> GenerateTechReportDocumentAsync(int employeeId);

    /// <summary>Shares a generated document file using the platform share sheet.</summary>
    Task ShareDocumentAsync(string filePath, string title);
}
