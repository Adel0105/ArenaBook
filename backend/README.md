# Backend (.NET)

Jedan folder za sav serverski dio: **Web API** i **Worker** (RabbitMQ), zajedno u `ArenaBook.sln`.

- `ArenaBook.Api` — glavni REST API za desktop i mobilni klijent.
- `ArenaBook.Worker` — pozadinski servis (poruke iz RabbitMQ-a).

Iz ovog direktorija:

```powershell
dotnet build ArenaBook.sln
dotnet run --project ArenaBook.Api
dotnet run --project ArenaBook.Worker
```

Docker koristi `Dockerfile` unutar svakog projekta; kontekst builda je korijen repozitorija (`docker compose` iz korijena). Baza u compose-u je **SQL Server** (`sqlserver` servis); connection string dolazi iz `.env` / Compose okruženja, ne iz koda.
