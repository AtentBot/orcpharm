using Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Core;
using Models.Employees;
using Models.Pharmacy;
using Models.Security;
using System.Collections.Generic;

namespace Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Existentes
    public DbSet<Establishment> Establishments => Set<Establishment>();
    public DbSet<AccessLevel> AccessLevels => Set<AccessLevel>();
    public DbSet<UserSession> Sessions => Set<UserSession>();
    public DbSet<ClientOnboarding> ClientOnboardings { get; set; } = null!;

    // Novos - Farmácia
    public DbSet<RawMaterial> RawMaterials => Set<RawMaterial>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<Formula> Formulas => Set<Formula>();
    public DbSet<FormulaComponent> FormulaComponents => Set<FormulaComponent>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeSession> EmployeeSessions => Set<EmployeeSession>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<ManipulationOrder> ManipulationOrders => Set<ManipulationOrder>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    // ==================== NOVOS MODELOS - EMPLOYEES ====================

    public DbSet<JobPosition> JobPositions => Set<JobPosition>();
    public DbSet<EmployeeJobHistory> EmployeeJobHistories => Set<EmployeeJobHistory>();
    public DbSet<EmployeeBenefit> EmployeeBenefits => Set<EmployeeBenefit>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();

    // ==================== NOVOS MODELOS - SECURITY ====================
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AccessProfile> AccessProfiles => Set<AccessProfile>();

    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar configurações
        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new JobPositionConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeJobHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeSessionConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeBenefitConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionConfiguration());
        modelBuilder.ApplyConfiguration(new RolePermissionConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());


        SeedInitialData(modelBuilder);


        // RawMaterial -> Establishment
        modelBuilder.Entity<RawMaterial>()
            .HasOne(r => r.Establishment)
            .WithMany()
            .HasForeignKey(r => r.EstablishmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Batch -> RawMaterial
        modelBuilder.Entity<Batch>()
            .HasOne(b => b.RawMaterial)
            .WithMany(r => r.Batches)
            .HasForeignKey(b => b.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        // Batch -> Supplier
        modelBuilder.Entity<Batch>()
            .HasOne(b => b.Supplier)
            .WithMany(s => s.Batches)
            .HasForeignKey(b => b.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        // Formula -> Establishment
        modelBuilder.Entity<Formula>()
            .HasOne(f => f.Establishment)
            .WithMany()
            .HasForeignKey(f => f.EstablishmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // FormulaComponent -> Formula
        modelBuilder.Entity<FormulaComponent>()
            .HasOne(fc => fc.Formula)
            .WithMany(f => f.Components)
            .HasForeignKey(fc => fc.FormulaId)
            .OnDelete(DeleteBehavior.Cascade);

        // FormulaComponent -> RawMaterial
        modelBuilder.Entity<FormulaComponent>()
            .HasOne(fc => fc.RawMaterial)
            .WithMany(r => r.FormulaComponents)
            .HasForeignKey(fc => fc.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        // Employee -> Establishment
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Establishment)
            .WithMany()
            .HasForeignKey(e => e.EstablishmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // EmployeeSession -> Employee
        modelBuilder.Entity<EmployeeSession>()
            .HasOne(es => es.Employee)
            .WithMany(e => e.Sessions)
            .HasForeignKey(es => es.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // StockMovement relationships
        modelBuilder.Entity<StockMovement>()
            .HasOne(sm => sm.Establishment)
            .WithMany()
            .HasForeignKey(sm => sm.EstablishmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMovement>()
            .HasOne(sm => sm.RawMaterial)
            .WithMany(r => r.StockMovements)
            .HasForeignKey(sm => sm.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMovement>()
            .HasOne(sm => sm.Batch)
            .WithMany(b => b.StockMovements)
            .HasForeignKey(sm => sm.BatchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMovement>()
            .HasOne(sm => sm.PerformedByEmployee)
            .WithMany(e => e.StockMovements)
            .HasForeignKey(sm => sm.PerformedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockMovement>()
            .HasOne(sm => sm.AuthorizedByEmployee)
            .WithMany()
            .HasForeignKey(sm => sm.AuthorizedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Supplier -> Establishment
        modelBuilder.Entity<Supplier>()
            .HasOne(s => s.Establishment)
            .WithMany()
            .HasForeignKey(s => s.EstablishmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sale -> Establishment
        modelBuilder.Entity<Sale>()
            .HasOne(s => s.Establishment)
            .WithMany()
            .HasForeignKey(s => s.EstablishmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sale -> Employee
        modelBuilder.Entity<Sale>()
            .HasOne(s => s.SoldByEmployee)
            .WithMany()
            .HasForeignKey(s => s.SoldByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Sale>()
            .HasOne(s => s.AuthorizedByPharmacist)
            .WithMany()
            .HasForeignKey(s => s.AuthorizedByPharmacistId)
            .OnDelete(DeleteBehavior.Restrict);

        // SaleItem -> Sale
        modelBuilder.Entity<SaleItem>()
            .HasOne(si => si.Sale)
            .WithMany(s => s.Items)
            .HasForeignKey(si => si.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        // SaleItem -> ManipulationOrder
        modelBuilder.Entity<SaleItem>()
            .HasOne(si => si.ManipulationOrder)
            .WithMany(mo => mo.SaleItems)
            .HasForeignKey(si => si.ManipulationOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // ManipulationOrder relationships
        modelBuilder.Entity<ManipulationOrder>()
            .HasOne(mo => mo.Establishment)
            .WithMany()
            .HasForeignKey(mo => mo.EstablishmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ManipulationOrder>()
            .HasOne(mo => mo.Formula)
            .WithMany(f => f.ManipulationOrders)
            .HasForeignKey(mo => mo.FormulaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ManipulationOrder>()
            .HasOne(mo => mo.RequestedByEmployee)
            .WithMany()
            .HasForeignKey(mo => mo.RequestedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ManipulationOrder>()
            .HasOne(mo => mo.ManipulatedByEmployee)
            .WithMany()
            .HasForeignKey(mo => mo.ManipulatedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ManipulationOrder>()
            .HasOne(mo => mo.CheckedByEmployee)
            .WithMany()
            .HasForeignKey(mo => mo.CheckedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ManipulationOrder>()
            .HasOne(mo => mo.ApprovedByPharmacist)
            .WithMany()
            .HasForeignKey(mo => mo.ApprovedByPharmacistId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configurações de precisão decimal
        modelBuilder.Entity<RawMaterial>()
            .Property(r => r.PurityFactor)
            .HasPrecision(10, 4);

        modelBuilder.Entity<RawMaterial>()
            .Property(r => r.EquivalenceFactor)
            .HasPrecision(10, 4);

        modelBuilder.Entity<RawMaterial>()
            .Property(r => r.CurrentStock)
            .HasPrecision(18, 4);

        modelBuilder.Entity<Batch>()
            .Property(b => b.ReceivedQuantity)
            .HasPrecision(18, 4);

        modelBuilder.Entity<StockMovement>()
            .Property(sm => sm.Quantity)
            .HasPrecision(18, 4);

        // Valores default para auditoria
        modelBuilder.Entity<RawMaterial>()
            .Property(r => r.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Employee>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Supplier>()
            .Property(s => s.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<Establishment>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Establishment>()
            .Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Configuração para AccessLevel
        modelBuilder.Entity<AccessLevel>()
            .Property(a => a.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<AccessLevel>()
            .Property(a => a.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relacionamento Establishment -> AccessLevel
        modelBuilder.Entity<Establishment>()
            .HasOne(e => e.AccessLevel)
            .WithMany()
            .HasForeignKey(e => e.AccessLevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void SeedPermissions(ModelBuilder modelBuilder)
    {
        var permissions = new List<Permission>
        {
            // Employees
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                ResourceAction = "employees.create",
                Resource = "employees",
                Action = "create",
                Category = "HR",
                DisplayName = "Criar Funcionários",
                Description = "Permite criar novos funcionários",
                Scope = "Establishment",
                RiskLevel = "High",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                ResourceAction = "employees.read",
                Resource = "employees",
                Action = "read",
                Category = "HR",
                DisplayName = "Visualizar Funcionários",
                Description = "Permite visualizar dados de funcionários",
                Scope = "Establishment",
                RiskLevel = "Low",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000003"),
                ResourceAction = "employees.update",
                Resource = "employees",
                Action = "update",
                Category = "HR",
                DisplayName = "Editar Funcionários",
                Description = "Permite editar dados de funcionários",
                Scope = "Establishment",
                RiskLevel = "Medium",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000004"),
                ResourceAction = "employees.delete",
                Resource = "employees",
                Action = "delete",
                Category = "HR",
                DisplayName = "Deletar Funcionários",
                Description = "Permite deletar funcionários",
                Scope = "Establishment",
                RiskLevel = "Critical",
                RequiresApproval = true,
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000005"),
                ResourceAction = "employees.terminate",
                Resource = "employees",
                Action = "terminate",
                Category = "HR",
                DisplayName = "Demitir Funcionários",
                Description = "Permite demitir funcionários",
                Scope = "Establishment",
                RiskLevel = "Critical",
                RequiresApproval = true,
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            // Inventory
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000101"),
                ResourceAction = "inventory.read",
                Resource = "inventory",
                Action = "read",
                Category = "Inventory",
                DisplayName = "Visualizar Estoque",
                Description = "Permite visualizar o estoque",
                Scope = "Establishment",
                RiskLevel = "Low",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000102"),
                ResourceAction = "inventory.update",
                Resource = "inventory",
                Action = "update",
                Category = "Inventory",
                DisplayName = "Atualizar Estoque",
                Description = "Permite atualizar quantidades no estoque",
                Scope = "Establishment",
                RiskLevel = "Medium",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            // Sales
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000201"),
                ResourceAction = "sales.create",
                Resource = "sales",
                Action = "create",
                Category = "Sales",
                DisplayName = "Realizar Vendas",
                Description = "Permite realizar vendas",
                Scope = "Establishment",
                RiskLevel = "Medium",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000202"),
                ResourceAction = "sales.read",
                Resource = "sales",
                Action = "read",
                Category = "Sales",
                DisplayName = "Visualizar Vendas",
                Description = "Permite visualizar vendas",
                Scope = "Own",
                RiskLevel = "Low",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            // Reports
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000301"),
                ResourceAction = "reports.read",
                Resource = "reports",
                Action = "read",
                Category = "Reports",
                DisplayName = "Visualizar Relatórios",
                Description = "Permite visualizar relatórios",
                Scope = "Establishment",
                RiskLevel = "Medium",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000302"),
                ResourceAction = "reports.export",
                Resource = "reports",
                Action = "export",
                Category = "Reports",
                DisplayName = "Exportar Relatórios",
                Description = "Permite exportar relatórios",
                Scope = "Establishment",
                RiskLevel = "High",
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            // Settings
            new Permission
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000401"),
                ResourceAction = "settings.update",
                Resource = "settings",
                Action = "update",
                Category = "Settings",
                DisplayName = "Alterar Configurações",
                Description = "Permite alterar configurações do sistema",
                Scope = "Establishment",
                RiskLevel = "Critical",
                RequiresTwoFactor = true,
                IsSystemPermission = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        modelBuilder.Entity<Permission>().HasData(permissions);
    }

    private void SeedInitialData(ModelBuilder modelBuilder)
    {
        // Seed de Níveis de Acesso padrão (se ainda não existir)
        var ownerAccessLevelId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var managerAccessLevelId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var employeeAccessLevelId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var userAccessLevelId = Guid.Parse("10000000-0000-0000-0000-000000000004");

        modelBuilder.Entity<AccessLevel>().HasData(
            new AccessLevel
            {
                Id = ownerAccessLevelId,
                Code = "owner",
                Name = "Proprietário",
                Description = "Acesso total ao sistema, incluindo configurações críticas e gestão financeira",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new AccessLevel
            {
                Id = managerAccessLevelId,
                Code = "manager",
                Name = "Gerente",
                Description = "Acesso de gerenciamento com permissões administrativas e de supervisão",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new AccessLevel
            {
                Id = employeeAccessLevelId,
                Code = "employee",
                Name = "Funcionário",
                Description = "Acesso básico para funcionários operacionais",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new AccessLevel
            {
                Id = userAccessLevelId,
                Code = "user",
                Name = "Usuário",
                Description = "Acesso limitado para usuários externos ou clientes",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed de Permissões básicas do sistema
        SeedPermissions(modelBuilder);
    }
}