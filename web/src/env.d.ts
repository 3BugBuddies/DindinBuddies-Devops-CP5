interface ImportMetaEnv {
  /** URL base da API, gravada no build. */
  readonly VITE_API_URL: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
