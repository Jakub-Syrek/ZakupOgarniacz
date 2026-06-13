using Microsoft.EntityFrameworkCore;

namespace ZakupOgarniacz.Infrastructure.Orders;

/// <summary>
/// Kontekst EF Core dla koszyków. Koszyk to agregat — zapisujemy go jako dokument
/// (Id + JSON), żeby nie obciążać modelu domenowego mapowaniem relacyjnym.
/// </summary>
public sealed class CartDbContext : DbContext
{
    public CartDbContext(DbContextOptions<CartDbContext> options) : base(options)
    {
    }

    public DbSet<CartDocument> Carts => Set<CartDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CartDocument>(entity =>
        {
            entity.ToTable("Carts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Json).IsRequired();
        });
    }
}

/// <summary>Wiersz tabeli koszyków: id + zserializowany snapshot.</summary>
public sealed class CartDocument
{
    public string Id { get; set; } = string.Empty;

    public string Json { get; set; } = string.Empty;
}
