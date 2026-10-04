import { CircleAlert } from 'lucide-react'
import { lerErro } from '@/api/erros'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'

/** Mostra o erro de uma chamada à API dentro do formulário. */
export function ErroFormulario({ erro }: { erro: unknown }) {
  if (!erro) return null
  const { mensagem, campos } = lerErro(erro)

  return (
    <Alert variant="destructive">
      <CircleAlert />
      <AlertTitle>{mensagem}</AlertTitle>
      {campos.length > 0 && (
        <AlertDescription>
          <ul className="list-disc pl-4">
            {campos.map((campo) => (
              <li key={campo}>{campo}</li>
            ))}
          </ul>
        </AlertDescription>
      )}
    </Alert>
  )
}
