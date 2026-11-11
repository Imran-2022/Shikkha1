# ASSUMPTIONS.md

This document explains every decision made where the spec left ambiguity.

---

## Domain Model

### 1. Each shift is a separate "class" entity
**Assumption:** "Class 6 – Morning Shift" and "Class 6 – Day Shift" are two completely independent `SchoolClass` aggregates, each with its own seat count and hold list.

**Reason:** The spec explicitly states *"প্রতিটা shift আলাদা একটা 'class' হিসেবে তৈরি হবে"* (each shift is a separate class). The `POST /classes` endpoint creates one class per call, so the caller is responsible for naming the class with its shift info.

### 2. Student identity = 17-digit birth registration number
**Assumption:** The `X-Student-Id` header value must be exactly 17 digits matching Bangladesh's জন্ম নিবন্ধন (birth registration) number format.

**Reason:** The spec says *"শিক্ষার্থীর ১৭ অঙ্কের জন্ম নিবন্ধন নম্বর"* (17-digit birth registration number). This is validated server-side with a regex and returned as `studentId` in the hold response.

### 3. No authentication or login
**Assumption:** Any caller may create classes (admin) or place holds (student). There is no authentication middleware.

**Reason:** The spec says *"Login লাগবে না"* (no login required). Role separation (admin vs student) is purely by UI context.

---

## Concurrency

### 4. Pessimistic locking via `SELECT … FOR UPDATE`
**Assumption:** When placing a hold, the backend opens one explicit transaction (`IUnitOfWork.ExecuteInTransactionAsync`), issues `SELECT 1 FROM classes WHERE id = $1 FOR UPDATE` as its first statement, and only then loads the aggregate, checks the rules and inserts the hold. The lock is released on commit/rollback. The same lock is used for confirm and cancel.

**Reason:** The spec requires *"যত request-ই একসাথে আসুক"* (no matter how many concurrent requests) the seat count must never exceed `totalSeats`. Without a row lock, two simultaneous requests can both read "1 seat available" and both succeed, causing over-booking. The `FOR UPDATE` lock serialises concurrent requests at the database level, satisfying the concurrency test.

### 5. Multi-process backend instances are safe when they share one database
**Assumption:** If two backend processes are running on different ports but both connect to the same PostgreSQL database, the seat-cap check is still safe. The lock is not process-local; it is a database row lock on the `classes` table. The first process to lock the class row blocks the second process until commit/rollback, so both requests cannot read the same available seat simultaneously.

**Reason:** The requirement is *"একই database ব্যবহার করে backend-এর দুটো process আলাদা port-এ একসাথে চললেও held + confirmed সংখ্যা totalSeats-এর বেশি হবে না"* (even with two backend processes on different ports using the same database, held + confirmed must never exceed `totalSeats`). Database row locks work across processes and machines as long as they all talk to the same SQL database. This is the same correctness guarantee as a single-node deployment, but enforced at the database boundary instead of in process memory.

### 6. Idempotent confirm
**Assumption:** Calling `POST /holds/{holdId}/confirm` on an already-confirmed hold returns `200 OK` with `status: "CONFIRMED"` and does **not** increment `confirmedSeats` again.

**Reason:** The spec explicitly requires *"একই আবেদনে confirm একাধিকবার এলেও confirmedSeats একবারই বাড়বে"* (confirmedSeats increments only once regardless of duplicate confirms). The `Confirm()` domain method returns early if already confirmed, so calling it multiple times in parallel is safe.

---

## Hold Expiry

### 6. Expiry is evaluated live **and** written back by a background sweeper
**Assumption:** A hold counts as expired the moment `ExpiresAt` has passed. Seat counts are computed in SQL with `status = 'Held' AND expires_at > now()`, and `GET /holds/{id}` / `GET /me/holds` report `EXPIRED` from the timestamp, so correctness never depends on a job having run. In addition, a hosted service (`HoldExpiryService`, every `HOLD_EXPIRY_SWEEP_SECONDS`, default 15) runs one set-based `UPDATE … SET status = 'Expired' WHERE status = 'Held' AND expires_at <= now()`, so the stored status matches reality for reporting and direct SQL.

