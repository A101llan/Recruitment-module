# EF smoke test for CompanyDetails queries. Usage: .\tools\dev\Test-CompanyDetailsLoad.ps1 -CompanyId 3012
param([int]$CompanyId = 3012)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$bin = Join-Path $repoRoot "HR.Web\bin"
$dll = Join-Path $bin "HR.Web.dll"

if (-not (Test-Path $dll)) {
    Write-Error "Build HR.Web (Debug) first. Missing $dll"
}

$ef = Join-Path $bin "EntityFramework.dll"
$efSql = Join-Path $bin "EntityFramework.SqlServer.dll"
Add-Type -Path $ef
Add-Type -Path $efSql
Add-Type -Path $dll

$stepCode = @"
using System;
using System.Linq;
using HR.Web.Data;

namespace DevTools {
    public static class CompanyDetailsSteps {
        public static void RunStep(int step, int companyId) {
            using (var uow = new UnitOfWork()) {
                switch (step) {
                    case 0: uow.Users.GetAll(u => u.RoleDefinition).Where(u => u.CompanyId == companyId).ToList(); break;
                    case 1: uow.RoleDefinitions.GetAll().Where(r => r.IsActive).ToList(); break;
                    case 2: uow.Positions.GetAll(p => p.Department).Where(p => p.CompanyId == companyId).ToList(); break;
                    case 3: uow.Applications.GetAll(a => a.Applicant, a => a.Position).Where(a => a.CompanyId == companyId).ToList(); break;
                    case 4: uow.Departments.GetAll().Where(d => d.CompanyId == companyId).ToList(); break;
                    case 5: uow.LicenseTransactions.GetAll().Where(lt => lt.CompanyId == companyId).ToList(); break;
                    case 6: uow.AuditLogs.GetAll().Where(a => a.CompanyId == companyId).ToList(); break;
                    case 7: uow.ImpersonationRequests.GetAll().Where(r => r.CompanyId == companyId).ToList(); break;
                    default: throw new ArgumentOutOfRangeException("step");
                }
            }
        }
    }
}
"@

if (-not ("DevTools.CompanyDetailsSteps" -as [type])) {
    Add-Type -TypeDefinition $stepCode -ReferencedAssemblies @($dll, $ef, $efSql, "System.dll", "System.Core.dll")
}

function Show-ExceptionChain($ex) {
    $depth = 0
    while ($ex) {
        Write-Host ("  " * $depth + $ex.GetType().Name + ": " + $ex.Message)
        $ex = $ex.InnerException
        $depth++
    }
}

$names = @(
    "Users + RoleDefinition",
    "RoleDefinitions",
    "Positions + Department",
    "Applications + includes",
    "Departments",
    "LicenseTransactions",
    "AuditLogs",
    "ImpersonationRequests"
)

for ($i = 0; $i -lt $names.Count; $i++) {
    try {
        [DevTools.CompanyDetailsSteps]::RunStep($i, $CompanyId)
        Write-Host "[OK] $($names[$i])" -ForegroundColor Green
    }
    catch {
        Write-Host "[FAIL] $($names[$i])" -ForegroundColor Red
        Show-ExceptionChain $_.Exception
        throw
    }
}

Write-Host "All CompanyDetails queries succeeded for company $CompanyId." -ForegroundColor Cyan
