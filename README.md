# School Admission System

## Stack

| Layer    | Technology                                      |
|----------|-------------------------------------------------|
| Backend  | .NET 9 · C# · ASP.NET Core Web API             |
| ORM      | Entity Framework Core 9 + Npgsql               |
| Database | PostgreSQL 16                                   |
| Frontend | React 18 · TypeScript · Vite · TanStack Query  |
| Styling  | Tailwind CSS                                    |
| Infra    | Docker · Docker Compose · nginx                |

## Architecture

The backend follows **Domain-Driven Design (DDD)** and **Clean Architecture**:

```
SchoolAdmission.Domain          ← Entities, Value Objects, Domain Events, Interfaces
SchoolAdmission.Application     ← CQRS Commands/Queries (MediatR), Validators (FluentValidation)
SchoolAdmission.Infrastructure  ← EF Core, Repositories, PostgreSQL, Migrations
SchoolAdmission.API             ← Controllers, Middleware, DI wiring
```

Dependencies only point **inward**: API → Application → Domain. Infrastructure implements Domain interfaces.

---

## Running the Application

> **Only Docker is required.** No .NET SDK or Node.js needed on your machine.

```bash
# Clone and start everything in one command
git clone <your-repo-url>
cd school-admission

docker compose up --build
```

| Service  | URL                        |
|----------|----------------------------|
| Frontend | http://localhost           |
| Backend API | http://localhost:8080   |

> Database data persists across restarts via a named Docker volume.  
> `docker compose down` stops containers; data is preserved.  
> `docker compose down -v` removes data volumes too.

### API documentation (Development only)

Docker Compose runs the backend in `Production`, where the OpenAPI and Scalar endpoints are disabled. To open the API docs, start PostgreSQL with Docker and run the backend locally in Development:

```bash
docker compose up -d db
dotnet run --project backend/src/SchoolAdmission.API
```

Then open http://localhost:8080/scalar. The Development environment uses the local PostgreSQL connection configured in `appsettings.Development.json`.

---

## Running Tests

### Backend Unit Tests

```bash
cd backend
dotnet test tests/SchoolAdmission.UnitTests
```

### Backend Integration Tests

Integration tests use **Testcontainers** — Docker must be running.

```bash
cd backend
dotnet test tests/SchoolAdmission.IntegrationTests
```

### Run all backend tests

```bash
cd backend
dotnet test
```

### Frontend Tests

```bash
cd frontend
npm install
npm test
```

### Frontend test with coverage

```bash
cd frontend
npm run test:coverage
```

---

## Configuration

### Backend environment variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ConnectionStrings__DefaultConnection` | (see docker-compose) | PostgreSQL connection string |
| `HOLD_DURATION_SECONDS` | `600` | How long a seat hold lasts (seconds) |
| `AllowedOrigins__0`, `__1`, … | localhost | CORS allowed origins |
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET environment |

### Frontend environment variables

| Variable | Default | Description |
|----------|---------|-------------|
| `VITE_API_URL` | `http://localhost:8080` | Backend base URL |

---

## API Reference

Base URL: `http://localhost:8080`

All requests and responses are JSON. Student identity is passed via the `X-Student-Id` header (17-digit birth registration number).

| # | Method | Path | Description |
|---|--------|------|-------------|
| 1 | POST | `/classes` | Create a new class |
| 2 | GET | `/classes` | List all classes with seat counts |
| 3 | GET | `/classes/{classId}` | Get one class's seat status |
| 4 | POST | `/classes/{classId}/holds` | Place a seat hold |
| 5 | GET | `/holds/{holdId}` | Get current hold status |
| 6 | POST | `/holds/{holdId}/confirm` | Confirm a hold (simulate payment) |
| 7 | DELETE | `/holds/{holdId}` | Cancel a hold |
| 8 | GET | `/me/holds` | List all holds for the current student |

---

## UI Screens

| Screen | Description |
|--------|-------------|
| **Classes** | Lists all classes with live seat counts; Apply Now button |
| **My Applications** | Shows all holds for the current student with Pay/Cancel actions |
| **Admin** | Create new classes; view all classes and seat counts |

---

## Key Design Decisions

See `ASSUMPTIONS.md` for the full list. Key highlights:

- **Each shift is a separate class entity** — "Class 6 – Morning Shift" and "Class 6 – Day Shift" are stored independently with their own seat counts.
- **No login required** — the student is identified by their 17-digit birth registration number via `X-Student-Id` header, stored in `sessionStorage` for page-refresh persistence.
- **Concurrency safety** — `SELECT … FOR UPDATE` on the `classes` row (inside one explicit transaction) prevents over-booking under concurrent requests.
- **Idempotent confirm** — confirming the same hold multiple times only increments `confirmedSeats` once.
- **Auto-expiry** — holds expire based on their `ExpiresAt` timestamp. Seat counts and hold status are evaluated live, so expiry is correct without any job; a background sweeper (`HoldExpiryService`) additionally persists `EXPIRED` to the database every 15 s (`HOLD_EXPIRY_SWEEP_SECONDS`). The UI polls every 5 seconds so counts update without page reload.
- **Payment simulation** — clicking "Pay Admission Fee" is treated as a successful payment with no real gateway.
- **Data persistence** — PostgreSQL data is stored in a named Docker volume, surviving `docker compose down` / `docker compose up` cycles.
