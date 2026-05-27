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

```bash
dotnet run --project TeamsMIPPoC.Api
```

## Test

```bash
dotnet test TeamsMIPPoC.slnx
```
