# TeamsMIPPoC

Minimal Microsoft Teams backend built with .NET/C# to return a document sensitivity label for SharePoint Online (SPO) and OneDrive for Business (ODfB) links via a real MIP SDK-backed service.

## What this provides

- ASP.NET Core API endpoint usable from a Teams bot/tab messaging flow.
- Minimal Teams message endpoint (`POST /api/messages`) to process a message that contains a file URL.
- Endpoint accepts a file link copied from the SPO/ODfB file details pane.
- URL validation for supported Microsoft 365 business locations.
- Label response payload returned from a real MIP SDK-backed upstream integration service.
- Entra ID JWT authentication support for protecting API access.

## API

`POST /api/sensitivity-label`

Request body:

```json
{
  "fileUrl": "https://contoso.sharepoint.com/sites/legal/Shared%20Documents/contract.docx"
}
```

Example response:

```json
{
  "fileUrl": "https://contoso.sharepoint.com/sites/legal/Shared%20Documents/contract.docx",
  "sensitivityLabel": "Confidential",
  "source": "MIP SDK",
  "evidence": "Label resolved from MIP SDK-backed service."
}
```

`POST /api/messages`

Request body:

```json
{
  "text": "Please check https://contoso.sharepoint.com/sites/legal/Shared%20Documents/contract.docx"
}
```

Example response:

```json
{
  "reply": "Sensitivity label: Confidential (MIP SDK)"
}
```

## Authentication model

- API uses Microsoft Entra ID JWT bearer authentication in non-development environments.
- Default model is **app-only** service-to-service flow.
- Configure `Authentication:Authority` and `Authentication:Audience`.
- Development profile disables auth by default for local iteration.

## MIP integration model

- This API does not generate labels locally.
- It calls a configured upstream service (`MipIntegration:ServiceBaseUrl`) that must be backed by a real MIP SDK implementation.
- No simulated label fallback is provided.
- Required MIP integration settings:
  - `MipIntegration:ServiceBaseUrl`
  - `MipIntegration:LabelLookupPath`
  - `MipIntegration:Scope`
  - `MipIntegration:TenantId`
  - `MipIntegration:ClientId`
  - `MipIntegration:ClientSecret` (set via environment variable / Key Vault reference, not committed)

## Run locally

Prerequisite: install the .NET 10 SDK (`net10.0`) before running or testing this project.

```bash
dotnet run --project TeamsMIPPoC.Api
```

Set secrets and environment-specific config before non-development runs.

## Test

```bash
dotnet test TeamsMIPPoC.slnx
```

## Deploy to a Microsoft 365 Developer tenant (Teams)

1. Create or use an existing [Microsoft 365 Developer Program](https://developer.microsoft.com/microsoft-365/dev-program) tenant.
2. Expose the local API publicly (for example with dev tunnels or ngrok) and note the HTTPS base URL.
3. In the [Teams Developer Portal](https://dev.teams.microsoft.com), create a new Teams app.
4. Add a bot capability to the app and set the bot messaging endpoint to your hosted API endpoint (`https://<public-host>/api/messages`).
5. In **App package**, define valid domains for your API host and required permissions/scopes.
6. Install the app to your developer tenant and Teams client from Developer Portal (**Test and distribute** -> **Install**).
7. In Teams, send or paste a SharePoint Online (SPO) or OneDrive for Business (ODfB) file URL through your bot flow and call `POST /api/sensitivity-label` (or proxy through `POST /api/messages`) to return the sensitivity label payload.

## Infrastructure baseline

Minimum infrastructure for production-style use:

1. API host (Azure App Service or container runtime).
2. Azure Bot Service for Teams messaging endpoint.
3. Microsoft Entra app registrations:
   - Teams/Bot app registration
   - API app registration
   - Upstream MIP SDK service app registration
4. Azure Key Vault for `MipIntegration:ClientSecret`.
5. Application Insights + Log Analytics for monitoring and alerting.

## CI/CD baseline

- Build and test on each PR (`dotnet test TeamsMIPPoC.slnx`).
- Environment-specific deployment (dev/test/prod) with environment variables.
- Smoke check for `/api/sensitivity-label` after deployment.
