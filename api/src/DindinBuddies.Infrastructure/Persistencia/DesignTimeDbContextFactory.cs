using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DindinBuddies.Infrastructure.Persistencia;

/// <summary>
/// Usada só pelo `dotnet ef` para gerar migrations sem depender da Api.
/// Para aplicar migrations em um banco, passe a connection string com
/// `dotnet ef database update --connection "..."`.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DindinBuddiesDbContext>
{
    private const string ConnectionStringExemplo =
        "Server=localhost,1433;Database=DindinBuddies;Integrated Security=True;TrustServerCertificate=True";

    public DindinBuddiesDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DindinBuddiesDbContext>()
            .UseSqlServer(ConnectionStringExemplo)
            .Options;

        return new DindinBuddiesDbContext(options);
    }
}
