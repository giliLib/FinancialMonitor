# Financial Monitor

**Real-Time Transaction Monitoring System**

Financial Monitor is a full-stack application for recording financial transactions and observing new activity as it happens. It combines a REST API and persistent PostgreSQL storage with SignalR notifications, a Redis backplane, and a React monitoring dashboard.

The project focuses on three concerns: **correct transaction handling**, **a responsive real-time monitoring experience**, and **a backend architecture that can be extended to multiple instances**.

## 1. What the application does

- **Add transactions:** submit a transaction through the React interface or the REST API.
- **Monitor transactions:** view existing records and receive newly created transactions without refreshing the page.
- **Filter activity:** switch between all, pending, completed, and failed transactions.
- **View summary metrics:** see transaction counts and amounts grouped by currency.
- **Persist data:** transactions are stored in PostgreSQL, so existing records can be retrieved after restarting the API.
- **Handle duplicate IDs:** the API returns `409 Conflict` if a transaction with the same ID already exists.
- **Support real-time scale-out:** SignalR uses a Redis backplane so notifications can be distributed across backend instances.

## 2. Technology stack

| Layer | Technology | Purpose |
| --- | --- | --- |
| Frontend | React, TypeScript, Vite | Transaction entry and monitoring UI |
| Backend | ASP.NET Core 8 Web API | HTTP endpoints and application logic |
| Real-time communication | ASP.NET Core SignalR | Push new transactions to connected clients |
| Message distribution | Redis / SignalR Redis backplane | Distribute SignalR messages between server instances |
| Database | PostgreSQL, Entity Framework Core | Persistent transaction storage |
| Testing | xUnit | Automated API, repository, and concurrency tests |
| Packaging | Docker | Container image for the backend |
| Deployment configuration | Kubernetes manifests | Example backend replica configuration and service |

## 3. Architecture and transaction flow

```text
                   React + TypeScript
                  /add       /monitor
                    |            ^
                    | HTTP       | SignalR event
                    v            |
               ASP.NET Core Web API
                 TransactionsController
                    |            |
                    v            v
            Transaction repository   SignalR notifier / hub
                    |                         |
                    v                         v
               PostgreSQL               Redis backplane
                                              |
                                    Other API instances
                                    (when deployed)
```

When a transaction is created:

1. The frontend sends `POST /api/Transactions`.
2. ASP.NET Core validates the request model.
3. The repository writes the transaction to PostgreSQL.
4. A duplicate transaction ID produces `409 Conflict` instead of a second record.
5. After a successful insert, the notifier publishes the transaction through SignalR.
6. Connected monitor clients receive the `ReceiveTransaction` event and update their lists.

The monitor initially loads persisted transactions using the REST API. SignalR is used for subsequent live updates. This separation means the dashboard can show historical data as well as new events.

**Design note:** PostgreSQL is the system of record; Redis is used as a SignalR backplane, not as the transaction database.

## 4. Project structure

```text
FinancialMonitor/
├── Backend/
│   ├── FinancialMonitor.Api/
│   │   ├── Controllers/       # REST endpoints
│   │   ├── Data/              # EF Core database context
│   │   ├── Hubs/              # SignalR hub
│   │   ├── Migrations/        # Database migrations
│   │   ├── Models/            # Transaction model and validation
│   │   ├── Services/          # Repository and notifier implementations
│   │   └── Program.cs        # Dependency injection and middleware
│   ├── FinancialMonitor.Tests/ # Automated tests
│   ├── k8s/                   # Deployment and Service manifests
│   └── Dockerfile             # Backend container build
└── client/
    ├── src/
    │   ├── pages/             # AddTransactionPage and MonitorPage
    │   ├── services/          # HTTP API and SignalR clients
    │   └── components/        # Shared UI components
    ├── seed-transactions.js   # Optional demo transaction generator
    └── package.json
```

## 5. Run locally

### Prerequisites

