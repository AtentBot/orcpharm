using Data;
using DTOs.Cash;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Service;

public class CashRegisterService
{
    private readonly AppDbContext _context;

    public CashRegisterService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, CashRegister? CashRegister)> OpenCashRegisterAsync(
        Guid establishmentId,
        Guid employeeId,
        decimal openingBalance,
        string? observations)
    {
        var existingOpen = await _context.Set<CashRegister>()
            .AnyAsync(c => c.EstablishmentId == establishmentId && c.Status == "ABERTO");

        if (existingOpen)
            return (false, "Já existe um caixa aberto para este estabelecimento", null);

        var code = await GenerateCashRegisterCode(establishmentId);

        var cashRegister = new CashRegister
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId,
            Code = code,
            OpeningDate = DateTime.UtcNow,
            OpenedByEmployeeId = employeeId,
            OpeningBalance = openingBalance,
            Status = "ABERTO",
            Observations = observations,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<CashRegister>().Add(cashRegister);

        var movement = new CashMovement
        {
            Id = Guid.NewGuid(),
            CashRegisterId = cashRegister.Id,
            MovementType = "ABERTURA",
            Amount = openingBalance,
            Description = "Abertura de caixa",
            EmployeeId = employeeId,
            MovementDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<CashMovement>().Add(movement);

        await _context.SaveChangesAsync();

        return (true, "Caixa aberto com sucesso", cashRegister);
    }

    public async Task<(bool Success, string Message)> CloseCashRegisterAsync(
        Guid cashRegisterId,
        Guid employeeId,
        decimal closingBalance,
        string? observations)
    {
        var cashRegister = await _context.Set<CashRegister>()
            .FirstOrDefaultAsync(c => c.Id == cashRegisterId);

        if (cashRegister == null)
            return (false, "Caixa não encontrado");

        if (cashRegister.Status == "FECHADO")
            return (false, "Caixa já está fechado");

        var expectedBalance = await CalculateExpectedBalance(cashRegisterId);

        cashRegister.ClosingDate = DateTime.UtcNow;
        cashRegister.ClosedByEmployeeId = employeeId;
        cashRegister.ClosingBalance = closingBalance;
        cashRegister.ExpectedBalance = expectedBalance;
        cashRegister.Difference = closingBalance - expectedBalance;
        cashRegister.Status = "FECHADO";
        cashRegister.Observations = observations;
        cashRegister.UpdatedAt = DateTime.UtcNow;

        var movement = new CashMovement
        {
            Id = Guid.NewGuid(),
            CashRegisterId = cashRegisterId,
            MovementType = "FECHAMENTO",
            Amount = closingBalance,
            Description = $"Fechamento de caixa - Diferença: {cashRegister.Difference:C}",
            EmployeeId = employeeId,
            MovementDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<CashMovement>().Add(movement);

        await _context.SaveChangesAsync();

        return (true, "Caixa fechado com sucesso");
    }

    public async Task<CashRegister?> GetOpenCashRegisterAsync(Guid establishmentId)
    {
        return await _context.Set<CashRegister>()
            .Include(c => c.OpenedByEmployee)
            .Include(c => c.Movements)
            .FirstOrDefaultAsync(c => c.EstablishmentId == establishmentId && c.Status == "ABERTO");
    }

    public async Task<(bool Success, string Message)> RegisterSaleInCashRegisterAsync(
        Guid cashRegisterId,
        Guid saleId,
        decimal amount,
        string paymentMethod,
        Guid employeeId)
    {
        var cashRegister = await _context.Set<CashRegister>()
            .FirstOrDefaultAsync(c => c.Id == cashRegisterId);

        if (cashRegister == null)
            return (false, "Caixa não encontrado");

        if (cashRegister.Status != "ABERTO")
            return (false, "Caixa não está aberto");

        var movement = new CashMovement
        {
            Id = Guid.NewGuid(),
            CashRegisterId = cashRegisterId,
            MovementType = "VENDA",
            Amount = amount,
            PaymentMethod = paymentMethod,
            SaleId = saleId,
            Description = $"Venda {paymentMethod}",
            EmployeeId = employeeId,
            MovementDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<CashMovement>().Add(movement);

        cashRegister.TotalSales += amount;
        cashRegister.SalesCount++;

        switch (paymentMethod.ToUpper())
        {
            case "DINHEIRO":
                cashRegister.TotalCash += amount;
                break;
            case "CARTAO_CREDITO":
            case "CARTAO_DEBITO":
                cashRegister.TotalCard += amount;
                break;
            case "PIX":
                cashRegister.TotalPix += amount;
                break;
        }

        cashRegister.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (true, "Venda registrada no caixa");
    }

    public async Task<(bool Success, string Message)> AddSupplyAsync(
        Guid cashRegisterId,
        decimal amount,
        string description,
        Guid employeeId)
    {
        var cashRegister = await _context.Set<CashRegister>()
            .FirstOrDefaultAsync(c => c.Id == cashRegisterId);

        if (cashRegister == null)
            return (false, "Caixa não encontrado");

        if (cashRegister.Status != "ABERTO")
            return (false, "Caixa não está aberto");

        var movement = new CashMovement
        {
            Id = Guid.NewGuid(),
            CashRegisterId = cashRegisterId,
            MovementType = "SUPRIMENTO",
            Amount = amount,
            PaymentMethod = "DINHEIRO",
            Description = description,
            EmployeeId = employeeId,
            MovementDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<CashMovement>().Add(movement);

        cashRegister.TotalCash += amount;
        cashRegister.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (true, "Suprimento adicionado");
    }

    public async Task<(bool Success, string Message)> AddWithdrawalAsync(
        Guid cashRegisterId,
        decimal amount,
        string description,
        Guid employeeId)
    {
        var cashRegister = await _context.Set<CashRegister>()
            .FirstOrDefaultAsync(c => c.Id == cashRegisterId);

        if (cashRegister == null)
            return (false, "Caixa não encontrado");

        if (cashRegister.Status != "ABERTO")
            return (false, "Caixa não está aberto");

        var movement = new CashMovement
        {
            Id = Guid.NewGuid(),
            CashRegisterId = cashRegisterId,
            MovementType = "SANGRIA",
            Amount = amount,
            PaymentMethod = "DINHEIRO",
            Description = description,
            EmployeeId = employeeId,
            MovementDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<CashMovement>().Add(movement);

        cashRegister.TotalCash -= amount;
        cashRegister.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return (true, "Sangria registrada");
    }

    private async Task<decimal> CalculateExpectedBalance(Guid cashRegisterId)
    {
        var cashRegister = await _context.Set<CashRegister>()
            .FirstOrDefaultAsync(c => c.Id == cashRegisterId);

        if (cashRegister == null)
            return 0;

        var movements = await _context.Set<CashMovement>()
            .Where(m => m.CashRegisterId == cashRegisterId)
            .ToListAsync();

        decimal expected = cashRegister.OpeningBalance;

        foreach (var movement in movements)
        {
            switch (movement.MovementType)
            {
                case "VENDA":
                    if (movement.PaymentMethod == "DINHEIRO")
                        expected += movement.Amount;
                    break;
                case "SUPRIMENTO":
                    expected += movement.Amount;
                    break;
                case "SANGRIA":
                    expected -= movement.Amount;
                    break;
            }
        }

        return expected;
    }

    private async Task<string> GenerateCashRegisterCode(Guid establishmentId)
    {
        var date = DateTime.UtcNow;
        var prefix = $"CX{date:yyyyMMdd}";

        var lastCash = await _context.Set<CashRegister>()
            .Where(c => c.EstablishmentId == establishmentId && c.Code.StartsWith(prefix))
            .OrderByDescending(c => c.Code)
            .Select(c => c.Code)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastCash != null)
        {
            var numberPart = lastCash.Substring(prefix.Length);
            if (int.TryParse(numberPart, out int lastNumber))
                nextNumber = lastNumber + 1;
        }

        return $"{prefix}{nextNumber:D2}";
    }
}
