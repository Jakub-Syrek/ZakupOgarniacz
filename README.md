# ZakupOgarniacz

Serwis **ASP.NET Core** do **automatyzacji zakupów spożywczych online**. Rdzeniem jest
zbudowanie koszyka z katalogu sklepu i możliwie daleko posunięta automatyzacja drogi do
złożenia zamówienia — od listy/koszyka, przez eksport/deep-link, po (docelowo) złożenie
zamówienia za użytkownika.

Pierwszy cel: **Frisco.pl** — udostępnia czyste (nieoficjalne) JSON API osiągalne
serwerowo, **z cenami i dostępnością** w wynikach wyszukiwania. Architektura jest
**sklep-agnostyczna** (adapter pattern), więc kolejne sklepy dochodzą za tymi samymi
interfejsami.

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
  2. **Docelowo:** automatyczne złożenie zamówienia za `IOrderProvider`.

> Analiza żywieniowa **nie** jest celem projektu — wcześniejszy kierunek (Nutri-Score,
> makroskładniki, Open Food Facts) został porzucony na rzecz automatyzacji zakupów.

## Realia integracji (ważne)

Sklepy **nie udostępniają oficjalnego publicznego API** do składania zamówień jako klient
(istniejące API są po stronie sprzedawcy/POS, nie klienta). Integracja jest więc
**nieoficjalna** (reverse-engineering wewnętrznych endpointów) i z natury krucha. Różni
sklepy różnią się jednak barierą wejścia:

- **Frisco.pl (cel główny)** — wewnętrzne JSON API (`/app/commerce/api/v1/…`) osiągalne
  zwykłym `HttpClient`-em (brak Cloudflare), z cenami w odpowiedzi. Adapter prosty
  i testowalny end-to-end.
- **Carrefour.pl (alternatywa, odłożona)** — strona za **Cloudflare Bot Management**,
  serwerowy HTTP dostaje `403`. Adapter wymaga **Playwright** (ruch przeglądarkowy);
  zostaje w repo jako alternatywny `ICatalogProvider`.
- **Auchan** — zakupy online na zamkniętej platformie Ocado, bez publicznego API klienta.

Mechanizm jest schowany za interfejsami w `Core`, więc wymienialny bez ruszania reszty.

## Architektura

Provider-agnostycznie (adapter pattern). Reszta aplikacji zależy tylko od interfejsów w `Core`.

| Projekt | Rola |
| --- | --- |
| `ZakupOgarniacz.Core` | Domeny i interfejsy (`ICatalogProvider`, `IOrderProvider`), logika koszyka |
| `ZakupOgarniacz.Providers` | Adaptery sklepów (`FriscoProvider`, `CarrefourProvider`, …) |
| `ZakupOgarniacz.Infrastructure` | Klienci HTTP / Playwright, Polly (resilience), cache, (później) EF Core |
| `ZakupOgarniacz.Api` | Host Web API (minimal API), DI, OpenAPI/Swagger |

Kluczowe interfejsy w `Core`:

- `ICatalogProvider` — `SearchAsync`, `GetProductAsync` (produkt z ceną i dostępnością)
- `IOrderProvider` — `ExportCartAsync` (lista/deep-link) → docelowo `PlaceOrderAsync`

Przekrojowo: `IHttpClientFactory` + typed clients, **Polly** (retry, circuit breaker)
przez `Microsoft.Extensions.Http.Resilience`; dla sklepów za anti-botem — **Playwright**.
Persystencja koszyka/historii: **EF Core + SQLite** (krok później), przełączalne na Postgres.

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
- Microsoft.Extensions.Http.Resilience (Polly), OpenAPI
- Microsoft.Playwright — dla sklepów za Cloudflare (Carrefour)
- EF Core (SQLite → opcjonalnie Postgres) — krok później
- xUnit + Microsoft.AspNetCore.Mvc.Testing

## Roadmapa

1. ✅ **Domena + szkielet** — interfejsy `ICatalogProvider` / `IOrderProvider`, modele
   (`Product` z ceną, `Cart` / `CartItem`), endpointy katalogu.
2. ✅ **Adapter Frisco (odczyt)** — wyszukiwanie z cenami przez `/offer/products/query`,
   mapowanie na modele domenowe (zweryfikowane end-to-end).
3. **Koszyk + eksport** — budowa koszyka, `ExportCartAsync`, persystencja (EF Core + SQLite).
4. **Auto-checkout (docelowo)** — `PlaceOrderAsync` (sesja / automatyzacja).
5. **Dodatki** — kolejne sklepy za tym samym interfejsem, porównanie cen, frontend.

## Start

```bash
dotnet build
dotnet run --project src/ZakupOgarniacz.Api
```

Adapter Frisco działa „od ręki" (zwykły HTTP). Endpointy:

| Metoda | Ścieżka | Opis |
| --- | --- | --- |
| `GET` | `/health` | Health-check. |
| `GET` | `/products/search?q={fraza}&page&pageSize` | Wyszukiwanie z cenami i dostępnością. |
| `GET` | `/products/{code}` | Produkt po EAN/SKU/id (`404`, gdy brak). |

```bash
curl "http://localhost:<port>/products/search?q=mleko&pageSize=5"
```

> Adapter **Carrefour** (alternatywny) wymaga Playwright: po zbudowaniu
> `pwsh src/ZakupOgarniacz.Api/bin/Debug/net10.0/playwright.ps1 install chromium`
> i przełączenia DI na `AddCarrefourStore`.

## Konwencje

- **Conventional Commits.**
- Pierwszy commit wylądował na `master`; cała dalsza praca na branchach tworzonych od `master`.
- `CLAUDE.md` jest w `.gitignore` i **nie jest** wersjonowany.
- Przy zmianach w CI/CD aktualizować badge w README (build, tests, coverage, wersja .NET).
