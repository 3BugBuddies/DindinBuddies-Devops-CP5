// Espelham os DTOs da API. Datas chegam como texto ISO (DateTime em UTC, DateOnly como "AAAA-MM-DD").

export type TipoConta = 'Corrente' | 'Poupanca'
export type TipoTransacao = 'Deposito' | 'Saque' | 'Transferencia'
export type SentidoMovimentacao = 'Entrada' | 'Saida'

export interface Cliente {
  id: number
  nome: string
  cpf: string
  email: string
  telefone: string | null
  dataNascimento: string
  dataCadastro: string
}

export interface CriarClienteRequest {
  nome: string
  cpf: string
  email: string
  telefone: string | null
  dataNascimento: string
}

export type EditarClienteRequest = Omit<CriarClienteRequest, 'cpf'>

export interface Conta {
  id: number
  clienteId: number
  agencia: string
  numeroConta: string
  tipoConta: TipoConta
  saldo: number
  dataAbertura: string
  ativa: boolean
}

export interface MovimentacaoRequest {
  valor: number
  descricao: string | null
}

export interface TransferenciaRequest extends MovimentacaoRequest {
  contaDestinoId: number
}

export interface MovimentacaoResponse {
  transacaoId: number
  tipo: TipoTransacao
  valor: number
  dataHora: string
  descricao: string | null
  contaDestinoId: number | null
  saldoAtual: number
}

export interface Transacao {
  id: number
  contaId: number
  tipo: TipoTransacao
  valor: number
  dataHora: string
  descricao: string | null
  contaDestinoId: number | null
}

export interface ExtratoItem {
  transacaoId: number
  tipo: TipoTransacao
  sentido: SentidoMovimentacao
  valor: number
  dataHora: string
  descricao: string | null
  contaOrigemId: number
  contaDestinoId: number | null
}

export interface Extrato {
  contaId: number
  saldoAtual: number
  inicio: string
  fim: string
  itens: ExtratoItem[]
}
