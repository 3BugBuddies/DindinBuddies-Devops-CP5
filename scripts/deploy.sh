#!/usr/bin/env bash
#
# Cria todo o ambiente do DindinBuddies na Azure e publica as aplicações com `az webapp deploy`:
# Resource Group, Azure SQL, Log Analytics + Application Insights, App Service Plan e os
# Web Apps da API (.NET 10) e do front (Node 24 servindo os arquivos estáticos).
#
# Pré-requisitos: Azure CLI logado (`az login`), .NET SDK 10 e Node.js com npm.
# Docker não é necessário (ele só é usado para rodar o projeto localmente).
#
# Configuração (a partir da raiz do repositório):
#   cp scripts/deploy.env.example scripts/deploy.env    # e preencha os seus valores
#   ./scripts/deploy.sh
#
# O scripts/deploy.env fica fora do Git. Todos os campos são opcionais: sem o arquivo,
# o script usa a assinatura atual do Azure CLI, a região chilecentral, um sufixo
# aleatório e pede a senha do SQL na execução. Variáveis de ambiente com os mesmos
# nomes têm prioridade sobre o arquivo (ex.: SUFIXO=abc12 ./scripts/deploy.sh).
# Outro arquivo de configuração pode ser indicado com CONFIG=<caminho>.

set -euo pipefail

# No Git Bash (Windows), evita que argumentos como "/subscriptions/..." ou "/home/site/wwwroot"
# virem caminhos do Windows. Caminhos de arquivos locais são convertidos com `nativo`.
export MSYS_NO_PATHCONV=1

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# ---------------------------------------------------------------------------
# Configuração: variáveis de ambiente > scripts/deploy.env > padrões
# ---------------------------------------------------------------------------
CAMPOS_CONFIG="ASSINATURA REGIAO GRUPO SUFIXO SQL_ADMIN_USER SQL_ADMIN_PASSWORD"
ARQUIVO_CONFIG="${CONFIG:-${RAIZ}/scripts/deploy.env}"

if [[ -f "$ARQUIVO_CONFIG" ]]; then
  # Guarda o que veio do ambiente, carrega o arquivo e devolve a prioridade ao ambiente.
  for campo in $CAMPOS_CONFIG; do printf -v "AMBIENTE_${campo}" '%s' "${!campo-}"; done
  # tr remove o "\r" caso o arquivo tenha sido salvo com fim de linha do Windows.
  eval "$(tr -d '\r' < "$ARQUIVO_CONFIG")"
  for campo in $CAMPOS_CONFIG; do
    valor_ambiente="AMBIENTE_${campo}"
    if [[ -n "${!valor_ambiente}" ]]; then printf -v "$campo" '%s' "${!valor_ambiente}"; fi
  done
fi

ASSINATURA="${ASSINATURA:-}"
REGIAO="${REGIAO:-chilecentral}"
GRUPO="${GRUPO:-rg-dindinbuddies}"
SQL_ADMIN_USER="${SQL_ADMIN_USER:-dindinadmin}"
SQL_ADMIN_PASSWORD="${SQL_ADMIN_PASSWORD:-}"
SUFIXO_GERADO=false
if [[ -z "${SUFIXO:-}" ]]; then
  # 5 caracteres hexadecimais aleatórios (o od lê só 3 bytes, sem quebrar o pipefail).
  SUFIXO="$(od -An -N3 -tx1 /dev/urandom | tr -d ' \n' | cut -c1-5)"
  SUFIXO_GERADO=true
fi

# Como repetir a execução sobre os mesmos recursos (usado nas mensagens de erro e no final).
if [[ -f "$ARQUIVO_CONFIG" ]]; then
  COMO_REPETIR="./scripts/deploy.sh (o sufixo fica salvo no deploy.env)"
else
  COMO_REPETIR="SUFIXO=${SUFIXO} ./scripts/deploy.sh"
fi

PREFIXO="dindinbuddies"
SQL_SERVIDOR="sql-${PREFIXO}-${SUFIXO}"
SQL_BANCO="DindinBuddies"
LOG_WORKSPACE="log-${PREFIXO}"
APP_INSIGHTS="appi-${PREFIXO}"
PLANO="asp-${PREFIXO}"
APP_API="app-${PREFIXO}-api-${SUFIXO}"
APP_WEB="app-${PREFIXO}-web-${SUFIXO}"
RUNTIME_API="DOTNETCORE:10.0"
RUNTIME_WEB="NODE:24-lts"
STARTUP_API="dotnet DindinBuddies.Api.dll"
# Comando recomendado pela Microsoft para SPA no runtime Node: serve os estáticos e
# devolve o index.html para as rotas do React Router.
STARTUP_WEB="pm2 serve /home/site/wwwroot --no-daemon --spa"

