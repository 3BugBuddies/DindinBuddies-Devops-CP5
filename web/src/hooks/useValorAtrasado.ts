import { useEffect, useState } from 'react'

/** Devolve o valor só depois que ele para de mudar por `atrasoMs` (ex.: busca enquanto digita). */
export function useValorAtrasado<T>(valor: T, atrasoMs = 400): T {
  const [atrasado, setAtrasado] = useState(valor)

  useEffect(() => {
    const id = setTimeout(() => setAtrasado(valor), atrasoMs)
    return () => clearTimeout(id)
  }, [valor, atrasoMs])

  return atrasado
}
