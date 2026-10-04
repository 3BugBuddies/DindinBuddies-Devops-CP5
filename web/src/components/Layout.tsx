import { Wallet } from 'lucide-react'
import { Link, Outlet } from 'react-router'
import { Toaster } from '@/components/ui/sonner'

export function Layout() {
  return (
    <div className="min-h-svh bg-muted/40">
      <header className="border-b bg-background">
        <div className="mx-auto flex h-14 max-w-5xl items-center gap-6 px-4">
          <Link to="/clientes" className="flex items-center gap-2 font-semibold">
            <Wallet className="size-5 text-primary" />
            DindinBuddies
          </Link>
          <nav className="text-sm">
            <Link to="/clientes" className="text-muted-foreground hover:text-foreground">
              Clientes
            </Link>
          </nav>
        </div>
      </header>
      <main className="mx-auto max-w-5xl px-4 py-8">
        <Outlet />
      </main>
      <Toaster richColors position="top-right" />
    </div>
  )
}
