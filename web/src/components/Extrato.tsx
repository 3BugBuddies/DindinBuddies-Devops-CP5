import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Undo2 } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { toast } from 'sonner'
import { excluirTransacao, obterExtrato } from '@/api/contas'
import type { ExtratoItem } from '@/api/tipos'
import { ConfirmarAcao } from '@/components/ConfirmarAcao'
import { DialogEditarDescricao } from '@/components/DialogEditarDescricao'
import { ErroFormulario } from '@/components/ErroFormulario'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { cn } from '@/lib/utils'
import { dataLocalIso, fimDoDiaUtc, formatarDataHora, formatarMoeda, inicioDoDiaUtc } from '@/lib/formatacao'

const DIAS_PADRAO = 30

function descreverTipo(item: ExtratoItem): string {
  switch (item.tipo) {
    case 'Deposito':
      return 'Depósito'
    case 'Saque':
      return 'Saque'
    case 'Transferencia':
      return item.sentido === 'Entrada' ? 'Transferência recebida' : 'Transferência enviada'
  }
}

/** Extrato por período (datas locais, convertidas para UTC na consulta). */
export function Extrato({ contaId }: { contaId: number }) {
  const queryClient = useQueryClient()
  const [periodo, setPeriodo] = useState({ de: dataLocalIso(-DIAS_PADRAO), ate: dataLocalIso() })

  const extrato = useQuery({
    queryKey: ['extrato', contaId, periodo.de, periodo.ate],
    queryFn: () => obterExtrato(contaId, inicioDoDiaUtc(periodo.de), fimDoDiaUtc(periodo.ate)),
  })

  function filtrar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault()
    const form = new FormData(evento.currentTarget)
    setPeriodo({ de: String(form.get('de')), ate: String(form.get('ate')) })
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Extrato</CardTitle>
      </CardHeader>
      <CardContent className="grid gap-4">
        <form onSubmit={filtrar} className="flex flex-wrap items-end gap-3">
          <div className="grid gap-2">
            <Label htmlFor="de">De</Label>
            <Input id="de" name="de" type="date" required defaultValue={periodo.de} />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="ate">Até</Label>
            <Input id="ate" name="ate" type="date" required defaultValue={periodo.ate} />
          </div>
          <Button type="submit" variant="outline">
            Filtrar
          </Button>
        </form>

        <ErroFormulario erro={extrato.error} />

        {extrato.isPending && <p className="text-sm text-muted-foreground">Carregando extrato...</p>}

        {extrato.data && extrato.data.itens.length === 0 && (
          <p className="text-sm text-muted-foreground">Nenhuma movimentação no período.</p>
        )}

        {extrato.data && extrato.data.itens.length > 0 && (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Data</TableHead>
                <TableHead>Movimentação</TableHead>
                <TableHead className="text-right">Valor</TableHead>
                <TableHead className="w-20">
                  <span className="sr-only">Ações</span>
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {extrato.data.itens.map((item) => {
                const entrada = item.sentido === 'Entrada'
                return (
                  <TableRow key={item.transacaoId}>
                    <TableCell className="whitespace-nowrap">{formatarDataHora(item.dataHora)}</TableCell>
                    <TableCell>
                      <div>{descreverTipo(item)}</div>
                      {item.descricao && <div className="text-xs text-muted-foreground">{item.descricao}</div>}
                    </TableCell>
                    <TableCell
                      className={cn(
                        'text-right font-medium whitespace-nowrap',
                        entrada ? 'text-green-600 dark:text-green-500' : 'text-red-600 dark:text-red-500',
                      )}
                    >
                      {entrada ? '+ ' : '− '}
                      {formatarMoeda(item.valor)}
                    </TableCell>
                    <TableCell>
                      <div className="flex justify-end">
                        <DialogEditarDescricao item={item} />
                        <ConfirmarAcao
                          gatilho={
                            <Button variant="ghost" size="icon" aria-label="Estornar transação">
                              <Undo2 />
                            </Button>
                          }
                          titulo="Estornar transação?"
                          descricao={`${descreverTipo(item)} de ${formatarMoeda(item.valor)}. O saldo das contas envolvidas volta ao que era antes, e a transação é excluída.`}
                          rotuloConfirmar="Estornar"
                          acao={() => excluirTransacao(item.transacaoId)}
                          aoConcluir={() => {
                            // O estorno pode mudar o saldo de duas contas (transferência).
                            queryClient.invalidateQueries({ queryKey: ['conta'] })
                            queryClient.invalidateQueries({ queryKey: ['extrato'] })
                            queryClient.invalidateQueries({ queryKey: ['contas-cliente'] })
                            toast.success('Transação estornada.')
                          }}
                        />
                      </div>
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
        )}
      </CardContent>
    </Card>
  )
}