TEMP_DEPLOY="$(mktemp -d)"
trap 'rm -rf "$TEMP_DEPLOY"' EXIT

# ---------------------------------------------------------------------------
# Funções auxiliares
# ---------------------------------------------------------------------------
etapa() { printf '\n\033[1;36m==> %s\033[0m\n' "$*"; }
info()  { printf '    %s\n' "$*"; }
ok()    { printf '    \033[32m✔ %s\033[0m\n' "$*"; }
aviso() { printf '    \033[33m! %s\033[0m\n' "$*"; }
falha() { printf '\n\033[1;31m✖ %s\033[0m\n' "$*" >&2; exit 1; }

# Executa o az com saída em texto, sem o "\r" que o Azure CLI do Windows deixa no fim das linhas.
azv() { az "$@" --only-show-errors -o tsv | tr -d '\r'; }

# Verdadeiro se o comando "az ... show" encontrar o recurso.
existe() { az "$@" --only-show-errors -o none >/dev/null 2>&1; }

# Caminho que programas nativos do Windows (az, dotnet, python) entendem: C:/Users/... no
# Git Bash; inalterado no Linux e no macOS.
nativo() {
  if command -v cygpath >/dev/null 2>&1; then cygpath -m "$1"; else printf '%s' "$1"; fi
}

# Python com o módulo zipfile, usado para gerar os pacotes (o Git Bash não tem o comando zip).
PYTHON=""
for candidato in python3 python py; do
  if command -v "$candidato" >/dev/null 2>&1 && "$candidato" -c "import zipfile" >/dev/null 2>&1; then
    PYTHON="$candidato"
    break
  fi
done

# Compacta o conteúdo de uma pasta (não a pasta em si) em um zip com caminhos "/".
# Uso: compactar <pasta> <arquivo.zip>
compactar() {
  local origem="$1" destino="$2"
  rm -f "$destino"
  if command -v zip >/dev/null 2>&1; then
    (cd "$origem" && zip -qr "$destino" .)
  elif [[ -n "$PYTHON" ]]; then
    "$PYTHON" - "$(nativo "$origem")" "$(nativo "$destino")" <<'PY'
import os, sys, zipfile
origem, destino = sys.argv[1], sys.argv[2]
with zipfile.ZipFile(destino, "w", zipfile.ZIP_DEFLATED) as z:
    for pasta, _, arquivos in os.walk(origem):
        for nome in arquivos:
            caminho = os.path.join(pasta, nome)
            # O zipfile grava o caminho relativo com "/", mesmo no Windows.
            z.write(caminho, os.path.relpath(caminho, origem))
PY
  else
    falha "Nenhuma ferramenta para gerar zip encontrada (zip ou Python 3)."
  fi
}

trap 'falha "O deploy parou na linha $LINENO. Veja a mensagem de erro acima. Para continuar de onde parou, rode: ${COMO_REPETIR}"' ERR

# ---------------------------------------------------------------------------
# 1. Verificações iniciais
# ---------------------------------------------------------------------------
etapa "1/9 Verificações iniciais"

command -v az >/dev/null 2>&1 || falha "Azure CLI não encontrado. Instale: https://learn.microsoft.com/cli/azure/install-azure-cli"
command -v curl >/dev/null 2>&1 || falha "curl não encontrado."
command -v dotnet >/dev/null 2>&1 || falha ".NET SDK não encontrado. Instale o .NET SDK 10: https://dotnet.microsoft.com/download"
dotnet --list-sdks | grep -q '^10\.' || falha "É necessário o .NET SDK 10 (encontrados: $(dotnet --list-sdks | cut -d' ' -f1 | tr '\n' ' '))."
command -v npm >/dev/null 2>&1 || falha "Node.js/npm não encontrado. Instale o Node.js 22 ou superior: https://nodejs.org"
command -v zip >/dev/null 2>&1 || [[ -n "$PYTHON" ]] || falha "Instale o comando zip ou o Python 3 (usados para gerar os pacotes de deploy)."
ok ".NET SDK, Node.js e ferramenta de zip encontrados"

if [[ -f "$ARQUIVO_CONFIG" ]]; then
  ok "Configuração lida de ${ARQUIVO_CONFIG}"
else
  info "Sem scripts/deploy.env: usando os valores padrão (veja scripts/deploy.env.example)."
fi

