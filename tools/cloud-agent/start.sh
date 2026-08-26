#!/usr/bin/env bash
# =============================================================================
# NanoHireHub - Cloud Agent per-boot service starter
# =============================================================================
# Brings up the three long-running services and returns once the web app is
# reachable. Idempotent: existing healthy services are left running.
#   1. SQL Server for Linux         (127.0.0.1:1433)
#   2. fastcgi-mono-server4 (ASP.NET) (127.0.0.1:9000)
#   3. nginx reverse proxy          (0.0.0.0:5002)  -> http://localhost:5002
# =============================================================================
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

SA_PASSWORD="${MSSQL_SA_PASSWORD:-HirehubDev2026x}"
SQLCMD=/opt/mssql-tools18/bin/sqlcmd
LOG_DIR=/tmp/cursor
mkdir -p "$LOG_DIR"

port_open() { (exec 3<>"/dev/tcp/127.0.0.1/$1") 2>/dev/null && exec 3>&- ; }

# Regenerate dev configs if they went missing (they are gitignored).
[ -f HR.Web/Web.config ] || bash tools/cloud-agent/generate-configs.sh

# -----------------------------------------------------------------------------
echo "=== Starting SQL Server ==="
# -----------------------------------------------------------------------------
if port_open 1433; then
    echo "  SQL Server already listening on 1433"
else
    sudo -u mssql env ACCEPT_EULA=Y MSSQL_SA_PASSWORD="$SA_PASSWORD" MSSQL_PID=Developer \
        /opt/mssql/bin/sqlservr >"$LOG_DIR/sqlservr.log" 2>&1 &
    for i in $(seq 1 60); do
        if "$SQLCMD" -S 127.0.0.1,1433 -U sa -P "$SA_PASSWORD" -C -l 2 -Q "SELECT 1" >/dev/null 2>&1; then
            echo "  SQL Server ready"; break
        fi
        sleep 2
    done
fi

# -----------------------------------------------------------------------------
echo "=== Starting fastcgi-mono-server4 (ASP.NET app) ==="
# -----------------------------------------------------------------------------
if port_open 9000; then
    echo "  Mono FastCGI already listening on 9000"
else
    MONO_IOMAP=all fastcgi-mono-server4 \
        /applications=/:"$REPO_ROOT/HR.Web" \
        /socket=tcp:127.0.0.1:9000 \
        /printlog=True /loglevels=Standard >"$LOG_DIR/fastcgi.log" 2>&1 &
    for i in $(seq 1 30); do
        port_open 9000 && { echo "  Mono FastCGI ready"; break; }
        sleep 1
    done
fi

# -----------------------------------------------------------------------------
echo "=== Starting nginx (reverse proxy on :5002) ==="
# -----------------------------------------------------------------------------
sed "s|__APP_ROOT__|$REPO_ROOT|g" tools/cloud-agent/nginx-hirehub.conf \
    | sudo tee /etc/nginx/sites-available/hirehub.conf >/dev/null
sudo rm -f /etc/nginx/sites-enabled/default
sudo ln -sf /etc/nginx/sites-available/hirehub.conf /etc/nginx/sites-enabled/hirehub.conf
sudo nginx -t
if sudo nginx -s reload 2>/dev/null; then
    echo "  nginx reloaded"
else
    sudo nginx
    echo "  nginx started"
fi

# -----------------------------------------------------------------------------
echo "=== Health check ==="
# -----------------------------------------------------------------------------
for i in $(seq 1 30); do
    CODE="$(curl -s -o /dev/null -w '%{http_code}' -m 30 http://127.0.0.1:5002/Account/Login || true)"
    if [ "$CODE" = "200" ]; then
        echo "  http://localhost:5002/Account/Login -> HTTP 200 (ready)"
        exit 0
    fi
    sleep 2
done

echo "  WARNING: app did not return HTTP 200 in time; check $LOG_DIR/fastcgi.log"
exit 0
