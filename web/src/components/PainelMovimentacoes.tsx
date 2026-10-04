import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { toast } from 'sonner'
import { obterCliente } from '@/api/clientes'
import { buscarContaPorNumero, depositar, sacar, transferir } from '@/api/contas'
import type { Cliente, Conta } from '@/api/tipos'
import { ErroFormulario } from '@/components/ErroFormulario'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { formatarMoeda, formatarTipoConta } from '@/lib/formatacao'

/** Depositar, sacar e transferir a partir da conta exibida. */
export function PainelMovimentacoes({ conta }: { conta: Conta }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Movimentações</CardTitle>
        {!conta.ativa && <CardDescription>Conta encerrada: não aceita novas movimentações.</CardDescription>}
      </CardHeader>
      <CardContent>
        <Tabs defaultValue="deposito">
          <TabsList className="w-full">
            <TabsTrigger value="deposito">Depositar</TabsTrigger>
            <TabsTrigger value="saque">Sacar</TabsTrigger>
            <TabsTrigger value="transferencia">Transferir</TabsTrigger>
          </TabsList>
          <TabsContent value="deposito" className="pt-4">
            <FormularioMovimentacao conta={conta} tipo="deposito" />
          </TabsContent>
          <TabsContent value="saque" className="pt-4">
            <FormularioMovimentacao conta={conta} tipo="saque" />
          </TabsContent>
          <TabsContent value="transferencia" className="pt-4">
            <FormularioTransferencia conta={conta} />
          </TabsContent>
        </Tabs>
      </CardContent>
    </Card>
  )
}

/** Atualiza saldo e extrato das contas envolvidas depois de uma movimentação. */
function useAtualizarContas() {
  const queryClient = useQueryClient()
  return (...contaIds: number[]) => {
    for (const id of contaIds) {
      queryClient.invalidateQueries({ queryKey: ['conta', id] })
      queryClient.invalidateQueries({ queryKey: ['extrato', id] })
    }
    queryClient.invalidateQueries({ queryKey: ['contas-cliente'] })
  }
}

function lerValorEDescricao(form: FormData) {
  return {
    valor: Number(form.get('valor')),
    descricao: String(form.get('descricao') ?? '').trim() || null,
  }
}

function CamposValorEDescricao({ prefixo, desabilitado }: { prefixo: string; desabilitado: boolean }) {
  return (
    <>
      <div className="grid gap-2">
        <Label htmlFor={`${prefixo}-valor`}>Valor (R$)</Label>
        <Input
          id={`${prefixo}-valor`}
          name="valor"
          type="number"
          inputMode="decimal"
          min="0.01"
          step="0.01"
          required
          disabled={desabilitado}
        />
      </div>
      <div className="grid gap-2">
        <Label htmlFor={`${prefixo}-descricao`}>Descrição (opcional)</Label>
        <Input id={`${prefixo}-descricao`} name="descricao" maxLength={200} disabled={desabilitado} />
      </div>
    </>
  )
}

function FormularioMovimentacao({ conta, tipo }: { conta: Conta; tipo: 'deposito' | 'saque' }) {
  const atualizarContas = useAtualizarContas()
  const mutacao = useMutation({
    mutationFn: ({ form }: { form: HTMLFormElement }) => {
      const request = lerValorEDescricao(new FormData(form))
      return tipo === 'deposito' ? depositar(conta.id, request) : sacar(conta.id, request)
    },
    onSuccess: (resultado, { form }) => {
      form.reset()
      atualizarContas(conta.id)
      toast.success(
        `${tipo === 'deposito' ? 'Depósito' : 'Saque'} de ${formatarMoeda(resultado.valor)} realizado. ` +
          `Saldo: ${formatarMoeda(resultado.saldoAtual)}.`,
      )
    },
  })

  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault()
    mutacao.mutate({ form: evento.currentTarget })
  }

  return (
    <form onSubmit={enviar} className="grid gap-4">
      <CamposValorEDescricao prefixo={tipo} desabilitado={!conta.ativa} />
      <ErroFormulario erro={mutacao.error} />
      <Button type="submit" disabled={!conta.ativa || mutacao.isPending}>
        {mutacao.isPending ? 'Processando...' : tipo === 'deposito' ? 'Depositar' : 'Sacar'}
      </Button>
    </form>
  )
}

