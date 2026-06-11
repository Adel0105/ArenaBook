# ArenaBook — mobilni klijent (Android)

Flutter aplikacija za igrače: registracija, profil, dvorane, kalendar termina, koin shop (Stripe/PayPal sandbox), recenzije i preporuke (CF).

## Preduvjeti

- Flutter SDK (Android toolchain)
- Pokrenut backend (`docker compose up` ili lokalni API)

## Konfiguracija API-ja

URL se prosljeđuje preko `dart-define` (bez hardkoda u kodu):

```powershell
cd mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000 --dart-define=STRIPE_PUBLISHABLE_KEY=pk_test_...
```

| Okruženje | Tipičan `API_BASE_URL` |
|-----------|-------------------------|
| Android emulator | `http://10.0.2.2:5000` |
| Fizički telefon (isti Wi‑Fi) | `http://<IP_računara>:5000` |
| API u Dockeru na hostu | port iz `.env` (`API_HTTP_PORT`, npr. 5000) |

Podrazumijevana vrijednost u kodu (ako nema `dart-define`) je `http://10.0.2.2:5000` za emulator.

## Android — cleartext HTTP (razvoj)

Za lokalni API bez HTTPS-a, u `android/app/src/main/AndroidManifest.xml` na `<application>` dodan je `android:usesCleartextTraffic="true"` (samo za razvoj/test).

## Glavni tokovi

1. **Registracija** — ime, prezime, e-mail, **obavezan datum rođenja**, lozinka.
2. **Dvorane** — pretraga, filter po gradu, detalj, termini, recenzije.
3. **Termini** — kalendar, pridruživanje (javni/privatni + kod), plaćanje koinima.
4. **Koini** — Stripe **PaymentSheet** (`flutter_stripe`) + `POST /complete` nakon uspjeha; PayPal otvara **approval URL** u pregledniku i vraća se u app preko deep linka `arenabook://paypal/return`, zatim `capture`.
5. **Preporuke** — CF na početnoj i posebnom ekranu s objašnjenjem.
6. **Profil** — uređivanje, promjena lozinke, historija rezervacija.
7. **Reset lozinke** — forgot/reset. U produkciji token stiže e-mailom (SMTP + Worker). U Development bez SMTP-a API vraća `resetToken` u odgovoru za lokalni test.

## Test korisnici

Nakon `POST /api/dev/seed-demo-data` (Development): demo igrači iz seeda, lozinka iz `SEED_USERS_PASSWORD` u `.env`.

## Dokumentacija preporuke

Vidi [`recommender-dokumentacija.md`](../recommender-dokumentacija.md) u korijenu repozitorija.

## Build APK (predaja)

```powershell
flutter build apk --dart-define=API_BASE_URL=http://10.0.2.2:5000
```

Izlaz: `build/app/outputs/flutter-apk/app-release.apk`
