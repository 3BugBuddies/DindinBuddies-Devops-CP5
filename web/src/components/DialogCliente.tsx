import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent, type ReactNode } from 'react'
import { toast } from 'sonner'
import { criarCliente, editarCliente } from '@/api/clientes'
import type { Cliente } from '@/api/tipos'
import { ErroFormulario } from '@/components/ErroFormulario'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { apenasDigitos, formatarCpf } from '@/lib/formatacao'

interface DialogClienteProps {
  gatilho: ReactNode
  /** Sem cliente: cadastro. Com cliente: edição (o CPF não pode mudar). */
  cliente?: Cliente
}

export function DialogCliente({ gatilho, cliente }: DialogClienteProps) {
  const [aberto, setAberto] = useState(false)
  const queryClient = useQueryClient()
  const edicao = cliente !== undefined

  const mutacao = useMutation({
    mutationFn: (form: FormData) => {
      const dados = {
        nome: String(form.get('nome')),
        email: String(form.get('email')),
        telefone: String(form.get('telefone') ?? '') || null,
        dataNascimento: String(form.get('dataNascimento')),
      }
      return edicao
        ? editarCliente(cliente.id, dados)
        : criarCliente({ ...dados, cpf: apenasDigitos(String(form.get('cpf'))) })
    },
    onSuccess: (salvo) => {
      queryClient.invalidateQueries({ queryKey: ['clientes'] })
      queryClient.setQueryData(['cliente', salvo.id], salvo)
      toast.success(edicao ? 'Cliente atualizado.' : 'Cliente cadastrado.')
      setAberto(false)
    },
  })

  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault()
    mutacao.mutate(new FormData(evento.currentTarget))
  }

  return (
    <Dialog
      open={aberto}
      onOpenChange={(valor) => {
        setAberto(valor)
        if (!valor) mutacao.reset()
      }}
    >
      <DialogTrigger asChild>{gatilho}</DialogTrigger>
      <DialogContent className="max-h-[90svh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{edicao ? 'Editar cliente' : 'Novo cliente'}</DialogTitle>
          <DialogDescription>
            {edicao ? 'Altere os dados do cliente. O CPF não pode ser alterado.' : 'Preencha os dados do novo cliente.'}
          </DialogDescription>
        </DialogHeader>

        <form id="form-cliente" onSubmit={enviar} className="grid gap-4">
          <div className="grid gap-2">
            <Label htmlFor="nome">Nome</Label>
            <Input id="nome" name="nome" required maxLength={150} defaultValue={cliente?.nome} />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="cpf">CPF</Label>
            <Input
              id="cpf"
              name="cpf"
              required={!edicao}
              disabled={edicao}
              inputMode="numeric"
              placeholder="000.000.000-00"
              // Aceita com ou sem máscara; só os dígitos são enviados.
              pattern="\d{3}\.?\d{3}\.?\d{3}-?\d{2}"
              title="Informe os 11 dígitos do CPF."
              defaultValue={cliente ? formatarCpf(cliente.cpf) : undefined}
            />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="email">E-mail</Label>
            <Input id="email" name="email" type="email" required maxLength={150} defaultValue={cliente?.email} />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label htmlFor="telefone">Telefone (opcional)</Label>
              <Input id="telefone" name="telefone" type="tel" maxLength={20} defaultValue={cliente?.telefone ?? ''} />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="dataNascimento">Data de nascimento</Label>
              <Input
                id="dataNascimento"
                name="dataNascimento"
                type="date"
                required
                defaultValue={cliente?.dataNascimento}
              />
            </div>
          </div>
          <ErroFormulario erro={mutacao.error} />
        </form>

        <DialogFooter>
          <Button type="submit" form="form-cliente" disabled={mutacao.isPending}>
            {mutacao.isPending ? 'Salvando...' : 'Salvar'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
