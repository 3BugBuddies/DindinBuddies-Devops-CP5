using DindinBuddies.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

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
}
