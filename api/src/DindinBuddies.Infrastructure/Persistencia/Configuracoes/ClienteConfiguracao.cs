using DindinBuddies.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DindinBuddies.Infrastructure.Persistencia.Configuracoes;

public class ClienteConfiguracao : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).UseIdentityColumn();

        builder.Property(c => c.Nome).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Cpf).HasColumnType("char(11)").IsRequired();
        builder.Property(c => c.Email).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Telefone).HasColumnType("varchar(20)");
        builder.Property(c => c.DataNascimento).HasColumnType("date").IsRequired();
        builder.Property(c => c.DataCadastro)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(c => c.Cpf).IsUnique();
        builder.HasIndex(c => c.Email).IsUnique();

        builder.HasMany(c => c.Contas)
            .WithOne(c => c.Cliente)
            .HasForeignKey(c => c.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(c => c.Contas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
