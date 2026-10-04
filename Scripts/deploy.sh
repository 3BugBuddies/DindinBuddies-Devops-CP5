#!/usr/bin/env bash
#
# Cria todo o ambiente do DindinBuddies na Azure:
# Resource Group, Azure SQL, Log Analytics + Application Insights, Container Registry,
# App Service Plan e os Web Apps da API e do front.
#
# Pré-requisito: Azure CLI instalado e logado (`az login`). Docker é opcional: só é usado
# se a assinatura não permitir o `az acr build` (comum em Azure for Students).
#
# Uso (a partir da raiz do repositório):
#   ./Scripts/deploy.sh
#
# Variáveis de ambiente opcionais:
#   SQL_ADMIN_PASSWORD  senha do administrador do SQL (se ausente, é pedida na execução)
#   SUFIXO              sufixo dos nomes globais; informe o de uma execução anterior para
#                       atualizar os mesmos recursos em vez de criar novos
#   REGIAO              região da Azure (padrão: chilecentral)

set -euo pipefail

# No Git Bash (Windows), evita que argumentos como "/subscriptions/..." virem caminhos do Windows.
export MSYS_NO_PATHCONV=1

# ---------------------------------------------------------------------------
# Configuração
# ---------------------------------------------------------------------------
REGIAO="${REGIAO:-chilecentral}"
PREFIXO="dindinbuddies"
# 5 caracteres hexadecimais aleatórios (o od lê só 3 bytes, sem quebrar o pipefail).
SUFIXO="${SUFIXO:-$(od -An -N3 -tx1 /dev/urandom | tr -d ' \n' | cut -c1-5)}"

GRUPO="rg-${PREFIXO}"
SQL_SERVIDOR="sql-${PREFIXO}-${SUFIXO}"
SQL_BANCO="DindinBuddies"
SQL_ADMIN="dindinadmin"
LOG_WORKSPACE="log-${PREFIXO}"
APP_INSIGHTS="appi-${PREFIXO}"
ACR="acr${PREFIXO}${SUFIXO}"
PLANO="asp-${PREFIXO}"
APP_API="app-${PREFIXO}-api-${SUFIXO}"
APP_WEB="app-${PREFIXO}-web-${SUFIXO}"
IMAGEM_API="dindinbuddies-api"
IMAGEM_WEB="dindinbuddies-web"
TAG="$(date +%Y%m%d%H%M%S)"

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

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

trap 'falha "O deploy parou na linha $LINENO. Veja a mensagem de erro acima. Para continuar de onde parou, rode de novo com SUFIXO=${SUFIXO}."' ERR

# ---------------------------------------------------------------------------
# 1. Verificações iniciais
# ---------------------------------------------------------------------------
etapa "1/8 Verificações iniciais"

command -v az >/dev/null 2>&1 || falha "Azure CLI não encontrado. Instale: https://learn.microsoft.com/cli/azure/install-azure-cli"
command -v curl >/dev/null 2>&1 || falha "curl não encontrado."

az account show -o none 2>/dev/null || falha "Azure CLI não está logado. Rode 'az login' e tente de novo."
ASSINATURA="$(azv account show --query name)"
ok "Assinatura: ${ASSINATURA}"

[[ "$(azv account list-locations --query "[?name=='${REGIAO}'] | length(@)")" == "1" ]] \
  || falha "A região '${REGIAO}' não está disponível nesta assinatura. Defina outra com REGIAO=<nome>."
ok "Região: ${REGIAO}"

[[ "$SUFIXO" =~ ^[a-z0-9]{3,10}$ ]] || falha "SUFIXO deve ter de 3 a 10 letras minúsculas ou números."
ok "Sufixo dos nomes: ${SUFIXO}"

for provedor in Microsoft.Web Microsoft.Sql Microsoft.ContainerRegistry Microsoft.OperationalInsights Microsoft.Insights; do
  if [[ "$(azv provider show -n "$provedor" --query registrationState)" != "Registered" ]]; then
    info "Registrando o provedor ${provedor} (pode levar alguns minutos)..."
    az provider register -n "$provedor" --wait --only-show-errors
  fi
done
ok "Provedores de recursos registrados"

