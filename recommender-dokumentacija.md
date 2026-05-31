# ArenaBook — dokumentacija sistema preporuke

## Cilj

Sistem preporuke predlaže igračima dvorane koje najbolje odgovaraju njihovim interesima na osnovu prethodnih rezervacija i eksplicitnih ocjena (recenzija), u skladu s prijavom teme i algoritmom **Collaborative Filtering (CF)**.

## Tip algoritma

Korišten je **user-based collaborative filtering**:

1. Za svakog korisnika gradi se vektor ocjena po dvoranama (1–5 zvjezdica iz `HallReviews`).
2. Implicitni signal: sudjelovanje na terminu bez recenzije tretira se kao neutralna ocjena **3.5**.
3. Sličnost između korisnika računa se **kosinusnom sličnošću** na zajedničkim ocijenjenim dvoranama.
4. Za ciljnog korisnika predlažu se dvorane koje slični korisnici visoko ocjenjuju, a ciljni korisnik ih još nije ocijenio/rezervisao.
5. Svaka preporuka uključuje **objašnjenje** (`Explanation`) pogodno za prikaz u mobilnoj aplikaciji.

## Implementacija u backendu

| Komponenta | Putanja |
|------------|---------|
| Servis | `backend/ArenaBook.Infrastructure/Services/Recommendations/CollaborativeFilteringRecommendationService.cs` |
| API | `GET /api/me/recommendations/halls?cityId=&limit=` |
| Ugovor | `RecommendedHallResponse` (score, explanation, prosječna ocjena, broj recenzija) |

## Ulazni podaci

- Eksplicitne ocjene: tablica `HallReviews` (`RatingStars`, `Comment`, vezano na `ScheduledSessionId`).
- Implicitne: `ScheduledSessionParticipants` (rezervacije).

## Izlaz u mobilnoj aplikaciji

- Početni ekran: sekcija **Preporučeno za vas** (do 5 stavki).
- Ekran **Preporuke (CF)**: lista s proširenim objašnjenjem po stavci.
- Filtriranje po gradu (`cityId`) na API-ju.
- **Preporučeni termini**: `GET /api/me/recommendations/sessions` — termini u dvoranama iz CF skora, filtrirani na buduće CONFIRMED termine koje korisnik još nije rezervisao.

## Ograničenja i fallback

Ako u bazi nema dovoljno ocjena za CF, sistem vraća popularne dvorane sortirane po prosječnoj ocjeni i broju recenzija, s objašnjenjem da personalizacija još nije moguća.

## Poboljšanja (izvan trenutnog opsega)

- Item-based CF ili hibrid s content-based filtrima (oprema, lokacija).
- Matricna faktorizacija (SVD) za veće skupove podataka.
- Keširanje preporuka po korisniku i invalidacija nakon nove recenzije.
