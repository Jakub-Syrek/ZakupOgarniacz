# Handoff — ZakupOgarniacz

Dokument przekazania dla kolejnej sesji. Stan na commit `3948822`, branch `feat/shopping-pivot`.

## 1. Czym jest projekt (TL;DR)

Serwis **ASP.NET Core (.NET 10)** do **automatyzacji zakupów spożywczych online**. Główny sklep: **Frisco.pl**.
Flow docelowy: *mówisz w naturalnym języku czego chcesz → apka składa koszyk z realnej oferty Frisco → push koszyka do Twojego konta Frisco → dostawę i płatność finalizujesz w Frisco*.

Projekt **był** o analizie żywieniowej — porzucone (pivot). Szczegóły decyzji w pamięci `[[pivot-shopping-automation]]`.

## 2. Repo / branch / PR

- Remote: https://github.com/Jakub-Syrek/ZakupOgarniacz (właściciel **Jakub-Syrek**, e-mail commitów `jakubvonsyrek@gmail.com`).
- Branch pracy: **`feat/shopping-pivot`** (od `master`). **PR #1** otwarty: `feat/shopping-pivot → master`.
- Solucja: **`ZakupOgarniacz.slnx`** (nowy format .slnx, NIE .sln).
- `feat/catalog-readonly` — stary kierunek (Open Food Facts), niezmergowany, do usunięcia (blokował klasyfikator — nie usunięto).

## 3. Build / run / test

```bash
dotnet build ZakupOgarniacz.slnx
dotnet test  ZakupOgarniacz.slnx          # 23/23 (14 unit + 9 integ)
dotnet run --project src/ZakupOgarniacz.Api --urls http://localhost:5123   # UI na /
```

- `Directory.Build.props`: net10.0, Nullable, ImplicitUsings, **TreatWarningsAsErrors** (build musi być czysty).
- Końce linii: `.gitattributes` (`* text=auto`) — ostrzeżenia LF→CRLF są nieszkodliwe.
- **Pułapka:** jeśli build pada na „file locked … ZakupOgarniacz.Api.exe" — ktoś (user) ma uruchomioną instancję. Zatrzymać ją przed buildem (czasem proces usera jest nie-do-ubicia z poziomu narzędzia → poprosić usera).

## 4. Architektura (warstwy)

| Projekt | Zawartość |
| --- | --- |
| `Core` | `Catalog` (Product, Money, SearchResult, ICatalogProvider), `Orders` (Cart, CartItem, CartExport, IOrderProvider, ICartStore), `Shopping` (IShoppingListParser, ShoppingItem) |
| `Providers` | **Frisco** (FriscoProvider, FriscoCatalogMapper, DTO, FriscoCheckoutClient, FriscoCredentialStore, FriscoOrderProvider, FriscoOptions/FriscoCheckoutOptions) + **Carrefour** (CarrefourProvider na Playwright — alternatywa, odłożona) |
| `Infrastructure` | DI: `AddFriscoStore`, `AddFriscoCheckout`, `AddSqliteCartStore`/`AddInMemoryCartStore`, `AddClaudeShoppingParser`; `SqliteCartStore`/`CartDbContext`, `InMemoryCartStore`, `PlaywrightCarrefourTransport`, `ClaudeShoppingListParser`/`ClaudeOptions` |
| `Api` | minimal API + `wwwroot/index.html` (statyczny SPA UI) |
| `tests` | UnitTests, IntegrationTests (fabryka `CatalogApiFactory` podmienia katalog/koszyk/parser na atrapy) |

Kierunek zależności: `Core ← Providers/Infrastructure ← Api`.

## 5. Endpointy HTTP

- `GET /health`
- `GET /products/search?q=&page=&pageSize=` · `GET /products/{code}`
- `POST /carts` · `GET /carts/{id}` · `POST /carts/{id}/items {code,quantity}` · `POST /carts/{id}/export` · `POST /carts/{id}/push-to-frisco`
- `POST /carts/from-command {command, cartId?}` — NL → Claude → wyszukanie → koszyk (503 bez klucza)
- `POST /frisco/token` · `GET /frisco/token/status` · `GET /frisco/cart` · `GET /frisco/delivery?postcode=` · `GET /frisco/raw?path=` (autoryzowany odczyt user-scoped — narzędzie reconu)
- UI `/`: wyszukiwarka, przeglądanie z cenami, koszyk, **pole „Powiedz, co kupić…"**, sekcja Auto-checkout (token + „Wrzuć do Frisco" + terminy dostawy).

## 6. Integracje zewnętrzne + GOTCHA