interface TransferenciaPendente {
  destino: Conta
  titular: Cliente
  valor: number
  descricao: string | null
}

/**
 * Transferência em dois passos: o usuário informa o número da conta de destino,
 * confere a conta encontrada e só então confirma.
 */
function FormularioTransferencia({ conta }: { conta: Conta }) {
  const atualizarContas = useAtualizarContas()
  const [pendente, setPendente] = useState<TransferenciaPendente | null>(null)

  const busca = useMutation({
    mutationFn: async (form: FormData): Promise<TransferenciaPendente> => {
      const destino = await buscarContaPorNumero(String(form.get('numeroDestino')).trim())
      const titular = await obterCliente(destino.clienteId)
      return { destino, titular, ...lerValorEDescricao(form) }
    },
    onSuccess: setPendente,
  })

  const transferencia = useMutation({
    mutationFn: (p: TransferenciaPendente) =>
      transferir(conta.id, { contaDestinoId: p.destino.id, valor: p.valor, descricao: p.descricao }),
    onSuccess: (resultado, p) => {
      setPendente(null)
      atualizarContas(conta.id, p.destino.id)
      toast.success(
        `Transferência de ${formatarMoeda(resultado.valor)} para ${p.titular.nome} realizada. ` +
          `Saldo: ${formatarMoeda(resultado.saldoAtual)}.`,
      )
    },
  })

  function buscarDestino(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault()
    busca.mutate(new FormData(evento.currentTarget))
  }

  if (pendente) {
    const { destino, titular, valor, descricao } = pendente
    return (
      <div className="grid gap-4">
        <div className="rounded-lg border p-4 text-sm">
          <p className="text-muted-foreground">Confira os dados antes de confirmar:</p>
          <dl className="mt-2 grid grid-cols-[auto_1fr] gap-x-4 gap-y-1">
            <dt className="text-muted-foreground">Destino</dt>
            <dd>
              Ag. {destino.agencia} · Conta {destino.numeroConta} ({formatarTipoConta(destino.tipoConta)})
            </dd>
            <dt className="text-muted-foreground">Titular</dt>
            <dd>{titular.nome}</dd>
            <dt className="text-muted-foreground">Valor</dt>
            <dd className="font-medium">{formatarMoeda(valor)}</dd>
            {descricao && (
              <>
                <dt className="text-muted-foreground">Descrição</dt>
                <dd>{descricao}</dd>
              </>
            )}
          </dl>
        </div>
        <ErroFormulario erro={transferencia.error} />
        <div className="grid grid-cols-2 gap-2">
          <Button
            variant="outline"
            disabled={transferencia.isPending}
            onClick={() => {
              setPendente(null)
              transferencia.reset()
            }}
          >
            Voltar
          </Button>
          <Button disabled={transferencia.isPending} onClick={() => transferencia.mutate(pendente)}>
            {transferencia.isPending ? 'Transferindo...' : 'Confirmar'}
          </Button>
        </div>
      </div>
    )
  }

  return (
    <form onSubmit={buscarDestino} className="grid gap-4">
      <div className="grid gap-2">
        <Label htmlFor="numeroDestino">Número da conta de destino</Label>
        <Input
          id="numeroDestino"
          name="numeroDestino"
          inputMode="numeric"
          placeholder="000042"
          pattern="\d{1,6}"
          title="Informe o número da conta, com até 6 dígitos."
          required
          disabled={!conta.ativa}
        />
      </div>
      <CamposValorEDescricao prefixo="transferencia" desabilitado={!conta.ativa} />
      <ErroFormulario erro={busca.error} />
      <Button type="submit" disabled={!conta.ativa || busca.isPending}>
        {busca.isPending ? 'Buscando conta...' : 'Continuar'}
      </Button>
    </form>
  )
}