az extension add --name application-insights --upgrade --only-show-errors -o none
ok "Extensão application-insights do Azure CLI pronta"

if [[ -z "${SQL_ADMIN_PASSWORD:-}" ]]; then
  read -r -s -p "    Senha do administrador do SQL (${SQL_ADMIN}): " SQL_ADMIN_PASSWORD
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
etapa "2/8 Resource Group"
az group create -n "$GRUPO" -l "$REGIAO" --only-show-errors -o none
ok "${GRUPO} (${REGIAO})"

# ---------------------------------------------------------------------------
# 3. Azure SQL
# ---------------------------------------------------------------------------
etapa "3/8 Azure SQL"
if existe sql server show -g "$GRUPO" -n "$SQL_SERVIDOR"; then
  info "Servidor já existe; atualizando a senha do administrador..."
  az sql server update -g "$GRUPO" -n "$SQL_SERVIDOR" --admin-password "$SQL_ADMIN_PASSWORD" --only-show-errors -o none
else
  info "Criando o servidor ${SQL_SERVIDOR} (alguns minutos)..."
  az sql server create -g "$GRUPO" -n "$SQL_SERVIDOR" -l "$REGIAO" \
    --admin-user "$SQL_ADMIN" --admin-password "$SQL_ADMIN_PASSWORD" --only-show-errors -o none
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
CONNECTION_STRING="Server=tcp:${SQL_HOST},1433;Database=${SQL_BANCO};User ID=${SQL_ADMIN};Password=${SQL_ADMIN_PASSWORD};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30"

# ---------------------------------------------------------------------------
# 4. Monitoramento
# ---------------------------------------------------------------------------
etapa "4/8 Log Analytics e Application Insights"
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
# 5. Container Registry e imagem da API
# ---------------------------------------------------------------------------
etapa "5/8 Container Registry e imagem da API"
if ! existe acr show -g "$GRUPO" -n "$ACR"; then
  az acr create -g "$GRUPO" -n "$ACR" -l "$REGIAO" --sku Basic --only-show-errors -o none
fi
ACR_ID="$(azv acr show -g "$GRUPO" -n "$ACR" --query id)"
ACR_SERVIDOR="$(azv acr show -g "$GRUPO" -n "$ACR" --query loginServer)"
ok "Registry ${ACR_SERVIDOR}"

# Gera a imagem com `az acr build`. Se a assinatura bloquear o ACR Tasks (comum em Azure for
# Students), usa o Docker local, quando disponível. Uso: construir_imagem <imagem> <pasta> [build-arg]
construir_imagem() {
  local imagem="$1" pasta="$2" build_arg="${3:-}"
  local referencia="${ACR_SERVIDOR}/${imagem}:${TAG}"
  local args_acr=() args_docker=()
  if [[ -n "$build_arg" ]]; then
    args_acr=(--build-arg "$build_arg")
    args_docker=(--build-arg "$build_arg")
  fi

  info "Gerando ${imagem}:${TAG} com az acr build..."
  # ${arr[@]+"${arr[@]}"} expande um array vazio sem erro no Bash 3.2 (macOS) com set -u.
  if az acr build -r "$ACR" -t "${imagem}:${TAG}" ${args_acr[@]+"${args_acr[@]}"} "${RAIZ}/${pasta}" --only-show-errors; then
    return 0
  fi

  aviso "O az acr build falhou (a assinatura pode não permitir ACR Tasks)."
  if ! command -v docker >/dev/null 2>&1 || ! docker info >/dev/null 2>&1; then
    falha "Sem az acr build e sem Docker em execução. Inicie o Docker e rode de novo com SUFIXO=${SUFIXO}."
  fi

  info "Gerando ${imagem}:${TAG} com o Docker local..."
  az acr login -n "$ACR" --only-show-errors
  docker build --platform linux/amd64 -t "$referencia" ${args_docker[@]+"${args_docker[@]}"} "${RAIZ}/${pasta}"
  docker push "$referencia"
}

construir_imagem "$IMAGEM_API" "api"
ok "Imagem ${IMAGEM_API}:${TAG}"

