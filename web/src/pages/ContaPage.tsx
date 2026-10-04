import { useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Lock, Trash2 } from 'lucide-react'
import { Link, useNavigate, useParams } from 'react-router'
import { toast } from 'sonner'
import { obterCliente } from '@/api/clientes'
import { encerrarConta, excluirConta, obterConta } from '@/api/contas'
import { ConfirmarAcao } from '@/components/ConfirmarAcao'
import { DialogAlterarTipoConta } from '@/components/DialogAlterarTipoConta'
import { ErroFormulario } from '@/components/ErroFormulario'
import { Extrato } from '@/components/Extrato'
import { PainelMovimentacoes } from '@/components/PainelMovimentacoes'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { formatarMoeda, formatarTipoConta } from '@/lib/formatacao'

export function ContaPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
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
          <CardAction className="flex flex-wrap justify-end gap-2">
            {c.ativa && (
              <>
                <DialogAlterarTipoConta conta={c} />
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
              </>
            )}
            <ConfirmarAcao
              gatilho={
                <Button variant="destructive">
                  <Trash2 /> Excluir conta
                </Button>
              }
              titulo="Excluir conta?"
              descricao="A conta será removida. Só é possível excluir contas sem movimentações; contas com histórico devem ser encerradas."
              rotuloConfirmar="Excluir"
              acao={() => excluirConta(id)}
              aoConcluir={() => {
                queryClient.removeQueries({ queryKey: ['conta', id] })
                queryClient.invalidateQueries({ queryKey: ['contas-cliente', c.clienteId] })
                toast.success('Conta excluída.')
                navigate(`/clientes/${c.clienteId}`)
              }}
            />
          </CardAction>
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
