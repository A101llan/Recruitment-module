#Requires -Version 5.0
<#
.SYNOPSIS
  Builds a single self-contained SQL file for IIS/production database bootstrap.

.DESCRIPTION
  Concatenates all idempotent schema scripts in dependency order into
  Infrastructure\Database\HR_CREATE_DATABASE_COMPLETE.sql.
  Regenerate after changing any source fragment under Infrastructure or HR.Web\Migrations.

.EXAMPLE
  .\tools\dev\Build-CompleteDatabaseScript.ps1
#>
$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$outPath = Join-Path $repoRoot "Infrastructure\Database\HR_CREATE_DATABASE_COMPLETE.sql"
$dbRoot = Join-Path $repoRoot "Infrastructure\Database"
$migRoot = Join-Path $repoRoot "HR.Web\Migrations"
$networkScriptsRoot = "\\SERVER1\from old computer\NanoHireHub(Recruitment)\DatabaseScripts"
$networkDbbRoot = "\\SERVER1\from old computer\NanoHireHub(Recruitment)\DatabaseScripts\DBBScripts2"

$sections = @(
    @{ Title = "HEADER"; Path = $null }
    @{ Title = "FULL_STAGING_DEPLOYMENT"; Path = (Join-Path $repoRoot "Infrastructure\FULL_STAGING_DEPLOYMENT.sql") }
    @{ Title = "COMPLETE_SCHEMA_FIX"; Path = (Join-Path $dbRoot "COMPLETE_SCHEMA_FIX.sql") }
    @{ Title = "MFA_SCHEMA_UPDATE"; Path = (Join-Path $repoRoot "Infrastructure\MFA_SCHEMA_UPDATE.sql") }
    @{ Title = "MFA_EXPANSION"; Path = (Join-Path $repoRoot "Infrastructure\MFA_EXPANSION.sql") }
    @{ Title = "HOST_ALIGN_EF_MODEL_COLUMNS"; Path = (Join-Path $dbRoot "HOST_ALIGN_EF_MODEL_COLUMNS.sql") }
    @{ Title = "MultiTenantMigration"; Path = (Join-Path $migRoot "MultiTenantMigration.sql") }
    @{ Title = "202604220000000_AddWeightToPositionQuestion"; Path = (Join-Path $migRoot "202604220000000_AddWeightToPositionQuestion.sql") }
    @{ Title = "202604220000001_AddRoleDefinitionsAndPermissions"; Path = (Join-Path $migRoot "202604220000001_AddRoleDefinitionsAndPermissions.sql") }
    @{ Title = "202604230000001_AddPositionTypeAndApplicantProfile"; Path = (Join-Path $migRoot "202604230000001_AddPositionTypeAndApplicantProfile.sql") }
    @{ Title = "202604230000002_AddMultipleChoiceToQuestions"; Path = (Join-Path $migRoot "202604230000002_AddMultipleChoiceToQuestions.sql") }
    @{ Title = "202604230000003_AddPassMarkToPosition"; Path = (Join-Path $migRoot "202604230000003_AddPassMarkToPosition.sql") }
    @{ Title = "202604230000004_AddPositionExpiryDate"; Path = (Join-Path $migRoot "202604230000004_AddPositionExpiryDate.sql") }
    @{ Title = "202604240000005_RemoveApplicantProfileStringLimits"; Path = (Join-Path $migRoot "202604240000005_RemoveApplicantProfileStringLimits.sql") }
    @{ Title = "202604250000001_AddCoverLetterToApplications"; Path = (Join-Path $migRoot "202604250000001_AddCoverLetterToApplications.sql") }
    @{ Title = "202604250000006_AddDateOfBirthToUsersAndApplicants"; Path = (Join-Path $migRoot "202604250000006_AddDateOfBirthToUsersAndApplicants.sql") }
    @{ Title = "202604250000007_AddCompanyLogoPathToCompanies"; Path = (Join-Path $migRoot "202604250000007_AddCompanyLogoPathToCompanies.sql") }
    @{ Title = "202604280000008_AddIsPanelistToUsers"; Path = (Join-Path $migRoot "202604280000008_AddIsPanelistToUsers.sql") }
    @{ Title = "202604300000009_AddApplicationStages"; Path = (Join-Path $migRoot "202604300000009_AddApplicationStages.sql") }
    @{ Title = "202604300000010_AddHasSecondaryStageToPosition"; Path = (Join-Path $migRoot "202604300000010_AddHasSecondaryStageToPosition.sql") }
    @{ Title = "202604300000011_QuestionnaireMultiStage"; Path = (Join-Path $migRoot "202604300000011_QuestionnaireMultiStage.sql") }
    @{ Title = "202605040000012_PanelistRoleWorkflow"; Path = (Join-Path $migRoot "202605040000012_PanelistRoleWorkflow.sql") }
    @{ Title = "202605050000013_AddCompanyHrCcEmails"; Path = (Join-Path $migRoot "202605050000013_AddCompanyHrCcEmails.sql") }
    @{ Title = "202605051000000_AddPassMarksByStageJsonToPosition"; Path = (Join-Path $migRoot "202605051000000_AddPassMarksByStageJsonToPosition.sql") }
    @{ Title = "202605052000000_AddFailedCandidateEmailSentAtToApplications"; Path = (Join-Path $migRoot "202605052000000_AddFailedCandidateEmailSentAtToApplications.sql") }
    @{ Title = "202605140000014_AddLegalAcceptanceTracking"; Path = (Join-Path $migRoot "202605140000014_AddLegalAcceptanceTracking.sql") }
    @{ Title = "202605240000015_AddQuestionnaireTemplates"; Path = (Join-Path $migRoot "202605240000015_AddQuestionnaireTemplates.sql") }
    @{ Title = "202605290000000_DropResumePathFromApplications"; Path = (Join-Path $migRoot "202605290000000_DropResumePathFromApplications.sql") }
    @{ Title = "HOST_SCHEMA_EXTENSIONS"; Path = (Join-Path $dbRoot "HOST_SCHEMA_EXTENSIONS.sql") }
    @{ Title = "HR_SCHEMA_PATCH_FOR_EXISTING_DB"; Path = (Join-Path $dbRoot "HR_SCHEMA_PATCH_FOR_EXISTING_DB.sql") }
    @{ Title = "VERIFY_MODEL_COLUMNS"; Path = (Join-Path $migRoot "Verify-ModelColumns.sql") }
    @{ Title = "FOOTER"; Path = $null }
)