# ---------------------------------------------------------------------------
# 6. App Service Plan e Web Apps
# ---------------------------------------------------------------------------
etapa "6/8 App Service Plan e Web Apps"
if ! existe appservice plan show -g "$GRUPO" -n "$PLANO"; then
  az appservice plan create -g "$GRUPO" -n "$PLANO" -l "$REGIAO" --is-linux --sku B1 --only-show-errors -o none
fi
ok "Plano ${PLANO} (B1 Linux)"

# Cria o Web App com identidade gerenciada que pode baixar imagens do ACR (AcrPull), sem senha.
# Uso: criar_web_app <nome> <imagem:tag>
criar_web_app() {
  local nome="$1" imagem="$2"
  if ! existe webapp show -g "$GRUPO" -n "$nome"; then
    az webapp create -g "$GRUPO" -p "$PLANO" -n "$nome" \
      --container-image-name "${ACR_SERVIDOR}/${imagem}" \
      --assign-identity '[system]' --role AcrPull --scope "$ACR_ID" \
      --acr-use-identity --acr-identity '[system]' \
      --https-only true --only-show-errors -o none
  fi
  az webapp config set -g "$GRUPO" -n "$nome" --always-on true \
    --generic-configurations '{"acrUseManagedIdentityCreds": true}' --only-show-errors -o none
  az webapp config container set -g "$GRUPO" -n "$nome" \
    --container-image-name "${ACR_SERVIDOR}/${imagem}" \
    --container-registry-url "https://${ACR_SERVIDOR}" --only-show-errors -o none
}

criar_web_app "$APP_API" "${IMAGEM_API}:${TAG}"
API_HOST="$(azv webapp show -g "$GRUPO" -n "$APP_API" --query defaultHostName)"
API_URL="https://${API_HOST}"
ok "Web App da API: ${API_URL}"

# A URL da API só é conhecida depois que o Web App existe; o front a recebe no build.
construir_imagem "$IMAGEM_WEB" "web" "VITE_API_URL=${API_URL}"
ok "Imagem ${IMAGEM_WEB}:${TAG}"

criar_web_app "$APP_WEB" "${IMAGEM_WEB}:${TAG}"
WEB_HOST="$(azv webapp show -g "$GRUPO" -n "$APP_WEB" --query defaultHostName)"
WEB_URL="https://${WEB_HOST}"
ok "Web App do front: ${WEB_URL}"

# ---------------------------------------------------------------------------
# 7. Configuração
# ---------------------------------------------------------------------------
etapa "7/8 Configuração da API"
# Do tipo SQLAzure, vira a variável SQLAZURECONNSTR_DindinBuddies, que o .NET lê como
# ConnectionStrings:DindinBuddies. Fica oculta no portal.
az webapp config connection-string set -g "$GRUPO" -n "$APP_API" -t SQLAzure \
  --settings "DindinBuddies=${CONNECTION_STRING}" --only-show-errors -o none
az webapp config appsettings set -g "$GRUPO" -n "$APP_API" --only-show-errors -o none --settings \
  "WEBSITES_PORT=8080" \
  "APPLICATIONINSIGHTS_CONNECTION_STRING=${APPINSIGHTS_CONNECTION_STRING}" \
  "Cors__OrigemFront=${WEB_URL}"
ok "Connection string, Application Insights e CORS configurados"

az webapp config appsettings set -g "$GRUPO" -n "$APP_WEB" --settings "WEBSITES_PORT=80" --only-show-errors -o none

# Reinicia para aplicar a configuração e repetir o download da imagem já com a permissão AcrPull.
az webapp restart -g "$GRUPO" -n "$APP_API" --only-show-errors
az webapp restart -g "$GRUPO" -n "$APP_WEB" --only-show-errors
ok "Web Apps reiniciados"

# ---------------------------------------------------------------------------
# 8. Saída
# ---------------------------------------------------------------------------
etapa "8/8 Aguardando a API responder"
info "Na primeira vez, o download da imagem e a criação das tabelas levam alguns minutos."
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
printf '  Para atualizar estes recursos: SUFIXO=%s ./Scripts/deploy.sh\n' "$SUFIXO"
printf '  Para remover tudo:             az group delete -n %s --yes --no-wait\n\n' "$GRUPO"
