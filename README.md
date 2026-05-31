# ArenaBook

**ArenaBook** je seminarski sustav za rezervaciju sportskih dvorana i termina. Projekt obuhvaća **.NET Web API** i worker, **Flutter** admin klijent (Windows) i mobilnu aplikaciju (Android), te infrastrukturu u **Dockeru** (SQL Server, RabbitMQ).

**Autor:** Adel Čomor (IB210169)

---

## Sadržaj repozitorija

| Putanja | Opis |
|---------|------|
| [`backend/`](backend/) | .NET rješenje: Web API, Worker, migracije, poslovna logika |
| [`desktop/`](desktop/) | Flutter admin aplikacija (Windows) |
| [`mobile/`](mobile/) | Flutter mobilna aplikacija (Android) |
| [`docker-compose.yml`](docker-compose.yml) | SQL Server, RabbitMQ, API i Worker |
| [`.env.example`](.env.example) | Uzorak varijabli okruženja (bez tajni) |
| [`.env-tajne.zip`](.env-tajne.zip) | Šifrirani `.env` za ocjenjivanje (vidi dolje) |
| [`recommender-dokumentacija.md`](recommender-dokumentacija.md) | Dokumentacija sustava preporuke |

---

## Preduvjeti

- [.NET SDK 9](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Flutter SDK](https://docs.flutter.dev/get-started/install) (za lokalni razvoj klijenata)

---

## Brzo pokretanje (Docker)

1. Raspakuj `.env` iz [`.env-tajne.zip`](.env-tajne.zip) u **korijen** repozitorija (pored `docker-compose.yml`).
2. Iz korijena repozitorija:

```powershell
docker compose up -d --build
```

3. Provjera:
   - API: `http://localhost:5000/health`
   - Swagger: `http://localhost:5000/swagger`

Zaustavljanje stacka:

```powershell
docker compose down
```

Pri prvom pokretanju s novom bazom API primjenjuje migracije i, ako je u `.env` uključeno, puni demo podatke.

---

## Konfiguracija (`.env`)

Tajne se **ne** čuvaju u kodu ni u `appsettings.json`. Koristi se datoteka `.env` u korijenu projekta.

Za ocjenjivanje dostupan je arhiv **[`.env-tajne.zip`](.env-tajne.zip)** u korijenu repozitorija:

| Stavka | Vrijednost |
|--------|------------|
| **Lozinka arhive** | `OvoJeSifra1!` |
| **Sadržaj** | datoteka `.env` (SQL Server, JWT, RabbitMQ, Stripe/PayPal sandbox, seed) |

Nakon raspakivanja `.env` u korijen pokreni `docker compose up -d --build` kao gore.

Za vlastiti razvoj kopiraj [`.env.example`](.env.example) u `.env` i postavi vrijednosti. Važne varijable:

- **`SQLSERVER_DATABASE`** — ime baze (FIT: broj indeksa bez prefiksa IB, npr. `210169`)
- **`SQLSERVER_SA_PASSWORD`** — mora zadovoljiti [SQL Server pravila lozinke](https://learn.microsoft.com/en-us/sql/relational-databases/security/password-policy)
- **`SEED_USERS_PASSWORD`** — lozinka za sve demo korisnike u bazi
- **`SEED_RUN_DEMO_DATA_ON_STARTUP`** — `true` za automatski demo seed pri startu API-ja

---

## Korisnici za testiranje

Nakon uspješnog seeda (automatski pri Docker startu, ako je konfigurirano u `.env`):

| Uloga | E-mail | Lozinka | Klijent |
|-------|--------|---------|---------|
| Administrator | `admin@arena.local` | `OvoJeSifra1!` | Desktop (Windows) |
| Demo igrač | `amir.hadzic@arena.local` | `OvoJeSifra1!` | Mobile (Android) |
| Demo organizator | `tarik.selimovic@arena.local` | `OvoJeSifra1!` | Mobile ili desktop |

Lozinka odgovara vrijednosti **`SEED_USERS_PASSWORD`** u `.env`. Ostali demo računi: `ime.prezime@arena.local` (npr. `dino.basic@arena.local`).

U Development okruženju demo podatke je moguće ponovo učitati: `POST /api/dev/seed-demo-data` (Swagger).

---

## Izdanje aplikacija (GitHub Releases)

Gotovi buildovi za predaju nalaze se na kartici **Releases** ovog repozitorija:

| Arhiva | Sadržaj |
|--------|---------|
| **fit-build-mobile.zip** | `app-release.apk` (Android) |
| **fit-build-desktop.zip** | Windows `Release` (pokretanje: `arena_book_desktop.exe`) |

**Mobile (emulator):** API na hostu — `http://10.0.2.2:5000` (ugrađeno u release build).

**Desktop:** API na hostu — `http://localhost:5000` (ugrađeno u release build).

Detalji za mobilni klijent: [`mobile/README.md`](mobile/README.md).

---

## Lokalni razvoj (opcionalno)

### Backend na hostu (baza u Dockeru)

```powershell
cd backend
$env:ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=210169;User Id=sa;Password=IZ_ENV;TrustServerCertificate=True;MultipleActiveResultSets=true"
dotnet run --project ArenaBook.Api
```

### Flutter desktop

```powershell
cd desktop
flutter pub get
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5000
```

### Flutter mobile

```powershell
cd mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000
```

---

## Dokumentacija

- [Sustav preporuke](recommender-dokumentacija.md)
- [Backend](backend/README.md)
- [Mobile klijent](mobile/README.md)
