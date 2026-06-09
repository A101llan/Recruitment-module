# Smoke test for Interviews/Index data loading (mirrors InterviewsController.Index).
# Usage: .\tools\dev\Test-InterviewsIndexLoad.ps1 -Username testcompanyltdadmin
param(
    [string]$Username = "testcompanyltdadmin"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$bin = Join-Path $repoRoot "Publish\bin"
if (-not (Test-Path (Join-Path $bin "HR.Web.dll"))) {
    $bin = Join-Path $repoRoot "HR.Web\bin"
}

$dll = Join-Path $bin "HR.Web.dll"
$ef = Join-Path $bin "EntityFramework.dll"

if (-not (Test-Path $dll)) {
    Write-Error "Build HR.Web first. Missing $dll"
}

Add-Type -Path $ef -ErrorAction SilentlyContinue
Add-Type -Path $dll

$stepCode = @"
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using HR.Web.Data;
using HR.Web.Models;
using HR.Web.Services;

namespace DevTools {
    public static class InterviewsIndexSteps {
        public static User LoadUser(string username) {
            using (var uow = new UnitOfWork()) {
                var lower = username.ToLower();
                return uow.Context.Users.FirstOrDefault(u => u.UserName.ToLower() == lower);
            }
        }

        public static List<Interview> LoadManagementInterviews(int? companyId, bool asSuperAdmin) {
            using (var uow = new UnitOfWork()) {
                var items = uow.Context.Interviews
                    .Include("Application.Applicant")
                    .Include("Application.Position")
                    .Include("Interviewer")
                    .AsQueryable();

                if (!asSuperAdmin && companyId.HasValue) {
                    items = items.Where(i => i.CompanyId == companyId.Value);
                }

                return items.OrderByDescending(i => i.ScheduledAt).ToList();
            }
        }

        public static List<Application> LoadApplicationsWithoutScheduledInterview(int? companyId, bool asSuperAdmin, IEnumerable<Interview> interviews) {
            var scheduledApplicationIds = new HashSet<int>(
                interviews != null ? interviews.Select(i => i.ApplicationId) : Enumerable.Empty<int>());

            using (var uow = new UnitOfWork()) {
                var appsQuery = uow.Context.Applications
                    .Include("Applicant")
                    .Include("Position")
                    .AsQueryable();

                if (!asSuperAdmin && companyId.HasValue) {
                    appsQuery = appsQuery.Where(a => a.CompanyId == companyId.Value);
                }

                return appsQuery
                    .ToList()
                    .Where(a => !scheduledApplicationIds.Contains(a.Id))
                    .OrderByDescending(a => a.AppliedOn)
                    .ToList();
            }
        }

        public static void LoadEmailCc(int companyId) {
            using (var uow = new UnitOfWork()) {
                var tenantUsers = uow.Users.GetAll().Where(u => u.CompanyId == companyId).ToList();
                var panelists = CandidateEmailCcHelper.GetPanelistUsersForCc(tenantUsers);
                var hrContacts = CandidateEmailCcHelper.GetActiveHrContacts(uow.Context, companyId);
                Console.WriteLine("Panelists=" + panelists.Count + " HrContacts=" + hrContacts.Count);
            }
        }
    }
}
"@

if (-not ("DevTools.InterviewsIndexSteps" -as [type])) {
    Add-Type -TypeDefinition $stepCode -ReferencedAssemblies @($dll, $ef, "System.dll", "System.Core.dll")
}

function Show-ExceptionChain($ex) {
    $depth = 0
    while ($ex) {
        Write-Host ("  " * $depth + $ex.GetType().Name + ": " + $ex.Message)
        $ex = $ex.InnerException
        $depth++
    }
}

$user = [DevTools.InterviewsIndexSteps]::LoadUser($Username)
if ($null -eq $user) {
    Write-Error "User '$Username' not found."
}

$isSuperAdmin = [string]::Equals($user.Role, "SuperAdmin", [StringComparison]::OrdinalIgnoreCase) -and -not $user.CompanyId.HasValue
$isManagement = [string]::Equals($user.Role, "Admin", [StringComparison]::OrdinalIgnoreCase) -or $isSuperAdmin

Write-Host "User: $($user.UserName) | Role: $($user.Role) | CompanyId: $($user.CompanyId) | Management: $isManagement"

if (-not $isManagement) {
    Write-Host "Applicant path - skipping management-only steps." -ForegroundColor Yellow
    exit 0
}

try {
    $interviews = [DevTools.InterviewsIndexSteps]::LoadManagementInterviews($user.CompanyId, $isSuperAdmin)
    Write-Host "[OK] Interviews loaded: $($interviews.Count)" -ForegroundColor Green
}
catch {
    Write-Host "[FAIL] LoadManagementInterviews" -ForegroundColor Red
    Show-ExceptionChain $_.Exception
    exit 1
}

try {
    $unscheduled = [DevTools.InterviewsIndexSteps]::LoadApplicationsWithoutScheduledInterview($user.CompanyId, $isSuperAdmin, $interviews)
    Write-Host "[OK] Applications without interview: $($unscheduled.Count)" -ForegroundColor Green
}
catch {
    Write-Host "[FAIL] LoadApplicationsWithoutScheduledInterview" -ForegroundColor Red
    Show-ExceptionChain $_.Exception
    exit 1
}

if ($user.CompanyId.HasValue) {
    try {
        [DevTools.InterviewsIndexSteps]::LoadEmailCc($user.CompanyId.Value)
        Write-Host "[OK] Email CC lookups" -ForegroundColor Green
    }
    catch {
        Write-Host "[FAIL] LoadEmailCc" -ForegroundColor Red
        Show-ExceptionChain $_.Exception
        exit 1
    }
}

Write-Host "All Interviews/Index data steps succeeded for '$Username'." -ForegroundColor Cyan
