using DindinBuddies.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DindinBuddies.Infrastructure.Persistencia;

public class DindinBuddiesDbContext(DbContextOptions<DindinBuddiesDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<Transacao> Transacoes => Set<Transacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DindinBuddiesDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // O datetime2 não guarda o fuso. Todas as datas são gravadas em UTC, então ao ler
        // marcamos como UTC; assim o JSON sai com "Z" e o front converte para o horário local.
        configurationBuilder.Properties<DateTime>().HaveConversion<DateTimeUtcConverter>();
    }

    private sealed class DateTimeUtcConverter() : ValueConverter<DateTime, DateTime>(
        valor => valor.Kind == DateTimeKind.Local ? valor.ToUniversalTime() : valor,
        valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));
}
