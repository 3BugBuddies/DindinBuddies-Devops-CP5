#!/usr/bin/env bash
#
# Libera (ou remove) o acesso de um IP ao Azure SQL do DindinBuddies, criando uma regra
# no firewall do servidor. Serve para conectar ferramentas como DataGrip, Azure Data Studio
# ou sqlcmd direto ao banco, a partir de uma máquina fora da Azure.
#
# Por padrão o firewall só aceita os serviços da Azure (a API). Este script libera um IP
# específico, sem abrir o banco para a internet.
#
# Uso (a partir da raiz do repositório):
#   ./scripts/liberar-ip.sh                  libera o IP público desta máquina
#   ./scripts/liberar-ip.sh 200.150.10.20    libera um IP informado (ex.: o do professor)
#   ./scripts/liberar-ip.sh --remover        remove a regra do IP desta máquina
#   ./scripts/liberar-ip.sh --remover <ip>   remove a regra do IP informado
#
# Lê GRUPO, SUFIXO e ASSINATURA do scripts/deploy.env, se existir (variáveis de ambiente
# têm prioridade). Sem SUFIXO, procura o servidor sql-dindinbuddies-* no Resource Group.

set -euo pipefail

# No Git Bash (Windows), evita que argumentos que começam com "/" virem caminhos do Windows.
export MSYS_NO_PATHCONV=1

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARQUIVO_CONFIG="${CONFIG:-${RAIZ}/scripts/deploy.env}"

ok()    { printf '    \033[32m✔ %s\033[0m\n' "$*"; }
info()  { printf '    %s\n' "$*"; }
falha() { printf '\n\033[1;31m✖ %s\033[0m\n' "$*" >&2; exit 1; }
azv()   { az "$@" --only-show-errors -o tsv | tr -d '\r'; }

# ---------------------------------------------------------------------------
# Argumentos
# ---------------------------------------------------------------------------
ACAO="liberar"
IP=""
for argumento in "$@"; do
  case "$argumento" in
    --remover) ACAO="remover" ;;
    -h|--help) sed -n '2,18p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) IP="$argumento" ;;
  esac
done

# ---------------------------------------------------------------------------
# Configuração: variáveis de ambiente > scripts/deploy.env > padrões
# ---------------------------------------------------------------------------
AMBIENTE_GRUPO="${GRUPO:-}"
AMBIENTE_SUFIXO="${SUFIXO:-}"
AMBIENTE_ASSINATURA="${ASSINATURA:-}"
if [[ -f "$ARQUIVO_CONFIG" ]]; then
  # tr remove o "\r" caso o arquivo tenha sido salvo com fim de linha do Windows.
  eval "$(tr -d '\r' < "$ARQUIVO_CONFIG")"
fi
GRUPO="${AMBIENTE_GRUPO:-${GRUPO:-rg-dindinbuddies}}"
SUFIXO="${AMBIENTE_SUFIXO:-${SUFIXO:-}}"
ASSINATURA="${AMBIENTE_ASSINATURA:-${ASSINATURA:-}}"

# ---------------------------------------------------------------------------
# Azure CLI e servidor
# ---------------------------------------------------------------------------
command -v az >/dev/null 2>&1 || falha "Azure CLI não encontrado."
az account show -o none 2>/dev/null || falha "Azure CLI não está logado. Rode 'az login' e tente de novo."
if [[ -n "$ASSINATURA" ]]; then
  az account set --subscription "$ASSINATURA" --only-show-errors \
    || falha "Assinatura '${ASSINATURA}' não encontrada."
fi

if [[ -n "$SUFIXO" ]]; then
  SERVIDOR="sql-dindinbuddies-${SUFIXO}"
else
  SERVIDORES="$(azv sql server list -g "$GRUPO" --query "[?starts_with(name, 'sql-dindinbuddies-')].name" || true)"
  quantidade="$(printf '%s' "$SERVIDORES" | grep -c . || true)"
  if [[ "$quantidade" == "0" ]]; then
    falha "Nenhum servidor sql-dindinbuddies-* encontrado no Resource Group '${GRUPO}'. O ambiente foi criado?"
  elif [[ "$quantidade" != "1" ]]; then
    falha "Há mais de um servidor no Resource Group '${GRUPO}': $(echo "$SERVIDORES" | tr '\n' ' ')- defina SUFIXO."
  fi
  SERVIDOR="$SERVIDORES"
fi

HOST="$(azv sql server show -g "$GRUPO" -n "$SERVIDOR" --query fullyQualifiedDomainName 2>/dev/null || true)"
[[ -n "$HOST" ]] || falha "Servidor '${SERVIDOR}' não encontrado no Resource Group '${GRUPO}'."
ok "Servidor: ${HOST}"

# ---------------------------------------------------------------------------
# IP
# ---------------------------------------------------------------------------
if [[ -z "$IP" ]]; then
  command -v curl >/dev/null 2>&1 || falha "curl não encontrado. Informe o IP: ./scripts/liberar-ip.sh <ip>"
  IP="$(curl -s --max-time 10 https://api.ipify.org || true)"
  [[ -n "$IP" ]] || IP="$(curl -s --max-time 10 https://ifconfig.me || true)"
  [[ -n "$IP" ]] || falha "Não foi possível descobrir o IP público. Informe o IP: ./scripts/liberar-ip.sh <ip>"
  info "IP público desta máquina: ${IP}"
fi
[[ "$IP" =~ ^([0-9]{1,3}\.){3}[0-9]{1,3}$ ]] || falha "IP inválido: '${IP}'. Use o formato IPv4, ex.: 200.150.10.20"

# Nome da regra a partir do IP: 191.39.144.190 -> acesso-191-39-144-190
REGRA="acesso-${IP//./-}"

# ---------------------------------------------------------------------------
# Liberar ou remover
# ---------------------------------------------------------------------------
if [[ "$ACAO" == "liberar" ]]; then
  az sql server firewall-rule create -g "$GRUPO" -s "$SERVIDOR" -n "$REGRA" \
    --start-ip-address "$IP" --end-ip-address "$IP" --only-show-errors -o none
  ok "IP ${IP} liberado no firewall (regra ${REGRA})"
else
  az sql server firewall-rule delete -g "$GRUPO" -s "$SERVIDOR" -n "$REGRA" --only-show-errors -o none \
    || falha "Regra ${REGRA} não encontrada."
  ok "Regra ${REGRA} removida (IP ${IP})"
fi

printf '\n  Regras atuais do firewall:\n'
az sql server firewall-rule list -g "$GRUPO" -s "$SERVIDOR" --only-show-errors \
  --query "[].{Regra:name, De:startIpAddress, Ate:endIpAddress}" -o table | tr -d '\r' | sed 's/^/    /'

if [[ "$ACAO" == "liberar" ]]; then
  printf '\n  Dados para conectar (DataGrip, Azure Data Studio...):\n'
  printf '    Host:     %s\n' "$HOST"
  printf '    Porta:    1433\n'
  printf '    Banco:    DindinBuddies\n'
  printf '    Usuário:  %s (senha do scripts/deploy.env)\n' "${SQL_ADMIN_USER:-dindinadmin}"
  printf '\n  A liberação pode levar até 5 minutos para valer.\n'
  printf '  Para remover depois: ./scripts/liberar-ip.sh --remover %s\n\n' "$IP"
fi
