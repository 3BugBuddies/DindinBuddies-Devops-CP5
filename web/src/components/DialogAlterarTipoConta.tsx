import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Pencil } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { alterarConta } from '@/api/contas'
import type { Conta, TipoConta } from '@/api/tipos'
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
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { formatarTipoConta } from '@/lib/formatacao'

export function DialogAlterarTipoConta({ conta }: { conta: Conta }) {
  const [aberto, setAberto] = useState(false)
  const [tipo, setTipo] = useState<TipoConta>(conta.tipoConta)
  const queryClient = useQueryClient()

  const mutacao = useMutation({
    mutationFn: () => alterarConta(conta.id, tipo),
    onSuccess: (atualizada) => {
      queryClient.setQueryData(['conta', conta.id], atualizada)
      queryClient.invalidateQueries({ queryKey: ['contas-cliente', conta.clienteId] })
      toast.success(`Conta alterada para ${formatarTipoConta(atualizada.tipoConta)}.`)
      setAberto(false)
    },
  })

  return (
    <Dialog
      open={aberto}
      onOpenChange={(valor) => {
        setAberto(valor)
        setTipo(conta.tipoConta)
        if (!valor) mutacao.reset()
      }}
    >
      <DialogTrigger asChild>
        <Button variant="outline">
          <Pencil /> Alterar tipo
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Alterar tipo da conta</DialogTitle>
          <DialogDescription>Agência, número e saldo continuam os mesmos.</DialogDescription>
        </DialogHeader>
        <div className="grid gap-2">
          <Label htmlFor="novoTipoConta">Tipo de conta</Label>
          <Select value={tipo} onValueChange={(valor) => setTipo(valor as TipoConta)}>
            <SelectTrigger id="novoTipoConta" className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Corrente">Corrente</SelectItem>
              <SelectItem value="Poupanca">Poupança</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <ErroFormulario erro={mutacao.error} />
        <DialogFooter>
          <Button disabled={mutacao.isPending || tipo === conta.tipoConta} onClick={() => mutacao.mutate()}>
            {mutacao.isPending ? 'Salvando...' : 'Salvar'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