**Reason:** The spec says *"মেয়াদ শেষ হলে seat নিজে থেকে ফেরত যাবে, কাউকে কোনো API call করতে হবে না"* (seats return automatically, no API call needed). Live evaluation guarantees that immediately; the sweeper only keeps the database truthful. If the sweeper is down, the system is still correct.

### 7. `HOLD_DURATION_SECONDS` default is 600 (10 minutes)
**Assumption:** As stated in the spec; configurable via environment variable of the same name.

---

## UI

### 8. Student ID is stored in sessionStorage
**Assumption:** The 17-digit student ID entered in the header input is stored in `sessionStorage` so that page refreshes do not lose it, but closing the browser tab clears it.

**Reason:** The spec requires *"Page refresh করলে, বা browser বন্ধ করে আবার খুললেও, শিক্ষার্থী নিজের চলমান আবেদন দেখতে পাবে"* (student can see their active application after refresh). `sessionStorage` persists across page refreshes within the same tab but not across browser sessions, which is appropriate for a session-scoped admission process.

### 9. Seat counts refresh without page reload every 5 seconds
**Assumption:** React Query's `refetchInterval: 5000` polls the classes endpoint every 5 seconds.

**Reason:** The spec requires seat counts to update without page reload. 5-second polling is a good balance between freshness and server load. For a production system with many concurrent users, Server-Sent Events or WebSockets would be preferred.

### 10. Payment is simulated — "Pay Admission Fee" button = instant confirm
**Assumption:** Clicking the payment button calls `POST /holds/{holdId}/confirm` and treats the 200 response as a successful payment.

**Reason:** The spec says *"'ভর্তি ফি দিন' চাপলে ধরে নেবেন payment সফল হয়েছে। আসল payment gateway লাগবে না।"* (assume payment succeeds; no real gateway needed).

---

## API Contract

### 11. Status values are UPPERCASE strings
**Assumption:** `status` fields in all API responses use uppercase: `"HELD"`, `"CONFIRMED"`, `"EXPIRED"`, `"CANCELLED"`.

**Reason:** The spec specifies `HELD`, `CONFIRMED`, `EXPIRED`, `CANCELLED` (see §1.4). EF Core stores the enum as a string using its name, and the API serialises it as uppercase via `.ToUpperInvariant()`.

### 12. Additional response fields are allowed
**Assumption:** Responses may include fields beyond those shown in the spec (e.g. `createdAt`, `studentId` in the hold response).

**Reason:** The spec says *"Response-এ বাড়তি field যোগ করতে পারেন"* (extra fields in response are allowed), but no required fields may be removed or renamed.

### 13. ID format uses a prefix + UUID (e.g. `c_<uuid>`, `h_<uuid>`)
**Assumption:** Class IDs are prefixed with `c_` and hold IDs with `h_`, followed by a compact UUID (no hyphens).

**Reason:** The spec shows examples `c_123`, `h_789` and says *"ID-এর format আপনার পছন্দমতো"* (ID format is your choice). Prefixed UUIDs are human-readable, sortable, and guaranteed globally unique.

---

## Infrastructure

### 14. EF Core migrations run automatically on startup
**Assumption:** The backend calls `context.Database.MigrateAsync()` during `Program.cs` startup, so the database schema is always up-to-date when the container starts.

**Reason:** This allows `docker compose up` to work end-to-end with a fresh PostgreSQL instance without any manual migration step.

### 15. Data persists across container restarts
**Assumption:** PostgreSQL data is stored in a named Docker volume (`postgres_data`) so that `docker compose down` + `docker compose up` cycles do not lose data.

**Reason:** The spec requires *"App restart হলে কোনো data হারাবে না"* (no data loss on restart).
