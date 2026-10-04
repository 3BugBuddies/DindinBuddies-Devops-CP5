import { useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Pencil, Trash2 } from 'lucide-react'
import { Link, useNavigate, useParams } from 'react-router'
import { toast } from 'sonner'
import { excluirCliente, listarContasDoCliente, obterCliente } from '@/api/clientes'
import { ConfirmarAcao } from '@/components/ConfirmarAcao'
import { DialogAbrirConta } from '@/components/DialogAbrirConta'
import { DialogCliente } from '@/components/DialogCliente'
import { ErroFormulario } from '@/components/ErroFormulario'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardAction, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { formatarCpf, formatarData, formatarMoeda, formatarTipoConta } from '@/lib/formatacao'

export function ClienteDetalhePage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const cliente = useQuery({ queryKey: ['cliente', id], queryFn: () => obterCliente(id) })
  const contas = useQuery({ queryKey: ['contas-cliente', id], queryFn: () => listarContasDoCliente(id) })

  if (cliente.isPending) return <p className="text-sm text-muted-foreground">Carregando cliente...</p>
  if (cliente.isError) return <ErroFormulario erro={cliente.error} />

  const c = cliente.data

  return (
    <div className="grid gap-6">
      <Link to="/clientes" className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
        <ArrowLeft className="size-4" /> Clientes
      </Link>

      <Card>
        <CardHeader>
          <CardTitle className="text-2xl">{c.nome}</CardTitle>
          <CardAction className="flex gap-2">
            <DialogCliente
              cliente={c}
              gatilho={
                <Button variant="outline">
                  <Pencil /> Editar
                </Button>
              }
            />
            <ConfirmarAcao
              gatilho={
                <Button variant="destructive">
                  <Trash2 /> Excluir
                </Button>
              }
              titulo="Excluir cliente?"
              descricao={`O cadastro de ${c.nome} será removido. Só é possível excluir clientes sem contas.`}
              rotuloConfirmar="Excluir"
              acao={() => excluirCliente(id)}
              aoConcluir={() => {
                queryClient.invalidateQueries({ queryKey: ['clientes'] })
                toast.success('Cliente excluído.')
                navigate('/clientes')
              }}
            />
          </CardAction>
        </CardHeader>
        <CardContent>
          <dl className="grid gap-4 text-sm sm:grid-cols-2 lg:grid-cols-4">
            <Dado rotulo="CPF" valor={formatarCpf(c.cpf)} />
            <Dado rotulo="E-mail" valor={c.email} />
            <Dado rotulo="Telefone" valor={c.telefone ?? '—'} />
            <Dado rotulo="Nascimento" valor={formatarData(c.dataNascimento)} />
          </dl>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Contas</CardTitle>
          <CardAction>
            <DialogAbrirConta clienteId={id} />
          </CardAction>
        </CardHeader>
        <CardContent className="grid gap-4">
          <ErroFormulario erro={contas.error} />
          {contas.isPending && <p className="text-sm text-muted-foreground">Carregando contas...</p>}
          {contas.data?.length === 0 && (
            <p className="text-sm text-muted-foreground">Este cliente ainda não tem contas.</p>
          )}
          {contas.data && contas.data.length > 0 && (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Agência / Conta</TableHead>
                  <TableHead>Tipo</TableHead>
                  <TableHead>Situação</TableHead>
                  <TableHead className="text-right">Saldo</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {contas.data.map((conta) => (
                  <TableRow key={conta.id} className="cursor-pointer" onClick={() => navigate(`/contas/${conta.id}`)}>
                    <TableCell className="font-medium">
                      <Link to={`/contas/${conta.id}`} onClick={(e) => e.stopPropagation()}>
                        {conta.agencia} / {conta.numeroConta}
                      </Link>
                    </TableCell>
                    <TableCell>{formatarTipoConta(conta.tipoConta)}</TableCell>
                    <TableCell>
                      <Badge variant={conta.ativa ? 'secondary' : 'outline'}>{conta.ativa ? 'Ativa' : 'Encerrada'}</Badge>
                    </TableCell>
                    <TableCell className="text-right">{formatarMoeda(conta.saldo)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

function Dado({ rotulo, valor }: { rotulo: string; valor: string }) {
  return (
    <div>
      <dt className="text-muted-foreground">{rotulo}</dt>
      <dd className="font-medium">{valor}</dd>
    </div>
  )
}
