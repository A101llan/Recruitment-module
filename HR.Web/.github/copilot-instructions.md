# Copilot Instructions for HR.Web/Views/Positions

## Project Overview
This project is an ASP.NET MVC application. The `Views/Positions` directory contains Razor views for CRUD operations on "Position" entities. Each `.cshtml` file represents a UI for a specific action:
- `Index.cshtml`: List all positions
- `Create.cshtml`: Form to add a new position
- `Edit.cshtml`: Form to edit an existing position
- `Details.cshtml`: View details of a position
- `Delete.cshtml`: Confirm deletion of a position

## Key Patterns & Conventions
- **Razor Views**: Use C# Razor syntax for dynamic content. Model binding is typically to a `Position` model.
- **Partial Views/Sections**: If you see `@RenderSection` or `@Html.Partial`, follow the pattern for modular UI.
- **Naming**: File names match their controller actions (Index, Create, Edit, etc.).
- **Model Usage**: Views expect a strongly-typed model, usually declared at the top with `@model Namespace.Position`.
- **Validation**: Client/server validation uses ASP.NET MVC validation helpers (e.g., `@Html.ValidationMessageFor`).

## Developer Workflows
- **Build**: Use Visual Studio or `dotnet build` to compile the project.
- **Run/Debug**: Use Visual Studio's debugger or `dotnet run` for local development.
- **Test**: If tests exist, they are typically in a separate `Tests` project. Use `dotnet test`.

## Admin Features: Candidate Rankings by Position

### Candidate Filtering & Ranking
The admin panel allows HR managers to view all candidates grouped and ranked by the position they applied for:

- **View**: `Views/Admin/CandidateRankings.cshtml`
- **Controller**: `AdminController.CandidateRankings()` action
- **ViewModel**: `CandidateRankingsViewModel` + `CandidateApplicationScore`

**Key Features:**
- Candidates grouped by position (major structural decision: position is primary grouping, not individual candidate)
- Ranked by total points/score within each position group
- Dropdown filter to view specific position applicants
- Expandable rows showing candidate details, applied date, questionnaire score, status
- Action buttons for "View Details" and "Schedule Interview"
- Collapsible position sections for better UX

**Scoring Logic**: Implement in `AdminController.CalculateTotalScore()`. Currently supports questionnaire score; extend with resume score, experience matching, etc.

## Integration Points
- **Data Access**: Data flows through controllers, not views. Positions → Applications → Users model relationships.
- **External Dependencies**: Standard ASP.NET MVC; check `web.config` for DbContext and entity mappings.
- **Questionnaire Integration**: Applications reference questionnaire responses; scoring uses these responses.

## Examples
- To add a new field to all position forms, update `Create.cshtml` and `Edit.cshtml`.
- To change candidate ranking display, edit `Views/Admin/CandidateRankings.cshtml` candidate-row section.
- To modify scoring algorithm, update `CalculateTotalScore()` in `AdminController.cs`.

## Tips for AI Agents
- Always follow the existing Razor and MVC patterns.
- Do not add business logic to views; keep them presentational.
- Admin features require `[Authorize(Roles = "Admin,HR")]` attribute on controller actions.
- Position-based grouping is the architectural pattern; maintain this separation in queries and views.
- Reference the ViewModel `CandidateRankingsViewModel` when querying grouped candidate data.

---
If you add new conventions or workflows, update this file to help future contributors and AI agents.