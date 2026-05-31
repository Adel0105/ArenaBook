import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";
import {
  Document,
  Packer,
  Paragraph,
  TextRun,
  HeadingLevel,
  Table,
  TableRow,
  TableCell,
  WidthType,
  BorderStyle,
  AlignmentType,
  PageBreak,
} from "docx";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const desktop = path.join(process.env.USERPROFILE || "", "Desktop");
const outPath = path.join(desktop, "ArenaBook-Mobilna-App-Test-Plan.docx");

function h1(text) {
  return new Paragraph({ text, heading: HeadingLevel.HEADING_1, spacing: { before: 360, after: 200 } });
}
function h2(text) {
  return new Paragraph({ text, heading: HeadingLevel.HEADING_2, spacing: { before: 280, after: 160 } });
}
function h3(text) {
  return new Paragraph({ text, heading: HeadingLevel.HEADING_3, spacing: { before: 200, after: 120 } });
}
function p(text, opts = {}) {
  return new Paragraph({
    children: [new TextRun({ text, ...opts })],
    spacing: { after: 120 },
  });
}
function bullet(text) {
  return new Paragraph({ text, bullet: { level: 0 }, spacing: { after: 80 } });
}
function numbered(text) {
  return new Paragraph({ text, numbering: { reference: "steps", level: 0 }, spacing: { after: 80 } });
}

function checkTable(rows) {
  return new Table({
    width: { size: 100, type: WidthType.PERCENTAGE },
    rows: rows.map(
      (cells, i) =>
        new TableRow({
          children: cells.map(
            (text) =>
              new TableCell({
                children: [
                  new Paragraph({
                    children: [
                      new TextRun({
                        text: String(text),
                        bold: i === 0,
                        size: i === 0 ? 22 : 20,
                      }),
                    ],
                  }),
                ],
                borders: {
                  top: { style: BorderStyle.SINGLE, size: 1 },
                  bottom: { style: BorderStyle.SINGLE, size: 1 },
                  left: { style: BorderStyle.SINGLE, size: 1 },
                  right: { style: BorderStyle.SINGLE, size: 1 },
                },
              })
          ),
        })
    ),
  });
}

