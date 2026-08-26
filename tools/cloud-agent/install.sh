#!/usr/bin/env bash
# =============================================================================
# NanoHireHub - Cloud Agent / Linux (Mono) development environment installer
# =============================================================================
# Durable, idempotent setup for running the ASP.NET MVC 4 (.NET Framework 4.0)
# app on Linux via Mono + fastcgi-mono-server4 + nginx, backed by SQL Server for
# Linux. Safe to re-run. Long-running services are started by start.sh, not here.
#
# The app is normally a Windows/IIS app. On Linux we run it under Mono. The data
# stack is EntityFramework 6.1.3 (net40): EF5 cannot run on Mono because it
# depends on the framework's System.Data.Entity provider manifest, which Mono
# does not implement. EF6 bundles its own SQL Server provider and works.
#
# Overridable via environment variables:
#   MSSQL_SA_PASSWORD  (default: HirehubDev2026x)  SQL Server 'sa' password
#   HR_DB_NAME         (default: HR_Local)         application database name
#   HR_ADMIN_PASSWORD  (default: Nanosoft#2026!)   bootstrap 'admin' password
# =============================================================================
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

SA_PASSWORD="${MSSQL_SA_PASSWORD:-HirehubDev2026x}"
DB_NAME="${HR_DB_NAME:-HR_Local}"
ADMIN_PASSWORD="${HR_ADMIN_PASSWORD:-Nanosoft#2026!}"
SQLCMD=/opt/mssql-tools18/bin/sqlcmd

log() { printf '\n=== %s ===\n' "$*"; }

# -----------------------------------------------------------------------------
log "1/8 System packages (Mono, fastcgi, nginx)"
# -----------------------------------------------------------------------------
sudo apt-get update -y
sudo DEBIAN_FRONTEND=noninteractive apt-get install -y \
    mono-complete mono-fastcgi-server4 nginx unixodbc-dev curl ca-certificates
# Trust the system CA bundle from Mono so `nuget.exe` can use TLS.
sudo cert-sync /etc/ssl/certs/ca-certificates.crt || true

# -----------------------------------------------------------------------------
log "2/8 SQL Server for Linux + tools"
# -----------------------------------------------------------------------------
if ! [ -x /opt/mssql/bin/sqlservr ]; then
    curl -fsSL https://packages.microsoft.com/keys/microsoft.asc \
        | sudo tee /etc/apt/trusted.gpg.d/microsoft.asc >/dev/null
    # SQL Server 2022 is published for Ubuntu 22.04; it runs fine on 24.04.
    curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list \
        | sudo tee /etc/apt/sources.list.d/mssql-server-2022.list >/dev/null
    curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/prod.list \
        | sudo tee /etc/apt/sources.list.d/msprod.list >/dev/null
    sudo apt-get update -y
    sudo ACCEPT_EULA=Y DEBIAN_FRONTEND=noninteractive apt-get install -y mssql-server
fi
sudo ACCEPT_EULA=Y DEBIAN_FRONTEND=noninteractive apt-get install -y mssql-tools18

# SQL Server 2022 links against OpenLDAP 2.5, but Ubuntu 24.04 ships 2.6.
if ! [ -e /usr/lib/x86_64-linux-gnu/liblber-2.5.so.0 ]; then
    log "  installing libldap-2.5 (jammy) for SQL Server compatibility"
    LDAP_DEB="libldap-2.5-0_2.5.20+dfsg-0ubuntu0.22.04.1_amd64.deb"
    LDAP_URL="http://security.ubuntu.com/ubuntu/pool/main/o/openldap"
    if ! curl -fsSLO "$LDAP_URL/$LDAP_DEB"; then
        # Fall back to whatever 2.5 build the mirror currently offers.
        LDAP_DEB="$(curl -fsSL "$LDAP_URL/" | grep -o 'libldap-2.5-0_[^"]*_amd64.deb' | sort -u | tail -1)"
        curl -fsSLO "$LDAP_URL/$LDAP_DEB"
    fi
    sudo dpkg -i "$LDAP_DEB"
    rm -f "$LDAP_DEB"
fi

# -----------------------------------------------------------------------------
log "3/8 Restore NuGet packages"
# -----------------------------------------------------------------------------
# Remove the transient Mono build project so solution restore is unambiguous.
rm -f HR.Web/HR.Web.mono.csproj
mono nuget.exe restore HR.sln -Verbosity quiet

# -----------------------------------------------------------------------------
log "4/8 Generate local dev configs (Web.config is gitignored)"
# -----------------------------------------------------------------------------
bash tools/cloud-agent/generate-configs.sh

# -----------------------------------------------------------------------------
log "5/8 Build HR.Web with Mono (xbuild)"
# -----------------------------------------------------------------------------
# xbuild (the only MSBuild available in mono-complete) does not understand the
# SDK-style <Content Remove=.../> item removals, so build a sanitized copy. The
# committed HR.Web.csproj is left untouched for Windows/Visual Studio.
grep -v 'Remove="' HR.Web/HR.Web.csproj > HR.Web/HR.Web.mono.csproj
xbuild HR.Web/HR.Web.mono.csproj /p:Configuration=Debug /p:VisualStudioVersion=15.0 /nologo /verbosity:minimal

