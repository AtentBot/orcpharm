namespace DTOs;

public class CreatePrescriptionDto
{
    public Guid CustomerId { get; set; }
    public DateTime PrescriptionDate { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorCrm { get; set; } = string.Empty;
    public string DoctorCrmState { get; set; } = string.Empty;
    public string PrescriptionType { get; set; } = "COMUM";
    public string? ControlledType { get; set; }
    public string? PrescriptionColor { get; set; }
    public string Medications { get; set; } = string.Empty;
    public string Posology { get; set; } = string.Empty;
    public string? Observations { get; set; }
    public string? ImageUrl { get; set; }
}

public class UpdatePrescriptionDto
{
    public DateTime PrescriptionDate { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorCrm { get; set; } = string.Empty;
    public string DoctorCrmState { get; set; } = string.Empty;
    public string PrescriptionType { get; set; } = "COMUM";
    public string? ControlledType { get; set; }
    public string? PrescriptionColor { get; set; }
    public string Medications { get; set; } = string.Empty;
    public string Posology { get; set; } = string.Empty;
    public string? Observations { get; set; }
}

public class ValidatePrescriptionDto
{
    public bool IsValid { get; set; }
    public string? ValidationNotes { get; set; }
}

public class CancelPrescriptionDto
{
    public string Reason { get; set; } = string.Empty;
}

public class PrescriptionResponseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerCpf { get; set; }
    public DateTime PrescriptionDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public int DaysUntilExpiration { get; set; }
    public bool IsExpired { get; set; }

    public string DoctorName { get; set; } = string.Empty;
    public string DoctorCrm { get; set; } = string.Empty;
    public string DoctorCrmState { get; set; } = string.Empty;

    public string PrescriptionType { get; set; } = string.Empty;
    public string? ControlledType { get; set; }
    public string? PrescriptionColor { get; set; }

    public string Medications { get; set; } = string.Empty;
    public string Posology { get; set; } = string.Empty;
    public string? Observations { get; set; }

    public string? ImageUrl { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTime? ValidatedAt { get; set; }
    public string? ValidatedByEmployeeName { get; set; }
    public string? ValidationNotes { get; set; }

    public Guid? ManipulationOrderId { get; set; }
    public string? ManipulationOrderCode { get; set; }
    public DateTime? ManipulationGeneratedAt { get; set; }

    public DateTime? CancelledAt { get; set; }
    public string? CancelledByEmployeeName { get; set; }
    public string? CancellationReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedByEmployeeName { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedByEmployeeName { get; set; }
}

public class PrescriptionListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime PrescriptionDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string PrescriptionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsExpired { get; set; }
    public int DaysUntilExpiration { get; set; }
}

public class GenerateManipulationFromPrescriptionDto
{
    public Guid FormulaId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime ExpectedDate { get; set; }
    public string? AdditionalNotes { get; set; }
}
