# TeamsMIPPoC

Proof-of-concept Microsoft Teams backend built with .NET/C# to return a document sensitivity label for SharePoint Online (SPO) and OneDrive for Business (ODfB) links.

## What this provides

- ASP.NET Core API endpoint usable from a Teams bot/tab messaging flow.
- Endpoint accepts a file link copied from the SPO/ODfB file details pane.
- URL validation for supported Microsoft 365 business locations.
- Label response payload with a simulated MIP SDK result suitable for PoC/demo flows.

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
  "source": "MIP SDK PoC",
  "evidence": "Simulated lookup for Teams PoC. Replace with a real MIP SDK policy/label read."
}
```

## Run locally

Prerequisite: install the .NET 10 SDK (`net10.0`) before running or testing this project.

```bash
dotnet run --project TeamsMIPPoC.Api
```

## Test

```bash
dotnet test TeamsMIPPoC.slnx
```

## Deploy to a Microsoft 365 Developer tenant (Teams)

1. Create or use an existing [Microsoft 365 Developer Program](https://developer.microsoft.com/microsoft-365/dev-program) tenant.
2. Expose the local API publicly (for example with dev tunnels or ngrok) and note the HTTPS base URL.
3. In the [Teams Developer Portal](https://dev.teams.microsoft.com), create a new Teams app.
4. Add a bot capability to the app and set the bot messaging endpoint to your hosted API endpoint (for example `https://<public-host>/api/messages` if you add bot message handling, and use `POST /api/sensitivity-label` from your bot logic).
5. In **App package**, define valid domains for your API host and required permissions/scopes.
6. Install the app to your developer tenant and Teams client from Developer Portal (**Test and distribute** -> **Install**).
7. In Teams, send or paste a SharePoint Online (SPO) or OneDrive for Business (ODfB) file URL through your bot flow and call `POST /api/sensitivity-label` to return the sensitivity label payload.

> Note: This repository currently provides the backend label-lookup API PoC. A complete Teams app also requires bot message handling, app manifest metadata, and Azure Bot registration wiring.
>
> Security warning: this PoC API does not implement authentication/authorization. Do not expose it publicly unless you add protections such as Microsoft Entra ID auth, an API key, and/or IP restrictions.