$generated = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$sb = New-Object System.Text.StringBuilder

[void]$sb.AppendLine("-- =============================================================================")
[void]$sb.AppendLine("-- HR Recruitment Module - COMPLETE DATABASE BOOTSTRAP (single file)")
[void]$sb.AppendLine("-- =============================================================================")
[void]$sb.AppendLine("-- AUTO-GENERATED by tools/dev/Build-CompleteDatabaseScript.ps1")
[void]$sb.AppendLine("-- Generated: $generated")
[void]$sb.AppendLine("--")
[void]$sb.AppendLine("-- Run in SSMS or: sqlcmd -S YOUR_SERVER -E -b -i Infrastructure\Database\HR_CREATE_DATABASE_COMPLETE.sql")
[void]$sb.AppendLine("-- Creates database HR_Local and full schema.")
[void]$sb.AppendLine("-- Admin bootstrap password is generated at deploy-time by Invoke-HostDatabaseScript.ps1.")
[void]$sb.AppendLine("-- No custom stored procedures - tables + constraints only.")
[void]$sb.AppendLine("-- Network source package paths:")
[void]$sb.AppendLine("--   $networkScriptsRoot")
[void]$sb.AppendLine("--   $networkDbbRoot")
[void]$sb.AppendLine("--")
[void]$sb.AppendLine("-- To regenerate after schema changes: .\tools\dev\Build-CompleteDatabaseScript.ps1")
[void]$sb.AppendLine("-- Verify: HR.Web\Migrations\Verify-ModelColumns.sql")
[void]$sb.AppendLine("-- Existing DB missing columns only: Infrastructure\Database\HR_SCHEMA_PATCH_FOR_EXISTING_DB.sql")
[void]$sb.AppendLine("-- =============================================================================")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("SET NOCOUNT ON;")
[void]$sb.AppendLine("GO")
[void]$sb.AppendLine("")

foreach ($section in $sections) {
    if ($section.Title -eq "HEADER" -or $section.Title -eq "FOOTER") {
        continue
    }

    $path = $section.Path
    if (-not (Test-Path $path)) {
        throw "Missing source file for section $($section.Title): $path"
    }

    [void]$sb.AppendLine("-- #############################################################################")
    [void]$sb.AppendLine("-- BEGIN SECTION: $($section.Title)")
    [void]$sb.AppendLine("-- Source: $($path.Replace($repoRoot + '\', ''))")
    [void]$sb.AppendLine("-- #############################################################################")
    [void]$sb.AppendLine("")

    $content = Get-Content -Path $path -Raw -Encoding UTF8
  # Strip :r directives if any fragment accidentally contains them
    $content = $content -replace '(?m)^\s*:r\s+.*\r?\n', ''
    [void]$sb.AppendLine($content.TrimEnd())
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("GO")
    [void]$sb.AppendLine("")
}

[void]$sb.AppendLine("-- =============================================================================")
[void]$sb.AppendLine("PRINT '';")
[void]$sb.AppendLine("PRINT 'HR_CREATE_DATABASE_COMPLETE finished successfully.';")
[void]$sb.AppendLine("PRINT 'Database: HR_Local | Bootstrap user: admin';")
[void]$sb.AppendLine("PRINT 'Bootstrap password is generated by Invoke-HostDatabaseScript.ps1';")
[void]$sb.AppendLine("PRINT '';")
[void]$sb.AppendLine("GO")

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($outPath, $sb.ToString(), $utf8NoBom)

$lineCount = (Get-Content $outPath).Count
Write-Host "Wrote $outPath ($lineCount lines)" -ForegroundColor Green
