import { http } from './http'
import type { Cliente, Conta, CriarClienteRequest, EditarClienteRequest, TipoConta } from './tipos'

export async function listarClientes(busca: string): Promise<Cliente[]> {
  const { data } = await http.get<Cliente[]>('/api/clientes', { params: busca ? { busca } : undefined })
  return data
}

export async function obterCliente(id: number): Promise<Cliente> {
  const { data } = await http.get<Cliente>(`/api/clientes/${id}`)
  return data
}

export async function criarCliente(request: CriarClienteRequest): Promise<Cliente> {
  const { data } = await http.post<Cliente>('/api/clientes', request)
  return data
}

export async function editarCliente(id: number, request: EditarClienteRequest): Promise<Cliente> {
  const { data } = await http.put<Cliente>(`/api/clientes/${id}`, request)
  return data
}

export async function excluirCliente(id: number): Promise<void> {
  await http.delete(`/api/clientes/${id}`)
}

export async function listarContasDoCliente(id: number): Promise<Conta[]> {
  const { data } = await http.get<Conta[]>(`/api/clientes/${id}/contas`)
  return data
}

export async function abrirConta(clienteId: number, tipoConta: TipoConta): Promise<Conta> {
  const { data } = await http.post<Conta>(`/api/clientes/${clienteId}/contas`, { tipoConta })
  return data
}
