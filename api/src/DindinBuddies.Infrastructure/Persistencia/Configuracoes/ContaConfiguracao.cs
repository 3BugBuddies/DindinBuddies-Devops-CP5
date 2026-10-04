using DindinBuddies.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DindinBuddies.Infrastructure.Persistencia.Configuracoes;

public class ContaConfiguracao : IEntityTypeConfiguration<Conta>
{
    public void Configure(EntityTypeBuilder<Conta> builder)
    {
        builder.ToTable("Contas", t =>
            t.HasCheckConstraint("CK_Contas_Saldo", "[Saldo] >= 0"));

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).UseIdentityColumn();

        builder.Property(c => c.Agencia).HasColumnType("char(4)").IsRequired();
        builder.Property(c => c.NumeroConta).HasColumnType("varchar(10)").IsRequired();
        builder.Property(c => c.TipoConta).HasColumnType("tinyint").IsRequired();
        builder.Property(c => c.Saldo).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(c => c.DataAbertura).HasColumnType("datetime2").IsRequired();
        // Sentinel true: o EF só omite o valor (e usa o padrão do banco) quando ele já é true.
        builder.Property(c => c.Ativa).HasDefaultValue(true).HasSentinel(true);

        builder.HasIndex(c => c.ClienteId);
        builder.HasIndex(c => new { c.Agencia, c.NumeroConta }).IsUnique();
    }
}
