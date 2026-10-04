import { Navigate, Route, Routes } from 'react-router'
import { Layout } from '@/components/Layout'
import { ClienteDetalhePage } from '@/pages/ClienteDetalhePage'
import { ClientesPage } from '@/pages/ClientesPage'
import { ContaPage } from '@/pages/ContaPage'
import { NaoEncontradoPage } from '@/pages/NaoEncontradoPage'

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<Navigate to="/clientes" replace />} />
        <Route path="clientes" element={<ClientesPage />} />
        <Route path="clientes/:id" element={<ClienteDetalhePage />} />
        <Route path="contas/:id" element={<ContaPage />} />
        <Route path="*" element={<NaoEncontradoPage />} />
      </Route>
    </Routes>
  )
}