az account show -o none 2>/dev/null || falha "Azure CLI não está logado. Rode 'az login' e tente de novo."
if [[ -n "$ASSINATURA" ]]; then
  az account set --subscription "$ASSINATURA" --only-show-errors \
    || falha "Assinatura '${ASSINATURA}' não encontrada. Confira o valor de ASSINATURA (az account list -o table)."
fi
ok "Assinatura: $(azv account show --query name)"

[[ "$(azv account list-locations --query "[?name=='${REGIAO}'] | length(@)")" == "1" ]] \
  || falha "A região '${REGIAO}' não está disponível nesta assinatura. Ajuste REGIAO no scripts/deploy.env."
ok "Região: ${REGIAO}"

[[ "$GRUPO" =~ ^[A-Za-z0-9._()-]{1,90}$ ]] || falha "GRUPO inválido: use letras, números, '.', '_', '-' ou parênteses."
ok "Resource Group: ${GRUPO}"

[[ "$SUFIXO" =~ ^[a-z0-9]{3,10}$ ]] || falha "SUFIXO deve ter de 3 a 10 letras minúsculas ou números."
if [[ "$SUFIXO_GERADO" == true && -f "$ARQUIVO_CONFIG" ]]; then
  # Grava o sufixo gerado no deploy.env: as próximas execuções atualizam os mesmos recursos.
  if grep -q '^SUFIXO=' "$ARQUIVO_CONFIG"; then
    sed "s/^SUFIXO=.*/SUFIXO=\"${SUFIXO}\"/" "$ARQUIVO_CONFIG" > "${ARQUIVO_CONFIG}.tmp"
    mv "${ARQUIVO_CONFIG}.tmp" "$ARQUIVO_CONFIG"
  else
    printf '\nSUFIXO="%s"\n' "$SUFIXO" >> "$ARQUIVO_CONFIG"
  fi
  ok "Sufixo dos nomes: ${SUFIXO} (gerado e gravado em ${ARQUIVO_CONFIG})"
else
  ok "Sufixo dos nomes: ${SUFIXO}"
fi

[[ "$SQL_ADMIN_USER" =~ ^[A-Za-z][A-Za-z0-9_]{0,127}$ ]] \
  || falha "SQL_ADMIN_USER inválido: comece com letra e use só letras, números e '_'."
case "$(printf '%s' "$SQL_ADMIN_USER" | tr '[:upper:]' '[:lower:]')" in
  admin|administrator|sa|root|dbmanager|loginmanager|guest|public|dbo)
    falha "SQL_ADMIN_USER '${SQL_ADMIN_USER}' é reservado pela Azure. Escolha outro nome." ;;
esac

for provedor in Microsoft.Web Microsoft.Sql Microsoft.OperationalInsights Microsoft.Insights; do
  if [[ "$(azv provider show -n "$provedor" --query registrationState)" != "Registered" ]]; then
    info "Registrando o provedor ${provedor} (pode levar alguns minutos)..."
    az provider register -n "$provedor" --wait --only-show-errors
  fi
done
ok "Provedores de recursos registrados"

az extension add --name application-insights --upgrade --only-show-errors -o none
ok "Extensão application-insights do Azure CLI pronta"

if [[ -z "${SQL_ADMIN_PASSWORD:-}" ]]; then
  read -r -s -p "    Senha do administrador do SQL (${SQL_ADMIN_USER}): " SQL_ADMIN_PASSWORD
  echo
