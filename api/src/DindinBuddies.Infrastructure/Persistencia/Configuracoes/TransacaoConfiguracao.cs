using DindinBuddies.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DindinBuddies.Infrastructure.Persistencia.Configuracoes;

public class TransacaoConfiguracao : IEntityTypeConfiguration<Transacao>
{
    public void Configure(EntityTypeBuilder<Transacao> builder)
    {
        builder.ToTable("Transacoes", t =>
        {
            t.HasCheckConstraint("CK_Transacoes_Valor", "[Valor] > 0");
            t.HasCheckConstraint("CK_Transacoes_ContaDestino", "[ContaDestinoId] IS NULL OR [ContaDestinoId] <> [ContaId]");
        });

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).UseIdentityColumn();

        builder.Property(t => t.Tipo).HasColumnType("tinyint").IsRequired();
        builder.Property(t => t.Valor).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(t => t.DataHora).HasColumnType("datetime2").IsRequired();
        builder.Property(t => t.Descricao).HasMaxLength(200);

        builder.HasOne(t => t.Conta)
            .WithMany()
            .HasForeignKey(t => t.ContaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ContaDestino)
            .WithMany()
            .HasForeignKey(t => t.ContaDestinoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Extrato: a conta aparece como origem (ContaId) ou como destino de transferência (ContaDestinoId).
        builder.HasIndex(t => new { t.ContaId, t.DataHora });
        builder.HasIndex(t => new { t.ContaDestinoId, t.DataHora });
    }
}
