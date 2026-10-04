import { useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Lock } from 'lucide-react'
import { Link, useParams } from 'react-router'
import { toast } from 'sonner'
import { obterCliente } from '@/api/clientes'
import { encerrarConta, obterConta } from '@/api/contas'
import { ConfirmarAcao } from '@/components/ConfirmarAcao'
import { ErroFormulario } from '@/components/ErroFormulario'
import { Extrato } from '@/components/Extrato'
import { PainelMovimentacoes } from '@/components/PainelMovimentacoes'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { formatarMoeda, formatarTipoConta } from '@/lib/formatacao'

export function ContaPage() {
  const id = Number(useParams().id)
  const queryClient = useQueryClient()

  const conta = useQuery({ queryKey: ['conta', id], queryFn: () => obterConta(id) })
  const clienteId = conta.data?.clienteId
  const cliente = useQuery({
    queryKey: ['cliente', clienteId],
    queryFn: () => obterCliente(clienteId!),
    enabled: clienteId !== undefined,
  })

  if (conta.isPending) return <p className="text-sm text-muted-foreground">Carregando conta...</p>
  if (conta.isError) return <ErroFormulario erro={conta.error} />

  const c = conta.data

  return (
    <div className="grid gap-6">
      <Link
        to={`/clientes/${c.clienteId}`}
        className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="size-4" /> {cliente.data?.nome ?? 'Cliente'}
      </Link>

      <Card>
        <CardHeader>
          <CardDescription className="flex items-center gap-2">
            Ag. {c.agencia} · Conta {c.numeroConta} · {formatarTipoConta(c.tipoConta)}
            <Badge variant={c.ativa ? 'secondary' : 'outline'}>{c.ativa ? 'Ativa' : 'Encerrada'}</Badge>
          </CardDescription>
          <CardTitle className="text-sm font-normal text-muted-foreground">Saldo disponível</CardTitle>
          {c.ativa && (
            <CardAction>
              <ConfirmarAcao
                gatilho={
                  <Button variant="destructive">
                    <Lock /> Encerrar conta
                  </Button>
                }
                titulo="Encerrar conta?"
                descricao="A conta deixa de aceitar movimentações e o histórico é mantido. Só é possível encerrar com saldo zero."
                rotuloConfirmar="Encerrar"
                acao={() => encerrarConta(id)}
                aoConcluir={() => {
                  queryClient.invalidateQueries({ queryKey: ['conta', id] })
                  queryClient.invalidateQueries({ queryKey: ['contas-cliente', c.clienteId] })
                  toast.success('Conta encerrada.')
                }}
              />
            </CardAction>
          )}
        </CardHeader>
        <CardContent>
          <p className="text-4xl font-semibold tracking-tight">{formatarMoeda(c.saldo)}</p>
        </CardContent>
      </Card>

      <div className="grid gap-6 lg:grid-cols-[2fr_3fr]">
        <PainelMovimentacoes conta={c} />
        <Extrato contaId={id} />
      </div>
    </div>
  )
}
