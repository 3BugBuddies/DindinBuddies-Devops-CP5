import { useMutation } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'
import { ErroFormulario } from '@/components/ErroFormulario'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'

interface ConfirmarAcaoProps {
  /** Botão que abre a confirmação. */
  gatilho: ReactNode
  titulo: string
  descricao: string
  rotuloConfirmar: string
  acao: () => Promise<unknown>
  aoConcluir: () => void
}

/** Pede confirmação antes de uma ação irreversível (excluir, encerrar) e mostra o erro da API, se houver. */
export function ConfirmarAcao({ gatilho, titulo, descricao, rotuloConfirmar, acao, aoConcluir }: ConfirmarAcaoProps) {
  const [aberto, setAberto] = useState(false)
  const mutacao = useMutation({
    mutationFn: acao,
    onSuccess: () => {
      setAberto(false)
      aoConcluir()
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
      <DialogTrigger asChild>{gatilho}</DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{titulo}</DialogTitle>
          <DialogDescription>{descricao}</DialogDescription>
        </DialogHeader>
        <ErroFormulario erro={mutacao.error} />
        <DialogFooter>
          <DialogClose asChild>
            <Button variant="outline">Cancelar</Button>
          </DialogClose>
          <Button variant="destructive" disabled={mutacao.isPending} onClick={() => mutacao.mutate()}>
            {mutacao.isPending ? 'Aguarde...' : rotuloConfirmar}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
