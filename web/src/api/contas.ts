import { http } from './http'
import type {
  Conta,
  Extrato,
  MovimentacaoRequest,
  MovimentacaoResponse,
  TransferenciaRequest,
} from './tipos'

/** Agência única do banco; a API gera todas as contas nela. */
export const AGENCIA_PADRAO = '0001'

export async function obterConta(id: number): Promise<Conta> {
  const { data } = await http.get<Conta>(`/api/contas/${id}`)
  return data
}

export async function buscarContaPorNumero(numero: string): Promise<Conta> {
  const { data } = await http.get<Conta>('/api/contas/busca', {
    params: { agencia: AGENCIA_PADRAO, numero },
  })
  return data
}

export async function encerrarConta(id: number): Promise<Conta> {
  const { data } = await http.post<Conta>(`/api/contas/${id}/encerrar`)
  return data
}

export async function depositar(contaId: number, request: MovimentacaoRequest): Promise<MovimentacaoResponse> {
  const { data } = await http.post<MovimentacaoResponse>(`/api/contas/${contaId}/depositos`, request)
  return data
}

export async function sacar(contaId: number, request: MovimentacaoRequest): Promise<MovimentacaoResponse> {
  const { data } = await http.post<MovimentacaoResponse>(`/api/contas/${contaId}/saques`, request)
  return data
}

export async function transferir(contaId: number, request: TransferenciaRequest): Promise<MovimentacaoResponse> {
  const { data } = await http.post<MovimentacaoResponse>(`/api/contas/${contaId}/transferencias`, request)
  return data
}

/** Período em UTC (ISO 8601). */
export async function obterExtrato(contaId: number, inicio: string, fim: string): Promise<Extrato> {
  const { data } = await http.get<Extrato>(`/api/contas/${contaId}/extrato`, { params: { inicio, fim } })
  return data
}
