import type { TipoConta } from '@/api/tipos'

const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const dataHora = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' })

export function formatarMoeda(valor: number): string {
  return moeda.format(valor)
}

export function apenasDigitos(texto: string): string {
  return texto.replace(/\D/g, '')
}

/** 12345678901 → 123.456.789-01 */
export function formatarCpf(cpf: string): string {
  return cpf.replace(/^(\d{3})(\d{3})(\d{3})(\d{2})$/, '$1.$2.$3-$4')
}

/** Data/hora UTC da API, exibida no fuso local do navegador. */
export function formatarDataHora(isoUtc: string): string {
  return dataHora.format(new Date(isoUtc))
}

/** Data sem horário ("AAAA-MM-DD"), sem conversão de fuso: 1990-05-20 → 20/05/1990. */
export function formatarData(data: string): string {
  const [ano, mes, dia] = data.split('-')
  return `${dia}/${mes}/${ano}`
}

export function formatarTipoConta(tipo: TipoConta): string {
  return tipo === 'Poupanca' ? 'Poupança' : 'Corrente'
}

/** Data local de hoje (ou deslocada em dias) no formato do <input type="date">. */
export function dataLocalIso(deslocamentoEmDias = 0): string {
  const data = new Date()
  data.setDate(data.getDate() + deslocamentoEmDias)
  const mes = String(data.getMonth() + 1).padStart(2, '0')
  const dia = String(data.getDate()).padStart(2, '0')
  return `${data.getFullYear()}-${mes}-${dia}`
}

/** Início e fim de um dia local, convertidos para UTC, para filtrar o extrato. */
export function inicioDoDiaUtc(dataLocal: string): string {
  return new Date(`${dataLocal}T00:00:00`).toISOString()
}

export function fimDoDiaUtc(dataLocal: string): string {
  return new Date(`${dataLocal}T23:59:59.999`).toISOString()
}