# -----------------------------------------------------------------------------
log "6/8 Initialize SQL Server data directory"
# -----------------------------------------------------------------------------
# First boot initializes system databases using the SA password from the env.
SQL_LOG=/tmp/cursor/sqlservr-install.log
mkdir -p /tmp/cursor
if ! sudo test -f /var/opt/mssql/data/master.mdf; then
    log "  first-time SQL Server initialization"
fi
sudo -u mssql env ACCEPT_EULA=Y MSSQL_SA_PASSWORD="$SA_PASSWORD" MSSQL_PID=Developer \
    /opt/mssql/bin/sqlservr >"$SQL_LOG" 2>&1 &
SQL_PID=$!

# Wait for the server to accept connections.
for i in $(seq 1 60); do
    if "$SQLCMD" -S 127.0.0.1,1433 -U sa -P "$SA_PASSWORD" -C -l 2 -Q "SELECT 1" >/dev/null 2>&1; then
        break
    fi
    sleep 2
done

# -----------------------------------------------------------------------------
log "7/8 Apply database schema + seed bootstrap admin"
# -----------------------------------------------------------------------------
DB_EXISTS="$("$SQLCMD" -S 127.0.0.1,1433 -U sa -P "$SA_PASSWORD" -C -h -1 -W \
    -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('$DB_NAME') IS NULL THEN 0 ELSE 1 END" 2>/dev/null | tr -d '[:space:]')"

if [ "$DB_EXISTS" != "1" ]; then
    # Compute a valid PBKDF2 password hash using the app's own PasswordHelper.
    HASH_DIR=/tmp/cursor/genhash
    mkdir -p "$HASH_DIR"
    cat > "$HASH_DIR/GenHash.cs" <<'CS'
using System; using HR.Web.Helpers;
class GenHash { static void Main(string[] a){ Console.Write(PasswordHelper.HashPassword(a[0])); } }
CS
    mcs "$HASH_DIR/GenHash.cs" -r:HR.Web/bin/HR.Web.dll -out:"$HASH_DIR/GenHash.exe"
    ADMIN_HASH="$(mono "$HASH_DIR/GenHash.exe" "$ADMIN_PASSWORD")"

    # Pre-substitute the sqlcmd variable ourselves and disable sqlcmd variable
    # parsing (-x): the script contains a literal N'$(%' that sqlcmd otherwise
    # misreads as a variable reference.
    READY_SQL=/tmp/cursor/schema_ready.sql
    sed "s|\$(BOOTSTRAP_ADMIN_PASSWORD_HASH)|$ADMIN_HASH|g" \
        Infrastructure/Database/HR_CREATE_DATABASE_COMPLETE.sql > "$READY_SQL"

    "$SQLCMD" -S 127.0.0.1,1433 -U sa -P "$SA_PASSWORD" -C -x -f 65001 -i "$READY_SQL" || true
    "$SQLCMD" -S 127.0.0.1,1433 -U sa -P "$SA_PASSWORD" -C -x -f 65001 -d "$DB_NAME" \
        -i Infrastructure/Database/HR_SCHEMA_PATCH_FOR_EXISTING_DB.sql || true
    # One column (Applicants.FullName) is referenced before its ADD commits in a
    # batch on a fresh DB; ensure it exists so the EF model matches.
    "$SQLCMD" -S 127.0.0.1,1433 -U sa -P "$SA_PASSWORD" -C -d "$DB_NAME" \
        -Q "IF COL_LENGTH('dbo.Applicants','FullName') IS NULL ALTER TABLE dbo.Applicants ADD FullName NVARCHAR(200) NULL;"
    # Bootstrap admin is created email-verified for a smoother first login.
    "$SQLCMD" -S 127.0.0.1,1433 -U sa -P "$SA_PASSWORD" -C -d "$DB_NAME" \
        -Q "UPDATE Users SET IsEmailVerified = 1 WHERE UserName = 'admin';"
    log "  bootstrap admin credentials -> username: admin  password: $ADMIN_PASSWORD"
else
    log "  database $DB_NAME already present, skipping schema bootstrap"
fi

# Verify the schema matches the EF model (expect: 0 missing columns).
"$SQLCMD" -S 127.0.0.1,1433 -U sa -P "$SA_PASSWORD" -C -x -f 65001 -d "$DB_NAME" \
    -i HR.Web/Migrations/Verify-ModelColumns.sql | grep -i "missing column count\|All expected" || true

# -----------------------------------------------------------------------------
log "8/8 Stop the temporary SQL Server (start.sh runs it per-boot)"
# -----------------------------------------------------------------------------
kill "$SQL_PID" 2>/dev/null || true
wait "$SQL_PID" 2>/dev/null || true

log "install complete"