- .NET 8 SDK
- Node.js and npm (a version compatible with the project's Vite installation)
- Docker, or accessible PostgreSQL and Redis instances

The following commands assume that PostgreSQL and Redis are running on the local machine and that the API is served at **`http://localhost:5000`**.

### Step A — Start PostgreSQL and Redis

One option is to start local Docker containers:

```bash
docker volume create financialmonitor-postgres-data

docker run -d --name financialmonitor-postgres \
  -e POSTGRES_DB=financial_monitor \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD="<LOCAL_DB_PASSWORD>" \
  -p 5432:5432 \
  -v financialmonitor-postgres-data:/var/lib/postgresql/data \
  postgres:16-alpine

docker run -d --name financial-redis -p 6379:6379 redis:7-alpine
```

Replace `<LOCAL_DB_PASSWORD>` with a password of your choice. These commands are **examples for a fresh local environment**; reuse existing containers rather than creating duplicates. Do not commit real credentials.

### Step B — Configure and migrate the database

The backend reads its database connection from `ConnectionStrings:DefaultConnection` and its Redis connection from `ConnectionStrings:Redis` (default: `localhost:6379`). For example, in a local terminal:

```bash
cd Backend
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=financial_monitor;Username=postgres;Password=<LOCAL_DB_PASSWORD>'
export ConnectionStrings__Redis='localhost:6379'
```

Replace the placeholder password. Keep credentials in local environment variables or development secrets, not in committed configuration.

Apply the included Entity Framework Core migrations:

```bash
dotnet ef database update --project FinancialMonitor.Api/FinancialMonitor.Api.csproj
```

If `dotnet ef` is not installed, install the EF Core CLI tool compatible with the project first.

### Step C — Start the backend

From `Backend/`:

```bash
dotnet run --project FinancialMonitor.Api/FinancialMonitor.Api.csproj --urls http://localhost:5000
```

The API should be reachable at `http://localhost:5000/api/Transactions`.

In Development mode, Swagger is available at `http://localhost:5000/swagger`.

Health endpoints:

- `GET /health/live` — verifies that the application process can respond.
- `GET /health/ready` — checks Redis availability. **It does not currently check PostgreSQL.**

### Step D — Start the frontend

In a second terminal:

```bash
cd client
npm install
npm run dev
```

Open **`http://localhost:5173`**.

- `/add` — create a transaction.
- `/monitor` — view existing transactions and live updates.

The frontend's API and SignalR URLs are currently configured for `localhost:5000` in `client/src/services/`. The backend CORS policy allows `http://localhost:5173`.

## 6. API reference

| Method | Endpoint | Description | Expected success response |
| --- | --- | --- | --- |
| `GET` | `/api/Transactions` | Retrieve transactions, newest first | `200 OK` |
| `GET` | `/api/Transactions/{id}` | Retrieve a transaction by GUID | `200 OK` |
| `POST` | `/api/Transactions` | Validate and create a transaction | `201 Created` |

Other relevant responses include `400 Bad Request` for invalid input, `404 Not Found` for an unknown ID, and `409 Conflict` for an existing transaction ID.

Example request:

```bash
curl -X POST http://localhost:5000/api/Transactions \
  -H 'Content-Type: application/json' \
  -d '{
    "transactionId": "a5e2dd0d-2c13-4801-a0ce-4c2f08d829bd",
    "amount": 125.50,
    "currency": "USD",
    "status": "Pending",
    "timestamp": "2026-11-12T12:00:00Z"
  }'
```

The transaction model includes:

| Field | Type | Validation |
| --- | --- | --- |
| `transactionId` | GUID | Required, non-empty, unique |
| `amount` | Decimal | Greater than zero |
| `currency` | String | Exactly three uppercase letters |
| `status` | Enum | `Pending`, `Completed`, or `Failed` |
| `timestamp` | Date/time | Required, non-default value |

Statuses are serialized as strings in the API and SignalR payloads.

## 7. Demonstrating real-time updates

1. Open `http://localhost:5173/monitor`.
2. In another tab, open `http://localhost:5173/add`.
3. Submit a valid transaction.
4. Confirm that it appears in the monitor without manually refreshing.
5. Refresh the monitor and confirm that the record is still available from PostgreSQL.
6. Change the status filter to verify the corresponding view.

The monitor displays SignalR connection state and supports reconnection. Newly received transactions are briefly highlighted to make live activity visible.

An optional bulk-demo script is included at `client/seed-transactions.js`. It creates **100 example transactions sequentially**, with a short pause between requests to make live updates observable. **Before running it, verify that its `API_URL` points to `http://localhost:5000/api/Transactions`**; the original script used port `5163`.

```bash
cd client
node seed-transactions.js
```

This script is a UI demonstration tool, **not** a concurrent load or throughput benchmark.

## 8. Automated tests

Run tests from `Backend/` by specifying the test project explicitly (there is no solution file at that directory level):

```bash
dotnet test FinancialMonitor.Tests/FinancialMonitor.Tests.csproj
```

The test suite covers controller behavior, API integration, repository operations, duplicate IDs, ordering, and concurrent repository operations. The concurrency tests exercise the **in-memory repository**; they do not constitute a PostgreSQL stress test or a five-replica Kubernetes test.

**Latest verified result:** 19 passed, 0 failed, 0 skipped. Tests were rerun successfully after the SignalR package update.

A NuGet vulnerability audit of the backend reported no known vulnerable packages from the configured source at the time of the final check.

## 9. Docker

A multi-stage Dockerfile is provided at `Backend/Dockerfile`. It builds and publishes the ASP.NET Core application with the .NET 8 SDK, then packages it in an ASP.NET Core 8 runtime image. The runtime image exposes port `8080` and uses a non-root user.

Build the backend image from `Backend/`:

```bash
docker build -t financial-monitor-backend:latest .
```

**Verified:** the Docker image built successfully after the dependency update.

The backend image alone does not start PostgreSQL or Redis. Running it requires reachable database and Redis services, appropriate connection strings, and a prepared database schema. In a container, `localhost` refers to that container, not automatically to the host or other containers.

## 10. Kubernetes and scaling considerations

The `Backend/k8s/` directory contains a Deployment and a Service manifest for a possible multi-instance backend deployment.

The intended scale-out design is:

- Run multiple replicas of the ASP.NET Core API behind a Kubernetes Service.
- Use shared PostgreSQL storage so all replicas access the same transaction data.
- Use a shared Redis backplane to distribute SignalR messages between replicas.
- Use liveness and readiness probes to help Kubernetes manage instance health.
- Supply connection strings through environment variables and Kubernetes Secrets rather than embedding credentials in images.

The target configuration can be set to **five replicas** using `spec.replicas: 5`. This is a deployment configuration, not proof of a successful five-pod run.

**Scope and limitations:** the Kubernetes configuration is provided as a deployment example; a live cluster deployment and five-replica end-to-end test were **not performed**. A working cluster setup also needs a reachable PostgreSQL instance, a Redis service, a database Secret, an image accessible to the cluster, and appropriately configured health probes. Redis readiness is implemented in the API; PostgreSQL readiness is not yet included.

## 11. Implementation decisions and trade-offs

**Persistence before notification.** A transaction is broadcast only after the repository reports a successful insert. This avoids intentionally notifying clients about rejected duplicate transactions.

**Database-level uniqueness.** PostgreSQL's unique constraint is used to handle duplicate transaction IDs. The repository catches the PostgreSQL unique-violation error and returns a conflict result through the controller.

**Initial fetch plus live events.** REST loads existing transactions, while SignalR handles incoming events. The client can display both persisted history and new activity.

**Redis as a backplane.** Redis supports cross-instance SignalR message distribution. It is not used as a substitute for PostgreSQL persistence.

**Deliberate scope.** The project demonstrates real-time updates and a scale-out-ready architecture, but it does not claim production-grade load testing, deployed Kubernetes infrastructure, authentication, or guaranteed delivery of every event during disconnection. The initial-fetch/live-subscription boundary is not implemented as a transactional event replay mechanism.

## 12. Reviewer checklist

A short verification path:

1. Start PostgreSQL and Redis, configure the backend, and apply migrations.
2. Start the API on port `5000` and React on port `5173`.
3. Confirm `GET /api/Transactions` returns a response.
4. Open `/monitor`, then create a transaction through `/add`.
5. Observe the live update and refresh to confirm persistence.
6. Run `dotnet test FinancialMonitor.Tests/FinancialMonitor.Tests.csproj`.
7. Optionally build the Docker image and review the Kubernetes manifests.

---

**Project status:** the local API, frontend, PostgreSQL persistence, and SignalR connection were verified; all **19 automated tests passed**; the updated backend Docker image built successfully. Kubernetes manifests describe the intended deployment architecture and have not been validated in a live cluster.