# ArenaBook

Seminarski projekt: **backend** (.NET), **desktop** i **mobile** Flutter klijenti, **SQL Server** (Docker), RabbitMQ, worker servis.

## Struktura repozitorija (uvijek ovako)

| Folder | Sadržaj |
|--------|---------|
| **`backend/`** | Sav .NET kod zajedno: Web API, Worker, `ArenaBook.sln`, Dockerfajlovi za API i Worker. |
| **`desktop/`** | Flutter admin za Windows. |
| **`mobile/`** | Flutter aplikacija za igrače (Android). |
| Korijen | `docker-compose.yml`, `.env.example`, `PROJEKT_PODSJETNIK.md`, ovaj `README.md`. |

## Preduvjeti

- [.NET SDK 9](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (za `docker compose`)
- [Flutter SDK](https://docs.flutter.dev/get-started/install) (za `desktop/` i kasnije `mobile/`)

## Konfiguracija tajni

Tajne **ne** idu u `appsettings.json` ni u kod. Koristi `.env` u korijenu repozitorija (nije u Git-u).

1. (Preporučeno) Kopiraj `.env.example` u `.env` i postavi svoje lozinke i `SQLSERVER_DATABASE`.
2. **`SQLSERVER_SA_PASSWORD`** mora zadovoljiti [pravila jakosti](https://learn.microsoft.com/en-us/sql/relational-databases/security/password-policy) za SQL Server.
3. **`SQLSERVER_DATABASE`** — ime baze (prema uputama FIT obično broj indeksa bez prefiksa IB, npr. `210169`).

Ako **nemaš `.env`**, `docker-compose.yml` i dalje koristi **razvojne podrazumijevane vrijednosti** (`:-…` u fajlu) pa ne bi trebalo biti upozorenja „variable is not set”. Za predaju i stvarne tajne ipak koristi vlastiti `.env` i ne dijeli lozinke.

Connection string za API u kontejneru sastavlja Compose iz istih varijabli i hosta `sqlserver`.

## Docker (iz korijena repozitorija)

```powershell
docker compose up -d --build
```

- **API** — `http://localhost:${API_HTTP_PORT:-5000}` (npr. `/health`, **Swagger UI** na `/swagger`)
- **SQL Server** — port iz `SQLSERVER_PORT` (default **1433** na hostu)
- **RabbitMQ** — AMQP i management portovi iz `.env`

Zaustavljanje:

```powershell
docker compose down
```

Ako si prije koristio PostgreSQL volume iz starog compose-a, jednom očisti stare volume-e pa ponovo podigni stack.

## Backend lokalno (API na hostu, baza u Dockeru)

```powershell
cd backend
$env:ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=ArenaBook;User Id=sa;Password=ISTA_KAO_U_ENV;TrustServerCertificate=True;MultipleActiveResultSets=true"
dotnet run --project ArenaBook.Api
```

Lozinka i baza moraju odgovarati `.env`. Worker:

```powershell
$env:RabbitMQ__Host="localhost"
$env:RabbitMQ__Port="5672"
$env:RabbitMQ__UserName="arena"
$env:RabbitMQ__Password="ISTA_KAO_RABBITMQ_PASS_U_ENV"
$env:RabbitMQ__VirtualHost="/"
dotnet run --project ArenaBook.Worker
```

## Flutter desktop — `API_BASE_URL`

```powershell
cd desktop
flutter pub get
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5000
```

## Flutter mobile (Android)

```powershell
cd mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000
```

Za Android emulator: `http://10.0.2.2:5000`. Za desktop/Windows admin: `http://localhost:5000`. Detalji u [`mobile/README.md`](mobile/README.md) i [`recommender-dokumentacija.md`](recommender-dokumentacija.md).

## Korisnički podaci za predaju

Bit će u README kad seed i API budu spremni.
