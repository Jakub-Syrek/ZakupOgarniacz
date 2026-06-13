# ZakupOgarniacz

Serwis **ASP.NET Core** do zakupów spożywczych z naciskiem na **analizę żywieniową**.
Rdzeniem aplikacji jest katalog produktów oraz profil żywieniowy koszyka; samo
zamawianie jest opcjonalne i schowane za interfejsem, żeby dało się je dołożyć
później bez ruszania reszty.

<!-- Badge do uzupełnienia po podpięciu CI:
[![build](https://img.shields.io/badge/build-todo-lightgrey)]()
[![tests](https://img.shields.io/badge/tests-todo-lightgrey)]()
[![coverage](https://img.shields.io/badge/coverage-todo-lightgrey)]()
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)]()
[![license](https://img.shields.io/badge/license-todo-lightgrey)]()
-->

## Cel i zakres

- **Rdzeń (priorytet):** wyszukiwanie produktów spożywczych i analiza ich wartości
  odżywczych — kalorie, makroskładniki, składniki, alergeny, Nutri-Score.
- **Koszyk analityczny:** zamiast „złóż zamówienie" — „zbuduj koszyk i policz jego
  profil żywieniowy" (suma i gęstość odżywcza, ostrzeżenia np. o wysokim cukrze/soli,
  porównywanie produktów).
- **Zamawianie / eksport (opcjonalnie, później):** za interfejsem `IOrderProvider`.
  Uwaga: publiczne API do *składania* zamówień w sklepach spożywczych są rzadkością,
  więc realistycznie ta część kończy się na eksporcie koszyka (deep-link / lista),
  a nie pełnym programowym checkoucie.

Źródło danych żywieniowych: **Open Food Facts** (otwarte, darmowe API) — wpięte jako
adapter `OpenFoodFactsProvider` za interfejsem `ICatalogProvider`, więc wymienialne.

## Architektura

Projektowana **provider-agnostycznie** (adapter pattern). Reszta aplikacji nie wie,
z jakiego konkretnie źródła pochodzą dane — zależy tylko od interfejsów w `Core`.

| Projekt | Rola |
| --- | --- |
| `ZakupOgarniacz.Core` | Domeny i interfejsy (`ICatalogProvider`, `IOrderProvider`), logika analizy żywieniowej |
| `ZakupOgarniacz.Providers` | Adaptery do zewnętrznych API (`OpenFoodFactsProvider`, …) |
| `ZakupOgarniacz.Infrastructure` | EF Core, klienty HTTP, Polly (resilience), cache |
| `ZakupOgarniacz.Api` | Host Web API (minimal API), DI, OpenAPI/Swagger |

Kluczowe interfejsy w `Core`:

- `ICatalogProvider` — `SearchAsync`, `GetProductAsync`, `GetNutritionAsync`
- `IOrderProvider` — `CreateCartAsync`, `AddItemAsync`, `PlaceOrderAsync` lub `ExportCartAsync`

Przekrojowo: `IHttpClientFactory` + typed clients, **Polly** (retry, circuit breaker)
przez `Microsoft.Extensions.Http.Resilience`, cache katalogu, **Serilog**, OpenAPI.
Persystencja: **EF Core + SQLite** na start (koszyk, historia cen), przełączalne na Postgres.

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
- EF Core (SQLite → opcjonalnie Postgres)
- Microsoft.Extensions.Http.Resilience (Polly), Serilog, OpenAPI
- xUnit + Microsoft.AspNetCore.Mvc.Testing

## Roadmapa

1. **Szkielet + read-only katalog** — `ICatalogProvider`, adapter Open Food Facts,
   endpointy `search` / `product` z danymi żywieniowymi.
2. **Koszyk + persystencja** — EF Core, model `Cart`/`CartItem`, profil żywieniowy koszyka.
3. **Analiza** — sumy i gęstość odżywcza, ostrzeżenia, porównania produktów.
4. **Eksport koszyka** (opcjonalnie) — `IOrderProvider` jako deep-link / lista.
5. **Dodatki** — śledzenie cen (`BackgroundService`), powiadomienia, frontend (Blazor/MAUI).

## Start

```bash
dotnet build
dotnet run --project src/ZakupOgarniacz.Api
```

## Konwencje

- **Conventional Commits.**
- Pierwszy commit może wylądować na `master`; cała dalsza praca na branchach
  tworzonych od `master`.
- `CLAUDE.md` jest w `.gitignore` i **nie jest** wersjonowany.
- Przy zmianach w CI/CD aktualizować badge w README (build, tests, coverage, wersja .NET).
