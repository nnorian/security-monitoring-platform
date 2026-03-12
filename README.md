# Security Monitoring Platform

A full-stack .NET 8 platform that collects, processes, stores, and visualises security events from live attack simulations in real time. Built end-to-end as a personal project to apply backend development, REST API design, asynchronous programming, and database skills in a realistic distributed system.

---

## What the project does

A vulnerable Linux VM is placed under simulated attack (SSH brute-force, port scans, web exploitation). A lightweight agent tails its system logs and streams each line as a structured JSON event to the platform. Three .NET 8 microservices then handle ingestion, classification, persistence, and threat enrichment — with Grafana dashboards providing the visualisation layer.

---

## Architecture

```
Victim VM (Metasploitable3)
   └─ log-shipper (tails logs → HTTP POST)
         │
         ▼
   LogCollector  (.NET 8 REST API)
   • validates and accepts SecurityLog payloads
   • publishes events to RabbitMQ queue
   • exposes Prometheus metrics per source/severity
         │
         ▼  RabbitMQ — "security-logs" queue
         │
   AlertManager  (.NET 8 BackgroundService)
   • consumes queue asynchronously
   • classifies events: high/critical → Alert entity
   • maps source to MITRE ATT&CK tactic
   • persists to PostgreSQL via Entity Framework Core
   • exposes GET /alerts REST endpoint
         │
   ThreatIntel  (.NET 8 REST API)
   • enriches source IPs via AbuseIPDB external API
   • Redis cache (1-hour TTL) reduces redundant calls
         │
         ▼
   Prometheus ──► Grafana  (dashboards & visualisation)
   Loki       ──► log aggregation
```

---

## Backend — .NET 8

### OOP & design

| Concept | Where |
|---|---|
| **Encapsulation** | `RabbitMqPublisher` wraps the AMQP connection/channel; callers only see `PublishAsync<T>()` |
| **Abstraction** | `BackgroundService` base class; `IServiceProvider`, `IConfiguration`, `ILogger` injected via interfaces |
| **Dependency Injection** | All services registered in the DI container (`AddSingleton`, `AddHostedService`, `AddDbContext`) — constructor injection throughout |
| **Generics** | `PublishAsync<T>(T message)` serialises any type; `DbSet<Alert>` typed repository |
| **Async / concurrency** | Full `async/await` stack — queue consumer, DB writes, HTTP calls, Redis ops all non-blocking |
| **Exception handling** | `URLError` caught in the log-shipper; graceful degradation on missing config values |

### SOLID & design patterns

| Principle / Pattern | Where |
|---|---|
| **S — Single Responsibility** | Each microservice owns exactly one concern: LogCollector ingests, AlertManager classifies, ThreatIntel enriches — no service crosses those boundaries |
| **O — Open/Closed** | `LogConsumerService` extends `BackgroundService` to add queue-processing behaviour without modifying the base class |
| **D — Dependency Inversion** | All dependencies (`ILogger`, `IConfiguration`, `IServiceProvider`) are injected via interfaces; concrete types are resolved by the DI container at runtime, not hardcoded |
| **Factory pattern** | `ConnectionFactory` creates RabbitMQ connections; `IHttpClientFactory` creates scoped `HttpClient` instances in ThreatIntel |
| **Facade pattern** | `RabbitMqPublisher` hides AMQP connection management, channel lifecycle, and JSON serialisation behind a single `PublishAsync<T>()` call |
| **Template Method pattern** | `BackgroundService` defines the execution lifecycle; `LogConsumerService` implements only `ExecuteAsync()` — the framework calls it at the right time |
| **Repository pattern** | `AlbertDbContext` with `DbSet<Alert>` acts as a typed repository, abstracting SQL from the business logic in `LogConsumerService` |

### REST API design

- `POST /logs` — accepts `SecurityLog`, publishes to queue, returns `202 Accepted` with resource location header
- `GET /alerts` — returns all persisted alerts from PostgreSQL
- `GET /threat/check/{ip}` — IP reputation lookup with cache-hit flag in response
- `GET /health` — liveness probe on all three services
- `GET /metrics` — Prometheus scrape endpoint (auto-exposed via `prometheus-net`)
- Swagger / OpenAPI UI auto-generated on all services

### Database — PostgreSQL + Entity Framework Core

```sql
-- Alert entity (auto-migrated via EnsureCreated)
CREATE TABLE "Alerts" (
    "Id"               UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    "Title"            TEXT         NOT NULL,
    "Description"      TEXT         NOT NULL,
    "Severity"         TEXT         NOT NULL DEFAULT 'medium',
    "SourceIp"         TEXT         NOT NULL,
    "MitreAttackTactic" TEXT        NOT NULL,
    "CreatedAt"        TIMESTAMPTZ  NOT NULL DEFAULT now(),
    "Acknowledged"     BOOLEAN      NOT NULL DEFAULT false
);
```

Useful analytical queries:

```sql
-- Top 10 attacking IPs in the last 24 hours
SELECT "SourceIp", COUNT(*) AS alert_count
FROM "Alerts"
WHERE "CreatedAt" >= now() - INTERVAL '24 hours'
GROUP BY "SourceIp"
ORDER BY alert_count DESC
LIMIT 10;

-- Alert volume by severity
SELECT "Severity", COUNT(*) AS total
FROM "Alerts"
GROUP BY "Severity"
ORDER BY total DESC;

-- Unacknowledged critical alerts
SELECT * FROM "Alerts"
WHERE "Severity" = 'critical' AND "Acknowledged" = false
ORDER BY "CreatedAt" DESC;
```

### Caching — Redis

`ThreatIntel` stores AbuseIPDB responses in Redis with a 1-hour TTL. Repeated lookups for the same IP are served from cache — the response includes a `fromCache` flag so callers know the data age.

---

## Tech stack

| Layer | Technology |
|---|---|
| Backend | C# / .NET 8 minimal APIs |
| Messaging | RabbitMQ |
| Relational DB | PostgreSQL + Entity Framework Core 8 |
| Cache | Redis (StackExchange.Redis) |
| Metrics | Prometheus + prometheus-net |
| Visualisation | Grafana (pre-provisioned dashboards) |
| Log aggregation | Loki + Promtail |
| API docs | Swagger / Swashbuckle |
| Infrastructure | Docker Compose, Vagrant, VirtualBox |

---

## Running the platform

**Prerequisites:** Docker, Docker Compose

```bash
docker compose up -d
```

| Service | URL |
|---|---|
| Grafana dashboards | http://localhost:3000 (admin / admin) |
| LogCollector Swagger | http://localhost:5002/swagger |
| AlertManager Swagger | http://localhost:5003/swagger |
| ThreatIntel Swagger | http://localhost:8004/swagger |
| Prometheus | http://localhost:9090 |
| RabbitMQ management | http://localhost:15672 (admin / password) |

**Security lab (optional):**
```bash
cd security-lab
vagrant up victim attacker   # provisions Metasploitable3 + Kali Linux
```

---

## Why I built this

I wanted a project that went beyond tutorials — something with real moving parts that forced me to make actual architectural decisions. Designing three services that communicate asynchronously, share a database correctly, and stay observable under load taught me more about .NET, async patterns, and distributed systems than any course. The security domain gave me a realistic, high-throughput data source to work with rather than fake placeholder data.