const doc = new Document({
  numbering: {
    config: [
      {
        reference: "steps",
        levels: [{ level: 0, format: "decimal", text: "%1.", alignment: AlignmentType.START }],
      },
    ],
  },
  sections: [
    {
      properties: {},
      children: [
        new Paragraph({
          alignment: AlignmentType.CENTER,
          spacing: { after: 400 },
          children: [
            new TextRun({ text: "ArenaBook", bold: true, size: 48, color: "16A34A" }),
          ],
        }),
        new Paragraph({
          alignment: AlignmentType.CENTER,
          spacing: { after: 200 },
          children: [new TextRun({ text: "Plan testiranja mobilne aplikacije (Android)", bold: true, size: 32 })],
        }),
        p("Verzija dokumenta: 1.0"),
        p("Datum testiranja: _________________________"),
        p("Tester: _________________________"),
        p("API URL na telefonu: http://_________________:5000"),
        p("SEED_USERS_PASSWORD (iz .env): _________________________"),
        new Paragraph({ children: [new PageBreak()] }),

        h1("Sadržaj"),
        ...[
          "1. Priprema okruženja",
          "2. Demo korisnici",
          "3. Prijava i račun",
          "4. Tab Početna",
          "5. Tab Dvorane",
          "6. Tab Termini",
          "7. Pridruživanje i novčanik",
          "8. Tab Profil",
          "9. Organizator",
          "10. Recenzije dvorana",
          "11. Historija i notifikacije",
          "12. Sistem preporuke (CF)",
          "13. Redoslijed testiranja u jednom danu",
          "14. Česte greške",
          "15. Zapisnik (checklist)",
        ].map((t) => bullet(t)),

        new Paragraph({ children: [new PageBreak()] }),
        h1("1. Priprema okruženja"),

        h2("1.1 Backend"),
        checkTable([
          ["Korak", "Akcija", "✓"],
          ["1", "Kopirati .env.example → .env u korijenu ArenaBook repoa", "☐"],
          ["2", "Provjeriti API_HTTP_PORT=5000", "☐"],
          ["3", "Provjeriti SEED_USERS_PASSWORD i zapamtiti lozinku", "☐"],
          ["4", "Provjeriti SEED_RUN_DEMO_DATA_ON_STARTUP=true", "☐"],
          ["5", "Pokrenuti: docker compose up -d --build", "☐"],
          ["6", "Browser: http://localhost:5000/health → Healthy", "☐"],
          ["7", "(Opciono) Swagger: http://localhost:5000/swagger", "☐"],
        ]),
        p(" "),

        h2("1.2 Mobilna aplikacija"),
        checkTable([
          ["Korak", "Akcija", "✓"],
          ["1", "Telefon i PC na istom Wi‑Fi (ne mobilni podaci)", "☐"],
          ["2", "ipconfig → zapisati IPv4 računala", "☐"],
          ["3", "Na telefonu Chrome: http://[IP]:5000/health mora raditi", "☐"],
          ["4", "USB debugging uključen; Samsung Automatski blokator isključen", "☐"],
          ["5", "flutter devices vidi telefon", "☐"],
          ["6", "flutter run -d [ID] --dart-define=API_BASE_URL=http://[IP]:5000", "☐"],
        ]),

        h1("2. Demo korisnici"),
        p("Lozinka za sve seed račune = vrijednost SEED_USERS_PASSWORD iz .env datoteke."),
        checkTable([
          ["Uloga", "E-mail", "Namjena"],
          ["Igrač", "amir.hadzic@arena.local", "Glavni testovi"],
          ["Organizator", "tarik.selimovic@arena.local", "Kreiranje i potvrda termina"],
          ["Drugi igrač", "dino.basic@arena.local", "Uporedba preporuka (CF)"],
          ["Admin", "admin@arena.local", "Samo desktop admin (ne mobilna app)"],
        ]),

        h1("3. Prijava i račun"),
        h2("3.1 Login"),
        numbered("Otvoriti app → ekran prijave (logo ArenaBook, gradijent pozadina)."),
        numbered("E-mail: amir.hadzic@arena.local"),
        numbered("Lozinka: iz .env → Prijavi se."),
        numbered("Očekivano: ulaz u app, donja navigacija (Početna, Dvorane, Termini, Novčići, Profil)."),
        checkTable([["Test", "✓"], ["Login uspješan", "☐"]]),

        h2("3.2 Odjava"),
        numbered("Tab Profil → ikona odjave (gore desno)."),
        numbered("Očekivano: povratak na login ekran."),
        checkTable([["Test", "✓"], ["Odjava", "☐"]]),

        h2("3.3 Registracija"),
        numbered("Login → Registracija."),
        numbered("Popuniti ime, prezime, e-mail, datum rođenja (obavezno), grad, lozinku."),
        numbered("Registruj se → očekivano: ulaz u app."),
        numbered("Ponoviti bez datuma rođenja → očekivano: greška."),
        checkTable([
          ["Test", "✓"],
          ["Registracija s DOB", "☐"],
          ["Registracija bez DOB odbijena", "☐"],
        ]),

        h2("3.4 Zaboravljena lozinka"),
        numbered("Zaboravljena lozinka → unijeti e-mail → Pošalji."),
        numbered("Development: development token na ekranu (ako API vrati)."),
        numbered("Unesi novu lozinku → login s novom lozinkom."),
        checkTable([["Test", "✓"], ["Reset lozinke", "☐"]]),

        h1("4. Tab Početna"),
        checkTable([
          ["Korak", "Akcija", "Očekivano", "✓"],
          ["1", "Pregled sekcija", "Brze radnje, Preporučene dvorane, Preporučeni termini, Javni termini, Nadolazeći", "☐"],
          ["2", "Pull-to-refresh", "Podaci se osvježe", "☐"],
          ["3", "Zvono (notifikacije)", "Lista; badge ako nepročitano", "☐"],
          ["4", "Vidi sve (preporuke)", "Ekran Preporuke", "☐"],
          ["5", "Tap javnog termina", "Ekran plaćanja novčićima", "☐"],
          ["6", "FAB Kreiraj termin", "Forma novog termina", "☐"],
        ]),

        h1("5. Tab Dvorane"),
        checkTable([
          ["Korak", "Akcija", "Očekivano", "✓"],
          ["1", "Pretraga po nazivu", "Lista se sužava", "☐"],
          ["2", "Filter po gradu (npr. Sarajevo)", "Samo dvorane u gradu", "☐"],
          ["3", "Detalj dvorane", "Info, oprema, fotografije, termini, recenzije", "☐"],
          ["4", "Tap CONFIRMED termina", "Join ekran", "☐"],
          ["5", "Cijene u novčićima", "Prikaz novčić/h", "☐"],
        ]),

        h1("6. Tab Termini (kalendar)"),
        checkTable([
          ["Korak", "Akcija", "Očekivano", "✓"],
          ["1", "Filter Svi / PUBLIC / INVITE", "Lista se mijenja", "☐"],
          ["2", "Odabir dana na kalendaru", "Termini za taj dan", "☐"],
          ["3", "Banner podsjetnika 48h", "Prikaz ako imaš termin uskoro", "☐"],
          ["4", "Tap termina", "Plaćanje novčićima", "☐"],
        ]),

        h1("7. Pridruživanje i novčanik"),
        h2("7.1 Uspješno pridruživanje"),
        numbered("Login Amir → tab Novčići → zapamtiti stanje."),
        numbered("Naći CONFIRMED javni termin (Početna / Dvorane / Termini)."),
        numbered("Čekirati Prihvatam uslove → Plati X novčića."),
        numbered("Očekivano: dijalog Rezervacija uspješna."),
        numbered("Profil: statistika +1; Novčići: stanje smanjeno."),
        checkTable([["Test", "✓"], ["Join + plaćanje", "☐"]]),

        h2("7.2 Nedovoljno novčića"),
        numbered("Potrošiti novčiće ili koristiti račun s malim stanjem."),
        numbered("Join na skup termin → Plati onemogućen ili greška."),
        numbered("Dokupi novčiće → tab Novčići."),
        checkTable([["Test", "✓"], ["Nedovoljno novčića", "☐"]]),

        h2("7.3 Privatni termin (INVITE)"),
        numbered("Login Tarik → kreiraj INVITE termin, zapiši kod."),
        numbered("Potvrdi termin (PENDING → CONFIRMED)."),
        numbered("Login Amir → join → unijeti kod poziva → platiti."),
        checkTable([["Test", "✓"], ["INVITE + kod", "☐"]]),

        h2("7.4 Kupovina — Stripe (sandbox)"),
        numbered("Novčići → broj novčića → kartica, MM/GG, CVC → uslove → Plati karticom."),
        numbered("Potreban STRIPE_SECRET_KEY u .env i Development okruženje."),
        checkTable([["Test", "✓"], ["Stripe sandbox", "☐"]]),

        h2("7.5 Kupovina — PayPal"),
        numbered("Uključiti PayPal → Nastavi na PayPal → browser."),
        numbered("Vratiti se → Potvrdi PayPal uplatu."),
        checkTable([["Test", "✓"], ["PayPal capture", "☐"]]),

        h1("8. Tab Profil"),
        checkTable([
          ["Korak", "Akcija", "Očekivano", "✓"],
          ["1", "Sekcija Račun", "E-mail, ime, stanje novčanika", "☐"],
          ["2", "Historija rezervacija", "Tabovi Termini i Novčići", "☐"],
          ["3", "Statistika igranja", "Brojevi rezervacija, potrošnje", "☐"],
          ["4", "Uredi profil → Spremi", "Poruka uspjeha", "☐"],
          ["5", "Promjena lozinke", "Login s novom lozinkom", "☐"],
        ]),

        h1("9. Organizator (tarik.selimovic@arena.local)"),
        checkTable([
          ["Korak", "Akcija", "Očekivano", "✓"],
          ["1", "FAB ili Upravljaj mojim terminima → Kreiraj", "Termin PENDING", "☐"],
          ["2", "Potvrdi termin", "CONFIRMED — drugi mogu joinati", "☐"],
          ["3", "Uredi termin", "Promjene sačuvane", "☐"],
          ["4", "Otkaži termin", "Otkazan (cancel)", "☐"],
          ["5", "Završi termin", "COMPLETED (kad dozvoljeno)", "☐"],
        ]),

        h1("10. Recenzije dvorana"),
        numbered("Amir join na CONFIRMED termin."),
        numbered("Tarik → Završi termin."),
        numbered("Amir → Početna → Ocijenite dvoranu → zvjezdice + komentar."),
        numbered("Detalj dvorane → Recenzije — nova recenzija vidljiva."),
        p("Napomena: Recenzije su ulaz za sistem preporuke (CF)."),
        checkTable([["Test", "✓"], ["Recenzija nakon COMPLETED", "☐"]]),

        h1("11. Historija i notifikacije"),
        checkTable([
          ["Funkcija", "Gdje", "Provjera", "✓"],
          ["Moje rezervacije", "Profil → Historija → Termini", "Datum, status, novčići", "☐"],
          ["Ledger", "Historija → Novčići", "Uplate i terećenja", "☐"],
          ["Notifikacije", "Početna → zvono", "Lista, pročitano", "☐"],
        ]),

        new Paragraph({ children: [new PageBreak()] }),
        h1("12. Sistem preporuke (Collaborative Filtering)"),

        h2("12.1 Teorija (za odbranu)"),
        bullet("Tip: user-based collaborative filtering."),
        bullet("Eksplicitne ocjene: recenzije dvorana (1–5 zvjezdica)."),
        bullet("Implicitne: sudjelovanje na terminu bez recenzije = ocjena 3.5."),
        bullet("Sličnost korisnika: kosinusna sličnost."),
        bullet("Fallback: popularne dvorane ako nema dovoljno podataka."),
        bullet("Dokumentacija u repou: recommender-dokumentacija.md"),
        bullet("API: GET /api/me/recommendations/halls i /sessions"),

        h2("12.2 Gdje u aplikaciji"),
        checkTable([
          ["Mjesto", "Šta testirati"],
          ["Početna", "Preporučene dvorane (do 5), Preporučeni termini"],
          ["Preporuke (Vidi sve)", "Tab Dvorane / Termini, filter grada, objašnjenje (Explanation)"],
        ]),

        h2("12.3 Test A — Cold start (nov korisnik)"),
        numbered("Registrirati novog korisnika bez aktivnosti."),
        numbered("Početna → Preporučene dvorane."),
        numbered("Očekivano: popularne dvorane; objašnjenje bez personalizacije."),
        checkTable([["Test", "✓"], ["Cold start", "☐"]]),

        h2("12.4 Test B — Seed korisnik (Amir)"),
        numbered("Login amir.hadzic@arena.local."),
        numbered("Preporuke → tab Dvorane → proširiti stavku → pročitati Explanation."),
        numbered("Filter grad (npr. Sarajevo) → lista se sužava."),
        numbered("Tab Termini → preporučeni budući CONFIRMED termini."),
        checkTable([["Test", "✓"], ["Seed preporuke", "☐"]]),

        h2("12.5 Test C — Dva profila"),
        numbered("Zapisati top 3 preporučene dvorane za Amir."),
        numbered("Login dino.basic@arena.local → ponovo Preporuke."),
        numbered("Očekivano: lista može biti drugačija (različita historija)."),
        checkTable([["Test", "✓"], ["Različiti korisnici", "☐"]]),

        h2("12.6 Test D — Utjecaj aktivnosti"),
        numbered("Snimiti preporuke PRIJE aktivnosti."),
        numbered("Završiti termin u dvorani X → ocjena 5 zvjezdica."),
        numbered("Završiti termin u dvorani Y → ocjena 1 zvijezda."),
        numbered("Refresh / ponovo otvoriti Preporuke."),
        numbered("Očekivano: promjena skora/objašnjenja (kad ima dovoljno podataka u bazi)."),
        checkTable([["Test", "✓"], ["Utjecaj recenzija", "☐"]]),

        h2("12.7 Test E — API (Swagger, opciono)"),
        bullet("GET /api/me/recommendations/halls?limit=10"),
        bullet("GET /api/me/recommendations/sessions?limit=10"),
        bullet("GET /api/me/recommendations/halls?cityId=1"),
        checkTable([["Test", "✓"], ["API preporuke", "☐"]]),

        h1("13. Redoslijed testiranja u jednom danu"),
        ...[
          "Priprema Docker + flutter run",
          "Login Amir",
          "Početna (sekcije, refresh, notifikacije)",
          "Preporuke CF (test B, C)",
          "Dvorane (pretraga, filter, detalj)",
          "Termini (kalendar, filter)",
          "Join + plaćanje novčićima",
          "Novčići (Stripe, opciono PayPal)",
          "Profil + historija",
          "Login Tarik → kreiraj → potvrdi → Amir join",
          "Završi termin → recenzija",
          "Preporuke CF ponovo (test D)",
          "Registracija + zaboravljena lozinka",
          "Novi korisnik CF cold start (test A)",
        ].map((t, i) => p(`${i + 1}. ${t}`)),

        h1("14. Česte greške"),
        checkTable([
          ["Problem", "Uzrok"],
          ["Ne mogu joinati", "Termin je PENDING — organizator mora Potvrdi"],
          ["Join odbijen (dob)", "Nema datuma rođenja u profilu"],
          ["Prazne liste", "Seed nije prošao — docker compose logs api ili POST /api/dev/seed-demo-data"],
          ["Preporuke uvijek iste", "Malo podataka — uraditi test D (recenzije)"],
          ["Stripe ne radi", "STRIPE_SECRET_KEY u .env, Development"],
          ["Connection error", "Krivi API_BASE_URL ili API nije gore"],
          ["not authorized (USB)", "Dopusti USB debugging na telefonu"],
        ]),

        h1("15. Zapisnik testiranja"),
        p("Ukupno testova: _____  |  Prošlo: _____  |  Palо: _____"),
        p("Kritični bugovi:"),
        p("_______________________________________________________________________________"),
        p("_______________________________________________________________________________"),
        p("Napomene:"),
        p("_______________________________________________________________________________"),
        p("_______________________________________________________________________________"),

        new Paragraph({
          alignment: AlignmentType.CENTER,
          spacing: { before: 400 },
          children: [
            new TextRun({
              text: "ArenaBook — mobilni klijent | Test plan",
              italics: true,
              size: 20,
              color: "64748B",
            }),
          ],
        }),
      ],
    },
  ],
});

const buffer = await Packer.toBuffer(doc);
fs.writeFileSync(outPath, buffer);
console.log("Created:", outPath);
