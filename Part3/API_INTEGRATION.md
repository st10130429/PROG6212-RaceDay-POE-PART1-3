# Part 3 API integration notes

The Part 3 additions have been copied into the existing Part 2 API project. They are no longer standalone sample files.

## Integrated additions

- `Models/Event.cs`: `EventType`, `DistanceKm`, `BannerUrl`.
- `Models/User.cs`: `ProfilePictureUrl`.
- `Models/Enrolment.cs`: `Status`, defaulting to `Pending`.
- `Data/RaceDayContext.cs`: decimal precision for event distance.
- `Controllers/Part3Controller.cs`: browse/detail, event metadata and banner upload, organiser dashboard, entrants/status, participant enrolments/results, profile and picture endpoints.
- `Services/BlobStorageService.cs`: validates image type/size and stores images in Azure Blob Storage.
- `DTOs/Part3Dtos.cs`: response/request models for the added endpoints.

## Database update

For an existing Part 2 database, back it up and run `Part3_Database_Upgrade.sql`. `EnsureCreated()` does not add new columns to an already-created database. For a new database, the updated model is included when the API creates the schema.

## Azure configuration

Set `AzureBlob:ConnectionString` using user-secrets or the `AzureBlob__ConnectionString` environment variable. Set `AzureBlob:Container` optionally. The API creates the container with blob-level public read access so returned image URLs can be rendered by the MVC site; the Azure account must permit that access level. The MVC project does not receive or store Azure credentials.

## Important behavior

The results endpoint now rejects result entry when the event date/time is still in the future. The exact event-end time is not modelled separately in the original Part 2 schema, so the existing `EventDate` value is used as the cutoff. If the lecturer expects a separate event end time, add an `EndDateTime` field and validate against it.
