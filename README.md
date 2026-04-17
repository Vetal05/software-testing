# NewsAggregator — завдання 26 (software-testing)

ASP.NET Core 8 Web API + PostgreSQL, xUnit (модульні / інтеграційні / Testcontainers), k6.

## Вимоги

- .NET SDK 8
- Docker Desktop (для Testcontainers у тестах)
- PostgreSQL (для локального запуску API без контейнера в тестах)
- [k6](https://k6.io/) для навантажувальних сценаріїв

## Запуск API

Спочатку має слухати **PostgreSQL** на `localhost:5432` (як у `appsettings.json`). Якщо БД ще немає — найпростіше через Docker у корені репозиторію:

```bash
docker compose up -d
```

Дочекайся готовності (`healthy` у `docker compose ps`), потім:

```bash
cd src/NewsAggregator.Api
dotnet run
```

Помилка **«actively refused» на порту 5432** означає, що Postgres не запущений або порт зайнятий іншим процесом.

Строка підключення: `ConnectionStrings:DefaultConnection` у `appsettings.json` (користувач/пароль/БД мають збігатися з `docker-compose.yml`).

Перед прогоном k6 або ручним тестом з великим обсягом даних увімкніть сид (≥10 000 рядків):

```bash
set NewsAggregator__SeedOnStartup=true
dotnet run --project src/NewsAggregator.Api
```

(У PowerShell: `$env:NewsAggregator__SeedOnStartup="true"`)

## Тести

Усі інтеграційні тести та тести БД використовують **Testcontainers.PostgreSql** — потрібен запущений Docker.

```bash
dotnet test
```

### Запуск тестів з Swagger (Development, «для приколу»)

У Swagger з’являється **POST** `/api/dev/tests/run`:

- **`scope=all`** (за замовчуванням) — усі тести (потрібен Docker).
- **`scope=unit`** — лише швидкі модульні, без контейнерів.

Відповідь JSON структурована для Swagger: спочатку **`success`**, **`statusMessage`**, **`counts`** (failed/passed/skipped/total), **`vstestSummaryLine`**, потім **`outputTailLines`** (хвіст консолі), повні **`standardOutputText`** / **`standardErrorText`** (можуть бути обрізані).

Щоб це працювало, поки **API вже запущений**, виклик іде з **`dotnet test --no-build`**. Тому **спочатку зібери рішення** з кореня:

```powershell
cd "d:\каторга\software-testing"
dotnet build
```

Потім запускай API й натискай **Execute** у Swagger. У **Production** ендпоінт повертає **404**.

Лише швидкі модульні тести (без Docker):

```bash
dotnet test --filter "FullyQualifiedName~NewsAggregator.Tests.Unit"
```

## k6

Підніміть API з увімкненим сидом, вкажіть URL (порт з `launchSettings.json`):

```bash
k6 run perf/k6/articles-pagination.js -e BASE_URL=http://localhost:5239
k6 run perf/k6/trends-stress.js -e BASE_URL=http://localhost:5239
```

## Перед захистом (що запустити по черзі)

Працюй з кореня репозиторію: `d:\каторга\software-testing` (шлях підстав свій). **Docker Desktop** має бути запущений.

### 1. PostgreSQL для демо API

```powershell
cd "d:\каторга\software-testing"
docker compose up -d
docker compose ps
```

### 2. Тести (показати, що все зелене)

```powershell
cd "d:\каторга\software-testing"
dotnet test
```

### 3. API + Swagger (що показувати в браузері)

У **новому** вікні PowerShell:

```powershell
cd "d:\каторга\software-testing\src\NewsAggregator.Api"
dotnet run
```

У консолі з’явиться рядок на кшталт `Swagger UI: http://localhost:5239/swagger` — відкрий його в браузері.

Якщо треба **велика БД** (як у завданні, для k6 або демо обсягу) — **перед першим** успішним сидом у тому ж вікні:

```powershell
$env:NewsAggregator__SeedOnStartup="true"
dotnet run
```

(Повторні запуски з тим самим прапором дані не дублюють.) Після сиду можна зняти змінну: `Remove-Item Env:NewsAggregator__SeedOnStartup` і знову `dotnet run` без сиду.

### 4. k6 (якщо викладач просить навантаження)

Окремий термінал, API має вже працювати (бажано з сидом):

```powershell
cd "d:\каторга\software-testing"
k6 run perf/k6/articles-pagination.js -e BASE_URL=http://localhost:5239
k6 run perf/k6/trends-stress.js -e BASE_URL=http://localhost:5239
```

**Підсумок:** для мінімального захисту зазвичай достатньо **1 → 2 → 3**; k6 — за потреби.

## Структура

- `src/NewsAggregator.Core` — доменна логіка без інфраструктури (`Trending/`, `Articles/`, `NewsCategory`)
- `src/NewsAggregator.Api`
  - `Controllers/` — HTTP-шар
  - `Entities/` — EF-сутності (окремо від персистентності)
  - `Data/` — `AppDbContext`, міграції, `Seeding/` (Bogus)
  - `Models/Dtos/` — request/response-моделі API
- `tests/NewsAggregator.Tests` — `Unit/`, `Integration/`, `Database/`, `Support/`
- `perf/k6` — сценарії навантаження
