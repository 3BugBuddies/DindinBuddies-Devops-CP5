import { useQuery } from '@tanstack/react-query'
import { Pencil, Plus, Search } from 'lucide-react'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { listarClientes } from '@/api/clientes'
import { DialogCliente } from '@/components/DialogCliente'
import { ErroFormulario } from '@/components/ErroFormulario'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { useValorAtrasado } from '@/hooks/useValorAtrasado'
import { formatarCpf } from '@/lib/formatacao'

export function ClientesPage() {
  const navigate = useNavigate()
  const [busca, setBusca] = useState('')
  const buscaAtrasada = useValorAtrasado(busca.trim())

  const clientes = useQuery({
    queryKey: ['clientes', buscaAtrasada],
    queryFn: () => listarClientes(buscaAtrasada),
  })

  return (
    <div className="grid gap-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <h1 className="text-2xl font-semibold">Clientes</h1>
        <DialogCliente
          gatilho={
            <Button>
              <Plus /> Novo cliente
            </Button>
          }
        />
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="sr-only">Lista de clientes</CardTitle>
          <div className="relative max-w-sm">
            <Search className="absolute top-1/2 left-2.5 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              aria-label="Buscar por nome ou CPF"
              placeholder="Buscar por nome ou CPF"
              className="pl-8"
              value={busca}
              onChange={(e) => setBusca(e.target.value)}
            />
          </div>
        </CardHeader>
        <CardContent className="grid gap-4">
          <ErroFormulario erro={clientes.error} />

          {clientes.isPending && <p className="text-sm text-muted-foreground">Carregando clientes...</p>}

          {clientes.data?.length === 0 && (
            <p className="text-sm text-muted-foreground">
              {buscaAtrasada ? 'Nenhum cliente encontrado para a busca.' : 'Nenhum cliente cadastrado ainda.'}
            </p>
          )}

          {clientes.data && clientes.data.length > 0 && (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nome</TableHead>
                  <TableHead>CPF</TableHead>
                  <TableHead className="hidden md:table-cell">E-mail</TableHead>
                  <TableHead className="hidden md:table-cell">Telefone</TableHead>
                  <TableHead className="w-12">
                    <span className="sr-only">Ações</span>
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {clientes.data.map((cliente) => (
                  <TableRow
                    key={cliente.id}
                    className="cursor-pointer"
                    onClick={() => navigate(`/clientes/${cliente.id}`)}
                  >
                    <TableCell className="font-medium">
                      <Link to={`/clientes/${cliente.id}`} onClick={(e) => e.stopPropagation()}>
                        {cliente.nome}
                      </Link>
                    </TableCell>
                    <TableCell>{formatarCpf(cliente.cpf)}</TableCell>
                    <TableCell className="hidden md:table-cell">{cliente.email}</TableCell>
                    <TableCell className="hidden md:table-cell">{cliente.telefone ?? '—'}</TableCell>
                    <TableCell onClick={(e) => e.stopPropagation()}>
                      <DialogCliente
                        cliente={cliente}
                        gatilho={
                          <Button variant="ghost" size="icon" aria-label={`Editar ${cliente.nome}`}>
                            <Pencil />
                          </Button>
                        }
                      />
                    </TableCell>
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
