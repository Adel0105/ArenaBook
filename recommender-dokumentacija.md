# ArenaBook — dokumentacija sistema preporuke

## Cilj

Sistem preporuke predlaže igračima **dvorane** i **termine** u odabranom gradu koristeći **user-based collaborative filtering (CF)** nad poviješću ponašanja zajednice: rezervacija termina, recenzija (zvjezdice) i lajkova/dislajkova na dvorane. Svaka stavka uključuje **objašnjenje** (`Explanation`) pogodno za prikaz u mobilnoj aplikaciji.

## Tip algoritma — user-based collaborative filtering

### Matrica korisnik–dvorana

Za svaki par (korisnik, dvorana) u gradu gradi se **preferenca** (skala ~1–5) iz najjačeg dostupnog signala:

| Signal | Izvor | Težina preferencije |
|--------|--------|---------------------|
| Recenzija | `HallReviews.RatingStars` | 1–5 (direktno) |
| Lajk | `HallReactions` (`IsLike = true`) | **5** |
| Dislajk | `HallReactions` (`IsLike = false`) | **1** |
| Rezervacija / sudjelovanje | `ScheduledSessionParticipants` → dvorana termina | **4** |

Ako korisnik ima više signala za istu dvoranu, uzima se **maksimum** (najjači signal).

### Sličnost korisnika

Između aktivnog korisnika i ostalih korisnika računa se **kosinusna sličnost** nad vektorima preferencija (user–item matrica). U susjedstvo ulazi do **15** najsličnijih korisnika (prag sličnosti ≥ 0,05).

### Predviđanje ocjene dvorane

Za dvorane koje korisnik još nije ocijenio / rezervisao / reagirao:

```
predviđena_ocjena(h) = Σ sim(u,v) × pref(v,h) / Σ sim(u,v)
```

gdje je suma preko susjeda `v` koji imaju preferenciju za dvoranu `h`.

### Hibridni finalni skor

Finalni skor spaja CF predviđanje i **popularnost u gradu** (engagement score zajednice):

```
final = w × CF_pred + (1 − w) × popularnost_norm
```

Težina `w` raste s brojem sličnih korisnika (0,25–0,85). Ako korisnik nema povijest ili nema susjeda, koristi se samo popularnost (cold start).

### Popularnost u gradu (fallback / blend)

Za normalizaciju popularnosti koristi se engagement score po dvorani:

| Signal | Bod po jedinici |
|--------|-----------------|
| Lajk | **+10** |
| Dislajk | **−10** |
| Zvjezdice (suma) | **+1** |
| Komentar | **+1** |

Popularnost se skalira na raspon 0–5 unutar grada.

## Implementacija u backendu

| Komponenta | Putanja |
|------------|---------|
| CF engine | `CollaborativeFilteringEngine.cs` |
| Servis | `CollaborativeFilteringRecommendationService.cs` |
| API — dvorane | `GET /api/me/recommendations/halls?cityId=&limit=` |
| API — termini | `GET /api/me/recommendations/sessions?cityId=&limit=` |

### Odabir grada

1. Ako je poslan query parametar **`cityId`** i grad postoji → **koristi se taj grad** (prioritet nad profilom).
2. Inače grad iz profila korisnika (`Users.CityId`).
3. Ako grad nije poznat → prazna lista.

### Preporučene dvorane

- Aktivne dvorane u odabranom gradu.
- CF predviđanje + hibrid s popularnošću.
- Sortiranje: opadajuće po `Score`, zatim broj recenzija, zatim naziv.

### Preporučeni termini

- Budući **CONFIRMED** termini u gradu, u dvoranama koje korisnik **još nije** rezervisao.
- Skor termina = hibridni CF skor **dvorane**.
- Sortiranje: opadajuće po skoru, zatim rastuće po vremenu početka.

## Ulazni podaci

| Tablica | Značenje u CF |
|---------|----------------|
| `HallReviews` | Direktna ocjena dvorane |
| `HallReactions` | Lajk / dislajk |
| `ScheduledSessionParticipants` | Implicitna pozitivna povratna informacija (igrao u dvorani) |
| `Halls` | Filtar po gradu, `IsActive` |
| `ScheduledSessions` | Budući termini za preporuku |
| `Users` | `CityId` profila |

## Izlaz u mobilnoj aplikaciji

| Mjesto | Ponašanje |
|--------|-----------|
| Početni ekran | Sekcije **Preporučeno za vas** (do 5 dvorana) i preporučeni termini (do 5) |
| Ekran **Preporuke** | Puna lista s `Explanation` po stavci |
| API | `cityId` opcionalno — ima prioritet nad gradom u profilu |

## Ograničenja

- CF zahtijeva dovoljno preklapanja u ponašanju korisnika; na malom broju korisnika dominira popularnost.
- Sličnost se računa unutar interakcija vezanih za dvorane u odabranom gradu.
- Prazan rezultat ako nije poznat grad (ni parametar ni profil).

## Moguća poboljšanja

- Item-based CF ili matrix factorization.
- Keširanje matrice preferencija po gradu.
- Dodatni signali (cijena, oprema, udaljenost).
