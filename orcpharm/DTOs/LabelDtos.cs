namespace DTOs;

public class CreateLabelTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TemplateType { get; set; } = "PADRAO";
    public string? PharmaceuticalForm { get; set; }
    public decimal Width { get; set; } = 100;
    public decimal Height { get; set; } = 50;
    public string HtmlTemplate { get; set; } = string.Empty;
    public string? CssStyles { get; set; }
    public bool IncludeEstablishmentName { get; set; } = true;
    public bool IncludePharmacistName { get; set; } = true;
    public bool IncludeFormulaName { get; set; } = true;
    public bool IncludeComposition { get; set; } = true;
    public bool IncludePosology { get; set; } = true;
    public bool IncludeValidity { get; set; } = true;
    public bool IncludeBatchNumber { get; set; } = true;
    public bool IncludeManipulationDate { get; set; } = true;
    public bool IncludePatientName { get; set; } = true;
    public bool IncludeQrCode { get; set; } = true;
    public bool IncludeWarnings { get; set; } = true;
    public bool IsDefault { get; set; } = false;
}

public class UpdateLabelTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string HtmlTemplate { get; set; } = string.Empty;
    public string? CssStyles { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
}

public class LabelTemplateResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TemplateType { get; set; } = string.Empty;
    public string? PharmaceuticalForm { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GenerateLabelDto
{
    public Guid ManipulationOrderId { get; set; }
    public Guid? TemplateId { get; set; }
    public string? CustomWarnings { get; set; }
    public string? CustomStorageInstructions { get; set; }
}

public class GeneratedLabelResponseDto
{
    public Guid Id { get; set; }
    public string LabelCode { get; set; } = string.Empty;
    public Guid ManipulationOrderId { get; set; }
    public string ManipulationOrderCode { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string FormulaName { get; set; } = string.Empty;
    public DateTime ManipulationDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string QrCodeData { get; set; } = string.Empty;
    public string? QrCodeImageUrl { get; set; }
    public string GeneratedHtml { get; set; } = string.Empty;
    public int PrintCount { get; set; }
    public DateTime? LastPrintedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PrintLabelDto
{
    public int Copies { get; set; } = 1;
    public string? PrinterName { get; set; }
}

public class LabelPreviewDto
{
    public Guid ManipulationOrderId { get; set; }
    public Guid TemplateId { get; set; }
}