fi
categorias=0
[[ "$SQL_ADMIN_PASSWORD" =~ [A-Z] ]] && categorias=$((categorias + 1))
[[ "$SQL_ADMIN_PASSWORD" =~ [a-z] ]] && categorias=$((categorias + 1))
[[ "$SQL_ADMIN_PASSWORD" =~ [0-9] ]] && categorias=$((categorias + 1))
[[ "$SQL_ADMIN_PASSWORD" =~ [^A-Za-z0-9] ]] && categorias=$((categorias + 1))
(( ${#SQL_ADMIN_PASSWORD} >= 8 && categorias >= 3 )) \
  || falha "A senha do SQL precisa ter 8+ caracteres e 3 destes grupos: maiúsculas, minúsculas, números, símbolos."
[[ "$SQL_ADMIN_PASSWORD" != *";"* && "$SQL_ADMIN_PASSWORD" != *"'"* && "$SQL_ADMIN_PASSWORD" != *'"'* ]] \
  || falha "A senha do SQL não pode conter ';' nem aspas (ela entra na connection string)."
ok "Senha do SQL válida"

# ---------------------------------------------------------------------------
# 2. Resource Group
# ---------------------------------------------------------------------------
etapa "2/9 Resource Group"
az group create -n "$GRUPO" -l "$REGIAO" --only-show-errors -o none
ok "${GRUPO} (${REGIAO})"

# ---------------------------------------------------------------------------
# 3. Azure SQL
# ---------------------------------------------------------------------------
etapa "3/9 Azure SQL"
if existe sql server show -g "$GRUPO" -n "$SQL_SERVIDOR"; then
  info "Servidor já existe; atualizando a senha do administrador..."
  az sql server update -g "$GRUPO" -n "$SQL_SERVIDOR" --admin-password "$SQL_ADMIN_PASSWORD" --only-show-errors -o none
else
  info "Criando o servidor ${SQL_SERVIDOR} (alguns minutos)..."
  az sql server create -g "$GRUPO" -n "$SQL_SERVIDOR" -l "$REGIAO" \
    --admin-user "$SQL_ADMIN_USER" --admin-password "$SQL_ADMIN_PASSWORD" --only-show-errors -o none
fi
ok "Servidor ${SQL_SERVIDOR}"

if ! existe sql db show -g "$GRUPO" -s "$SQL_SERVIDOR" -n "$SQL_BANCO"; then
  az sql db create -g "$GRUPO" -s "$SQL_SERVIDOR" -n "$SQL_BANCO" \
    --edition Basic --backup-storage-redundancy Local --only-show-errors -o none
fi
ok "Banco ${SQL_BANCO} (Basic)"

# A regra 0.0.0.0 é a forma da Azure de dizer "permitir serviços da Azure" (inclui o App Service).
az sql server firewall-rule create -g "$GRUPO" -s "$SQL_SERVIDOR" -n AllowAzureServices \
  --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 --only-show-errors -o none
ok "Firewall liberando os serviços da Azure"

SQL_HOST="$(azv sql server show -g "$GRUPO" -n "$SQL_SERVIDOR" --query fullyQualifiedDomainName)"
CONNECTION_STRING="Server=tcp:${SQL_HOST},1433;Database=${SQL_BANCO};User ID=${SQL_ADMIN_USER};Password=${SQL_ADMIN_PASSWORD};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30"

# ---------------------------------------------------------------------------
# 4. Monitoramento
# ---------------------------------------------------------------------------
etapa "4/9 Log Analytics e Application Insights"
if ! existe monitor log-analytics workspace show -g "$GRUPO" -n "$LOG_WORKSPACE"; then
  az monitor log-analytics workspace create -g "$GRUPO" -n "$LOG_WORKSPACE" -l "$REGIAO" --only-show-errors -o none
fi
WORKSPACE_ID="$(azv monitor log-analytics workspace show -g "$GRUPO" -n "$LOG_WORKSPACE" --query id)"
ok "Log Analytics ${LOG_WORKSPACE}"

if ! existe monitor app-insights component show -g "$GRUPO" --app "$APP_INSIGHTS"; then
  az monitor app-insights component create -g "$GRUPO" --app "$APP_INSIGHTS" -l "$REGIAO" \
    --kind web --application-type web --workspace "$WORKSPACE_ID" --only-show-errors -o none
fi
APPINSIGHTS_CONNECTION_STRING="$(azv monitor app-insights component show -g "$GRUPO" --app "$APP_INSIGHTS" --query connectionString)"
ok "Application Insights ${APP_INSIGHTS}"

# ---------------------------------------------------------------------------
# 5. App Service Plan e Web Apps
# ---------------------------------------------------------------------------
etapa "5/9 App Service Plan e Web Apps"
if ! existe appservice plan show -g "$GRUPO" -n "$PLANO"; then
  az appservice plan create -g "$GRUPO" -n "$PLANO" -l "$REGIAO" --is-linux --sku B1 --only-show-errors -o none
fi
ok "Plano ${PLANO} (B1 Linux)"

# Cria (ou ajusta) o Web App com o runtime e o comando de inicialização.
# Uso: criar_web_app <nome> <runtime> <comando de inicialização>
criar_web_app() {
  local nome="$1" runtime="$2" startup="$3"
  if ! existe webapp show -g "$GRUPO" -n "$nome"; then
    az webapp create -g "$GRUPO" -p "$PLANO" -n "$nome" --runtime "$runtime" \
      --startup-file "$startup" --https-only true --only-show-errors -o none
  fi
  az webapp config set -g "$GRUPO" -n "$nome" --always-on true --startup-file "$startup" \
    --only-show-errors -o none
  # Os pacotes chegam prontos (já compilados); a Azure não precisa rodar build no deploy.
  az webapp config appsettings set -g "$GRUPO" -n "$nome" \
    --settings "SCM_DO_BUILD_DURING_DEPLOYMENT=false" --only-show-errors -o none
}

criar_web_app "$APP_API" "$RUNTIME_API" "$STARTUP_API"
API_URL="https://$(azv webapp show -g "$GRUPO" -n "$APP_API" --query defaultHostName)"
ok "Web App da API (${RUNTIME_API}): ${API_URL}"

criar_web_app "$APP_WEB" "$RUNTIME_WEB" "$STARTUP_WEB"
WEB_URL="https://$(azv webapp show -g "$GRUPO" -n "$APP_WEB" --query defaultHostName)"
ok "Web App do front (${RUNTIME_WEB}): ${WEB_URL}"

# ---------------------------------------------------------------------------
# 6. Configuração da API
# ---------------------------------------------------------------------------
etapa "6/9 Configuração da API"
# Do tipo SQLAzure, vira a variável SQLAZURECONNSTR_DindinBuddies, que o .NET lê como
# ConnectionStrings:DindinBuddies. Fica oculta no portal.
az webapp config connection-string set -g "$GRUPO" -n "$APP_API" -t SQLAzure \
  --settings "DindinBuddies=${CONNECTION_STRING}" --only-show-errors -o none
az webapp config appsettings set -g "$GRUPO" -n "$APP_API" --only-show-errors -o none --settings \
  "APPLICATIONINSIGHTS_CONNECTION_STRING=${APPINSIGHTS_CONNECTION_STRING}" \
  "Cors__OrigemFront=${WEB_URL}"
ok "Connection string, Application Insights e CORS configurados"

# ---------------------------------------------------------------------------
# 7. Deploy da API
# ---------------------------------------------------------------------------
etapa "7/9 Deploy da API (dotnet publish + az webapp deploy)"
info "Gerando o pacote da API..."
dotnet publish "$(nativo "${RAIZ}/api/src/DindinBuddies.Api/DindinBuddies.Api.csproj")" \
  -c Release -o "$(nativo "${TEMP_DEPLOY}/api")" --nologo -v quiet
compactar "${TEMP_DEPLOY}/api" "${TEMP_DEPLOY}/api.zip"
ok "Pacote api.zip gerado"

info "Publicando no Web App ${APP_API}..."
az webapp deploy -g "$GRUPO" -n "$APP_API" --src-path "$(nativo "${TEMP_DEPLOY}/api.zip")" \
  --type zip --clean true --restart true --track-status false --only-show-errors -o none
ok "API publicada"

# ---------------------------------------------------------------------------
# 8. Deploy do front
# ---------------------------------------------------------------------------
etapa "8/9 Deploy do front (npm run build + az webapp deploy)"
info "Gerando o build do front com a URL da API (${API_URL})..."
(
  cd "${RAIZ}/web"
  npm ci --no-audit --no-fund --loglevel=error
  VITE_API_URL="$API_URL" npm run build --silent
)
compactar "${RAIZ}/web/dist" "${TEMP_DEPLOY}/web.zip"
ok "Pacote web.zip gerado"

info "Publicando no Web App ${APP_WEB}..."
az webapp deploy -g "$GRUPO" -n "$APP_WEB" --src-path "$(nativo "${TEMP_DEPLOY}/web.zip")" \
  --type zip --clean true --restart true --track-status false --only-show-errors -o none
ok "Front publicado"

# ---------------------------------------------------------------------------
# 9. Saída
# ---------------------------------------------------------------------------
etapa "9/9 Aguardando a API responder"
info "Na primeira vez, a inicialização e a criação das tabelas levam alguns minutos."
api_no_ar=false
for _ in $(seq 1 40); do
  if [[ "$(curl -s -o /dev/null -w '%{http_code}' "${API_URL}/api/clientes" || true)" == "200" ]]; then
    api_no_ar=true
    break
  fi
  sleep 15
done
if [[ "$api_no_ar" == true ]]; then
  ok "API no ar e banco acessível"
else
  aviso "A API ainda não respondeu após 10 minutos. Veja os logs: az webapp log tail -g ${GRUPO} -n ${APP_API}"
fi

trap - ERR
printf '\n\033[1;32mDeploy concluído!\033[0m\n\n'
printf '  Front:    %s\n' "$WEB_URL"
printf '  API:      %s\n' "$API_URL"
printf '  Swagger:  %s/swagger\n\n' "$API_URL"
printf '  Resource Group: %s   Sufixo: %s\n' "$GRUPO" "$SUFIXO"
printf '  Para republicar nestes recursos: %s\n' "$COMO_REPETIR"
printf '  Para remover tudo:               az group delete -n %s --yes --no-wait\n\n' "$GRUPO"
