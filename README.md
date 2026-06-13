# ZakupOgarniacz

Serwis **ASP.NET Core** do **automatyzacji zakupów spożywczych online**. Rdzeniem jest
zbudowanie koszyka z katalogu sklepu i możliwie daleko posunięta automatyzacja drogi do
złożenia zamówienia — od listy/koszyka, przez eksport/deep-link, po (docelowo) złożenie
zamówienia za użytkownika.

Pierwszy cel: **Carrefour.pl**. Architektura jest **sklep-agnostyczna** (adapter pattern),
więc kolejne sklepy dochodzą za tymi samymi interfejsami.

<!-- Badge do uzupełnienia po podpięciu CI:
[![build](https://img.shields.io/badge/build-todo-lightgrey)]()
[![tests](https://img.shields.io/badge/tests-todo-lightgrey)]()
[![coverage](https://img.shields.io/badge/coverage-todo-lightgrey)]()
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)]()
[![license](https://img.shields.io/badge/license-todo-lightgrey)]()
-->

## Cel i zakres

- **Katalog sklepu (odczyt):** wyszukiwanie produktów z **cenami i dostępnością**.
- **Koszyk:** budowanie koszyka po naszej stronie (pozycje, ilości, podsumowanie ceny).
- **Zamawianie — etapowo:**
  1. **Eksport koszyka / deep-link** do sklepu (finalne „zamów" klika człowiek).
  2. **Docelowo:** automatyczne dodanie do koszyka i złożenie zamówienia za `IOrderProvider`.

> Analiza żywieniowa **nie** jest celem projektu — wcześniejszy kierunek (Nutri-Score,
> makroskładniki, Open Food Facts) został porzucony na rzecz automatyzacji zakupów.

## Realia integracji (ważne)

Sklepy spożywcze **nie udostępniają publicznego API** do składania zamówień jako klient:

- **Carrefour.pl** — własna platforma e-commerce, brak oficjalnego API; strona stoi za
  **Cloudflare Bot Management**, więc zwykły HTTP-klient dostaje `403`. Realna integracja
  wymaga ruchu „przeglądarkowego" (np. Playwright, ewentualnie z sesją zalogowanego
  użytkownika).
- **Auchan** — zakupy online przeniesione na zamkniętą platformę **Ocado**, bez
  publicznego API klienckiego.

Stąd integracja jest **nieoficjalna** (reverse-engineering wewnętrznych endpointów sklepu),
z natury krucha i potencjalnie wbrew regulaminowi — przeznaczona do automatyzacji
**własnych** zakupów. Mechanizm jest schowany za interfejsami w `Core`, więc wymienialny
bez ruszania reszty aplikacji.

## Architektura

Projektowana **provider-agnostycznie** (adapter pattern). Reszta aplikacji nie wie, z jakiego
sklepu pochodzą dane ani jak technicznie realizowane jest zamówienie — zależy tylko od
interfejsów w `Core`.

| Projekt | Rola |
| --- | --- |
| `ZakupOgarniacz.Core` | Domeny i interfejsy (`ICatalogProvider`, `IOrderProvider`), logika koszyka |
| `ZakupOgarniacz.Providers` | Adaptery sklepów (`CarrefourProvider`, …) |
| `ZakupOgarniacz.Infrastructure` | Klienci HTTP / automatyzacja przeglądarki, Polly (resilience), cache, (później) EF Core |
| `ZakupOgarniacz.Api` | Host Web API (minimal API), DI, OpenAPI/Swagger |

Kluczowe interfejsy w `Core`:

- `ICatalogProvider` — `SearchAsync`, `GetProductAsync` (produkt z ceną i dostępnością)
- `IOrderProvider` — `ExportCartAsync` (deep-link / lista) → docelowo `PlaceOrderAsync`

Przekrojowo: `IHttpClientFactory` + typed clients lub automatyzacja przeglądarki,
**Polly** (retry, circuit breaker) przez `Microsoft.Extensions.Http.Resilience`, cache
katalogu, **Serilog**, OpenAPI. Persystencja koszyka/historii: **EF Core + SQLite**
(dochodzi w późniejszym kroku), przełączalne na Postgres.

## Struktura repo

```
ZakupOgarniacz.slnx
src/
  ZakupOgarniacz.Api/
  ZakupOgarniacz.Core/
  ZakupOgarniacz.Providers/
  ZakupOgarniacz.Infrastructure/
tests/
  ZakupOgarniacz.UnitTests/
  ZakupOgarniacz.IntegrationTests/
Directory.Build.props
.gitignore
README.md
```

## Stack

- .NET 10 (LTS), ASP.NET Core Web API (minimal API)
- Microsoft.Extensions.Http.Resilience (Polly), Serilog, OpenAPI
- (do adaptera sklepu) automatyzacja przeglądarki — np. Playwright — z uwagi na Cloudflare
- EF Core (SQLite → opcjonalnie Postgres) — krok później
- xUnit + Microsoft.AspNetCore.Mvc.Testing

## Roadmapa

1. **Domena + szkielet** — interfejsy `ICatalogProvider` / `IOrderProvider`, modele
   (`Product` z ceną, `Cart` / `CartItem`), endpointy katalogu i koszyka.
2. **Adapter Carrefour (odczyt)** — rekonesans i reverse-engineering wyszukiwania/produktu,
   obejście Cloudflare (przeglądarka). Mapowanie na modele domenowe.
3. **Koszyk + eksport** — budowa koszyka, `ExportCartAsync` (deep-link / lista),
   persystencja koszyka (EF Core + SQLite).
4. **Auto-checkout (docelowo)** — `PlaceOrderAsync` przez sesję / automatyzację przeglądarki.
5. **Dodatki** — kolejne sklepy za tym samym interfejsem, porównanie cen, frontend.

## Start

```bash
dotnet build
dotnet run --project src/ZakupOgarniacz.Api
```

## Konwencje

- **Conventional Commits.**
- Pierwszy commit wylądował na `master`; cała dalsza praca na branchach tworzonych od `master`.
- `CLAUDE.md` jest w `.gitignore` i **nie jest** wersjonowany.
- Przy zmianach w CI/CD aktualizować badge w README (build, tests, coverage, wersja .NET).
