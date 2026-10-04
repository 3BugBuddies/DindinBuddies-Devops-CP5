import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { abrirConta } from '@/api/clientes'
import type { TipoConta } from '@/api/tipos'
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

export function DialogAbrirConta({ clienteId }: { clienteId: number }) {
  const [aberto, setAberto] = useState(false)
  const [tipo, setTipo] = useState<TipoConta>('Corrente')
  const queryClient = useQueryClient()

  const mutacao = useMutation({
    mutationFn: () => abrirConta(clienteId, tipo),
    onSuccess: (conta) => {
      queryClient.invalidateQueries({ queryKey: ['contas-cliente', clienteId] })
      toast.success(`Conta ${conta.agencia}/${conta.numeroConta} aberta.`)
      setAberto(false)
    },
  })

  return (
    <Dialog
      open={aberto}
      onOpenChange={(valor) => {
        setAberto(valor)
        if (!valor) mutacao.reset()
      }}
    >
      <DialogTrigger asChild>
        <Button>
          <Plus /> Abrir conta
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Abrir conta</DialogTitle>
          <DialogDescription>A agência e o número da conta são gerados automaticamente.</DialogDescription>
        </DialogHeader>
        <div className="grid gap-2">
          <Label htmlFor="tipoConta">Tipo de conta</Label>
          <Select value={tipo} onValueChange={(valor) => setTipo(valor as TipoConta)}>
            <SelectTrigger id="tipoConta" className="w-full">
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
          <Button disabled={mutacao.isPending} onClick={() => mutacao.mutate()}>
            {mutacao.isPending ? 'Abrindo...' : 'Abrir conta'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
