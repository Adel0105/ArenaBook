# ArenaBook — dokumentacija sistema preporuke

## Cilj

Sistem preporuke predlaže igračima **dvorane** i **termine** u njihovom gradu na osnovu **agregiranog angažmana** zajednice: recenzija (zvjezdice), komentara i lajkova/dislajkova na dvorane. Svaka stavka uključuje **objašnjenje** (`Explanation`) pogodno za prikaz u mobilnoj aplikaciji.

Algoritam **nije** klasični user-based collaborative filtering (nema matrice korisnik–dvorana niti kosinusne sličnosti između korisnika). Rangiranje je **globalno po gradu** — isti skor za sve korisnike iz istog grada, filtrirano prema gradu iz profila korisnika.

> Implementacija je u klasi `CollaborativeFilteringRecommendationService` (historijski naziv servisa); logika u kodu je **engagement scoring**.

## Tip algoritma — bodovanje angažmana

Za svaku aktivnu dvoranu u odabranom gradu računaju se agregati iz baze, zatim **engagement score**:

| Signal | Izvor | Bod po jedinici |
|--------|--------|-----------------|
| Lajk | `HallReactions` (`IsLike = true`) | **+10** |
| Dislajk | `HallReactions` (`IsLike = false`) | **−10** |
| Zvjezdice | `HallReviews.RatingStars` (suma 1–5) | **+1** po zvjezdici |
| Komentar | `HallReviews` s ne-praznim `Comment` | **+1** po komentaru |

**Formula:**

```
score = (lajkovi × 10) + (dislajkovi × −10) + (suma_zvjezdica × 1) + (broj_komentara × 1)
```

Dvorane se sortiraju **opadajuće** po `score`, zatim po broju recenzija, zatim po nazivu.

Prosječna ocjena (`AverageRating`) u odgovoru je informativna: `suma_zvjezdica / broj_recenzija` (0 ako nema recenzija).

## Implementacija u backendu

| Komponenta | Putanja |
|------------|---------|
| Servis | `backend/ArenaBook.Infrastructure/Services/Recommendations/CollaborativeFilteringRecommendationService.cs` |
| API — dvorane | `GET /api/me/recommendations/halls?cityId=&limit=` |
| API — termini | `GET /api/me/recommendations/sessions?cityId=&limit=` |
| Ugovori | `RecommendedHallResponse`, `RecommendedSessionResponse` |

### Odabir grada

1. Ako korisnik ima `CityId` u profilu → koristi se taj grad.
2. Inače, opcionalni query parametar `cityId`.
3. Ako grad nije poznat → prazna lista (mobilna aplikacija traži postavljanje grada u profilu).

### Preporučene dvorane

- Učitavaju se sve **aktivne** dvorane u gradu s agregatima recenzija i reakcija.
- Za svaku dvoranu izračunava se `score` i generira se `Explanation`, npr.:
  *„Dvorana u gradu Sarajevo. Bodovi: 42 (👍 2×10, 👎 0×-10, ★ 12×1, 💬 2×1).“*

### Preporučeni termini

- Uzimaju se budući termini u statusu **CONFIRMED** u istom gradu, u dvoranama koje korisnik **još nije** rezervisao.
- Skor termina = engagement score **dvorane** u kojoj se termin održava.
- Sortiranje: opadajuće po skoru dvorane, zatim rastuće po vremenu početka.
- Ako dvorana nema bodova, objašnjenje i dalje opisuje termin bez naglašavanja visokog skora.

## Ulazni podaci

| Tablica | Polja / značenje |
|---------|------------------|
| `HallReviews` | `RatingStars` (1–5), opcionalni `Comment` |
| `HallReactions` | `IsLike` (lajk / dislajk), vezano na korisnika i dvoranu |
| `Halls` | `CityId`, `IsActive`, cijena |
| `ScheduledSessions` | budući CONFIRMED termini |
| `ScheduledSessionParticipants` | isključuju već rezervisane termine |
| `Users` | `CityId` profila |

Demo seed (`DemoDataSeedService`) puni recenzije i lajkove radi realističnih preporuka pri testiranju.

## Izlaz u mobilnoj aplikaciji

| Mjesto | Ponašanje |
|--------|-----------|
| Početni ekran | Sekcije **Preporučeno za vas** (do 5 dvorana) i preporučeni termini (do 5) |
| Ekran **Preporuke** | Puna lista dvorana i termina s `Explanation` po stavci |
| Tekst u UI | *„Rangirano po ocjenama, komentarima i lajkovima.“* |

API pozivi: `GET /api/me/recommendations/halls` i `GET /api/me/recommendations/sessions` (parametar `limit`).

## Ograničenja

- Nema personalizacije po **povijesti pojedinačnog korisnika** — samo grad iz profila i globalni angažman dvorana u tom gradu.
- Prazan rezultat ako korisnik nema grad u profilu i ne pošalje `cityId`.
- Dislajkovi snižavaju score; dvorana bez aktivnosti ima score 0.

## Moguća poboljšanja (izvan trenutnog opsega)

- User-based ili item-based collaborative filtering (matrica korisnik–dvorana).
- Personalizacija prema vlastitim rezervacijama i recenzijama korisnika.
- Hibrid s content-based filtrima (oprema dvorane, udaljenost, cijena).
- Keširanje rang-listi po gradu i invalidacija nakon nove recenzije ili reakcije.
