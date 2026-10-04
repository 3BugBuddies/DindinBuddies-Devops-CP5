import { isAxiosError } from 'axios'

interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}

export interface ErroApi {
  /** Mensagem principal para o usuário. */
  mensagem: string
  /** Mensagens por campo (erro 400 de validação dos DTOs). */
  campos: string[]
  status?: number
}

/**
 * Converte o erro de uma chamada à API (ProblemDetails) em mensagens legíveis.
 */
export function lerErro(erro: unknown): ErroApi {
  if (!isAxiosError<ProblemDetails>(erro)) {
    return { mensagem: 'Ocorreu um erro inesperado.', campos: [] }
  }

  if (!erro.response) {
    return { mensagem: 'Não foi possível conectar à API. Verifique se ela está no ar.', campos: [] }
  }

  const { status, data } = erro.response
  const campos = data?.errors ? Object.values(data.errors).flat() : []

  if (campos.length > 0) {
    return { mensagem: 'Verifique os campos informados.', campos, status }
  }

  return { mensagem: data?.detail ?? data?.title ?? `Erro ${status} ao chamar a API.`, campos: [], status }
}
