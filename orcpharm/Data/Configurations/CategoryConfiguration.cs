using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Models.Core;

namespace Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // Tabela
        builder.ToTable("Categories");

        // Chave primária
        builder.HasKey(c => c.Id);

        // Propriedades
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(c => c.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Índices
        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("IX_Categories_Name");

        // Relacionamentos
        builder.HasMany(c => c.Establishments)
            .WithOne(e => e.Category)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Establishments_Categories_CategoryId");

        // Seed Data (dados iniciais)
        builder.HasData(
            new Category
            {
                Id = Guid.Parse("c0000000-0000-0000-0000-000000000001"),
                Name = "Farmácia de Manipulação",
                Description = "Estabelecimento autorizado pela ANVISA para manipulação magistral e oficinal conforme RDC 67/2007",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = Guid.Parse("c0000000-0000-0000-0000-000000000002"),
                Name = "Drogaria",
                Description = "Estabelecimento comercial de dispensação e comércio de medicamentos industrializados",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = Guid.Parse("c0000000-0000-0000-0000-000000000003"),
                Name = "Farmácia com Manipulação",
                Description = "Estabelecimento que combina dispensação de medicamentos industrializados e manipulação magistral",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = Guid.Parse("c0000000-0000-0000-0000-000000000004"),
                Name = "Farmácia Hospitalar",
                Description = "Unidade clínica, administrativa e técnica responsável pela assistência farmacêutica em ambiente hospitalar",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = Guid.Parse("c0000000-0000-0000-0000-000000000005"),
                Name = "Distribuidora",
                Description = "Empresa autorizada para armazenamento e distribuição de medicamentos e insumos farmacêuticos",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = Guid.Parse("c0000000-0000-0000-0000-000000000006"),
                Name = "Farmácia Homeopática",
                Description = "Estabelecimento especializado em manipulação e dispensação de medicamentos homeopáticos",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = Guid.Parse("c0000000-0000-0000-0000-000000000007"),
                Name = "Laboratório de Análises",
                Description = "Estabelecimento destinado à realização de análises clínicas e controle de qualidade",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = Guid.Parse("c0000000-0000-0000-0000-000000000008"),
                Name = "Posto de Medicamentos",
                Description = "Unidade destinada exclusivamente à dispensação de medicamentos industrializados",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = Guid.Parse("c0000000-0000-0000-0000-000000000009"),
                Name = "Ervanária",
                Description = "Estabelecimento de dispensação de plantas medicinais e fitoterápicos",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
    }
}
