import { Link } from 'react-router'
import { Button } from '@/components/ui/button'

export function NaoEncontradoPage() {
  return (
    <div className="grid justify-items-center gap-4 py-16 text-center">
      <h1 className="text-2xl font-semibold">Página não encontrada</h1>
      <Button asChild variant="outline">
        <Link to="/clientes">Voltar para clientes</Link>
      </Button>
    </div>
  )
}
