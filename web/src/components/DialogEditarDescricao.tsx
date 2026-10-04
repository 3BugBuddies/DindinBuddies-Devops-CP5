import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Pencil } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { toast } from 'sonner'
import { editarTransacao } from '@/api/contas'
import type { ExtratoItem } from '@/api/tipos'
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

/** Edita a descrição de uma transação do extrato (o único dado editável). */
export function DialogEditarDescricao({ item }: { item: ExtratoItem }) {
  const [aberto, setAberto] = useState(false)
  const queryClient = useQueryClient()

  const mutacao = useMutation({
    mutationFn: (descricao: string | null) => editarTransacao(item.transacaoId, descricao),
    onSuccess: () => {
      // A transação aparece no extrato da origem e, se for transferência, no do destino.
      queryClient.invalidateQueries({ queryKey: ['extrato'] })
      toast.success('Descrição atualizada.')
      setAberto(false)
    },
  })

  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault()
    const descricao = String(new FormData(evento.currentTarget).get('descricao') ?? '').trim()
    mutacao.mutate(descricao || null)
  }

  return (
    <Dialog
      open={aberto}
      onOpenChange={(valor) => {
        setAberto(valor)
        if (!valor) mutacao.reset()
      }}
    >
      <DialogTrigger asChild>
        <Button variant="ghost" size="icon" aria-label="Editar descrição">
          <Pencil />
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Editar descrição</DialogTitle>
          <DialogDescription>Só a descrição pode ser alterada; valor e contas continuam os mesmos.</DialogDescription>
        </DialogHeader>
        <form id={`form-descricao-${item.transacaoId}`} onSubmit={enviar} className="grid gap-2">
          <Label htmlFor={`descricao-${item.transacaoId}`}>Descrição</Label>
          <Input
            id={`descricao-${item.transacaoId}`}
            name="descricao"
            maxLength={200}
            defaultValue={item.descricao ?? ''}
            placeholder="Deixe em branco para remover"
          />
          <ErroFormulario erro={mutacao.error} />
        </form>
        <DialogFooter>
          <Button type="submit" form={`form-descricao-${item.transacaoId}`} disabled={mutacao.isPending}>
            {mutacao.isPending ? 'Salvando...' : 'Salvar'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
