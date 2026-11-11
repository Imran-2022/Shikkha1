# AI_USAGE.md

## Tools Used

**Claude (Anthropic)** — used throughout the implementation.

---

## Where AI Was Used

| Area | Usage |
|------|-------|
| Architecture design | Discussed DDD aggregate boundaries (SchoolClass as aggregate root containing AdmissionHold) |
| Backend structure | Generated initial project/folder structure for Clean Architecture layers |
| Domain entities | Drafted `SchoolClass.cs` and `AdmissionHold.cs` with business rules |
| Application layer | Generated CQRS command/query handlers and FluentValidation validators |
| EF Core config | Generated entity configurations and migration scaffold |
| API controllers | Generated controller stubs following the exact API contract |
| Exception middleware | Generated global exception handling middleware |
| Frontend components | Generated reusable UI components (Button, Alert, Badge, Countdown, Card) |
| React hooks | Generated TanStack Query hooks for classes and holds |
| Docker files | Generated `Dockerfile` and `docker-compose.yml` |
| Tests | Generated unit and integration test scaffolding |

---

## Where AI Made Mistakes and How They Were Fixed

### 1. EF Core `FOR UPDATE` placement (fixed in a second review pass)
**Mistake:** The first version loaded the aggregate and *then* issued `SELECT … FOR UPDATE` in a separate implicit transaction, so the lock was released immediately and protected nothing. It also called `ExecuteSqlRawAsync("… {0} …", id, cancellationToken)`, which binds the `CancellationToken` as a second SQL *parameter* instead of as a token.

**Fix:** `IUnitOfWork.ExecuteInTransactionAsync` opens one explicit transaction (inside EF's execution strategy, because retry-on-failure is enabled). The repository takes the row lock first with `ExecuteSqlInterpolatedAsync`, then loads the aggregate, and refuses to run outside a transaction. Place, confirm and cancel all use it. `_context.Update(aggregate)` calls were also removed: on an already-tracked aggregate with client-generated string keys they mark the new hold `Modified`, which makes EF issue an `UPDATE` for a row that does not exist yet.

### 2. Hold expiry evaluated on wrong status
**Mistake:** The initial `HeldSeats` computation counted holds with `Status == Held` without checking `IsExpired`. This meant expired holds were still consuming seats.

**Fix:** Updated `HeldSeats` property in `SchoolClass` to `Count(h => h.Status == HoldStatus.Held && !h.IsExpired)`. Added a unit test (`PlaceHold_ExpiredHoldExists_AllowsNewHold`) to catch this regression.

### 3. Idempotent confirm not truly idempotent under concurrency
**Mistake:** The first version of `Confirm()` checked `Status != Confirmed` and then set it, but without a row lock the check-then-set pattern was a race condition.

**Fix:** Since `Confirm()` already runs within the aggregate loaded with `FOR UPDATE`, the row lock at the class level serialises confirms. Additionally, the domain method returns early (no-op) when already confirmed, satisfying the idempotency requirement even if two requests race.

### 4. Frontend `sessionStorage` not shared across components
**Mistake:** AI initially used a React Context for the student ID, but after a full page refresh the context was lost (re-initialised to empty string) even though `sessionStorage` had the value.

**Fix:** Rewrote the student ID store as a module-level variable initialised from `sessionStorage` on import, with a lightweight pub-sub pattern to sync React state. This guarantees the value survives page refreshes and is shared across all components without a Context provider.

### 5. Countdown component caused infinite re-renders
**Mistake:** The initial `Countdown` implementation called `setRemaining` on every tick inside a `useEffect` that listed `remaining` as a dependency, causing the effect to restart on every render.

**Fix:** Removed `remaining` from the dependency array and used a ref to track the expiry timestamp instead. The interval only depends on `expiresAt` and the `onExpire` callback.

### 6. Packages, migration wiring and build blockers (found in a self-review)
**Mistakes:** `MediatR.Contracts` missing from the Domain project; `FluentValidation.DependencyInjectionExtensions` and `Configuration.Binder` missing from the Application project; `UseSnakeCaseNamingConvention()` called without its package; the `InitialCreate` migration had no `[Migration]`/`[DbContext]` attributes, so EF would never have applied it; the frontend had no lockfile, no Tailwind/ESLint dependencies, no `vite-env.d.ts` (so `tsc` failed) and two unused imports; the backend image had no `curl`, so the compose healthcheck could never pass.

**Fix:** Added/removed the packages (the naming-convention call was dropped instead, since every table and column is already named explicitly), added the migration attributes, generated `package-lock.json`, and verified the frontend with `tsc`, ESLint, Vitest and `vite build`. The backend could not be compiled in the authoring environment, so `dotnet build && dotnet test` should be run once before submitting.

### 7. Missing-header validation returned 400 instead of 422
**Mistake:** `[FromHeader] string studentId` is implicitly required under nullable reference types, so ASP.NET rejected a missing `X-Student-Id` with 400 before the FluentValidation pipeline could return 422. `GET /me/holds` additionally had no validator.

**Fix:** Header parameters are `string?` and normalised to an empty string, so the validators produce a consistent 422; added `GetStudentHoldsQueryValidator` plus tests.