### Frisco (recon: `[[frisco-api-recon]]`)
- Baza: `https://www.frisco.pl/app/commerce/api/v1/`. Katalog publiczny — **bez Cloudflare**, działa serwerowy HttpClient.
- **Wyszukiwanie:** `GET offer/products/query?search={fraza}&pageIndex={1-based}&pageSize={n}`. ⚠️ Fraza w **`search`**, NIE `query` (query ignorowane → cały katalog, pomidory malinowe #1 — to był bug, naprawiony).
- Produkt w odpowiedzi: `name.pl/en, ean, brand, producer, grammage, unitOfMeasure, isAvailable, price{price, priceAfterPromotion}, imageUrl`.
- **Checkout user-scoped:** `/users/{userId}/…`, auth = `Authorization: Bearer <JWT>` (access token, **TTL ~10 min**). userId użytkownika = `1831066`.
- **Odświeżanie tokena (OAuth/OpenIddict):** `POST https://www.frisco.pl/app/commerce/connect/token` (form: `grant_type=refresh_token`, `refresh_token`, `client_id` — `frisco-web` to niepotwierdzona zgadywanka). Zaimplementowane w `FriscoCheckoutClient` (auto-mint access-tokena + rotacja refresh).
- **Dodawanie do koszyka (POTWIERDZONE):** `PUT /users/{id}/cart` body `{"products":[{"productId","quantity"}]}` → 200.
- **Dostawa:** `GET /users/{id}/calendar/delivery-payment?postcode=` zwraca metody dostawy/płatności (Van, DotPay, karta) — **NIE** okna czasowe. Sloty + rezerwacja (`cart/reservation`, `cart/saved-reservations` [POST]) **niezmapowane** (trzeba przechwycić payloady z DevTools). Płatność = **DotPay**.

### Carrefour (recon: `[[carrefour-api-recon]]`)
- Za **Cloudflare** (serwerowy HTTP = 403). Adapter na **Playwright** (`PlaywrightCarrefourTransport`) — niezweryfikowany w runtime, **odłożony**. Zostaje jako alternatywny `ICatalogProvider`.

### Claude (parser poleceń)
- Oficjalny **Anthropic C# SDK** (`Anthropic` 12.29.0), structured output, model domyślny **`claude-opus-4-8`** (konfig `Claude:Model`).
- Klucz: **`ANTHROPIC_API_KEY`** (env) lub `Claude:ApiKey`. Czytany przy starcie. Bez klucza `/carts/from-command` → 503.

## 7. Sekrety / konfiguracja (NIE wersjonowane)

- **Token Frisco:** wklejany w UI (sekcja Auto-checkout) — `refresh_token` (zalecane, auto-odnawia) lub `access_token`; trzymany tylko w pamięci procesu (`FriscoCredentialStore`). Po restarcie trzeba wkleić ponownie. (refresh_token jest w payloadzie `POST /connect/token` w DevTools.)
- **Klucz Anthropic:** env `ANTHROPIC_API_KEY`.
- `carts.db` (SQLite koszyka) — gitignored.

## 8. GRANICE (twarde — przestrzegane, klasyfikator je egzekwuje)

- **Atrybucja commitów:** NIGDY nie dodawać Claude/AI jako autora/committera/co-authora ani żadnego trailera „Generated with / 🤖 / Co-Authored-By". Commity wyłącznie człowieka. (Globalna zasada usera — dealbreaker.)
- Asystent **nie**: wpisuje haseł/danych logowania, nie wykonuje płatności / nie klika „zapłać", nie wyciąga tokenów z sesji przeglądarki usera, nie robi zapisów na realnym koncie Frisco usera „z własnej inicjatywy" (push do koszyka klika **user** w UI). To było wielokrotnie blokowane i respektowane.
- **Zakres auto-checkoutu:** automat do **ekranu płatności**; logowanie i płatność robi user w Frisco.
- **Język: polski.**

## 9. Co zrobione (główne commity)

scaffold → pivot README → adapter Frisco (katalog) → koszyk + eksport → persystencja SQLite + cena promo → UI → token Frisco (edytowalny + auto-refresh) → push koszyka do Frisco (`PUT /cart`) → terminy dostawy + read-proxy → **NL → koszyk przez Claude** → **fix wyszukiwarki (`search` zamiast `query`)**.

## 10. PENDING / następne kroki

1. **Restart instancji usera** — by złapać fix wyszukiwarki (jego działająca instancja ma stary kod → wciąż zwraca pomidory).
2. **Ekran akceptacji listy** (user prosił): parser → podgląd listy z checkboxami → „Akceptuję" → dopiero koszyk (teraz od razu wpada do koszyka).
3. **Automat dostawy** (opcjonalny, grindy): przechwycić z DevTools payloady `cart/reservation` + endpoint listujący okna czasowe → `GetDeliverySlots` + `Reserve`. Push już stawia koszyk w Frisco, więc dostawę/płatność user kończy tam — zysk marginalny.
4. **Edycja klucza Anthropic w UI w locie** (jak token Frisco) — obecnie env var + restart.
5. **Usunięcie `feat/catalog-readonly`** (stary kierunek) — wymaga zgody usera (destrukcyjne).
6. Domknąć **PR #1** (merge do `master`) gdy gotowe.

## 11. Pamięć (auto-memory)

`C:\Users\jaqbs\.claude\projects\C--Repos-ZakupOgarniacz\memory\`: `MEMORY.md` (indeks), `language-polish`, `pivot-shopping-automation`, `frisco-api-recon`, `carrefour-api-recon`. Czytać przy starcie sesji.
