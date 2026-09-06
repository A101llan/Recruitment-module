# WIP: AllowHtml on email Send body

Allan will implement locally in Cursor, then merge.

## Problem
TinyMCE posts HTML in `body`. Four Send POSTs read `Request.Form["body"]` → `HttpRequestValidationException` before the action runs. Mail never sends.

## Fix (safer)
Bind a small view-model with `[AllowHtml]` **only** on `Body`. No `[ValidateInput(false)]`. No Web.config requestValidation change. Keep antiforgery + `EmailBodyHtmlSanitizer.Sanitize`.

Actions:
- `SendInterviewCandidateEmail` / `SendInterviewCandidatesBatchEmail`
- `SendFailedCandidateEmail` / `SendFailedCandidatesBulkEmail`

See team notes: `ALLOWHTML_BODY_FIX_NOTES.md`.
