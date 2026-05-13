# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Restore and run
dotnet restore SandboxPay.sln
dotnet run --project SandboxPay.Api

# Build only
dotnet build SandboxPay.sln

# Docker
docker compose build
```

Swagger UI is available at `http://localhost:5102/swagger` when running in Development mode. There are no automated tests in this project.

## Architecture

**SandboxPay** is a mock PSP / mock POS API for payment gateway testing. It returns deterministic responses based on test card numbers and does not process real payments.

The single project (`SandboxPay.Api`) uses a modular structure. All payment logic lives under `Modules/MockPos/`, organized into four layers:

- **Web** — ASP.NET Core controllers and `Contracts/` (request/response DTOs). Controllers do manual validation and map to commands; they do not call domain objects directly.
- **Application** — Service interfaces and implementations (`AuthorizePaymentService`, `CapturePaymentService`, `VoidPaymentService`). Each service takes a command record and returns a result record. Services orchestrate domain logic and the authorization store.
- **Domain** — Core types: `TestCardCatalog` (PAN → response code mapping), `PosResponseCode` (all ISO 8583-style codes as static instances), `LuhnValidator`, `CardNumber`, and status enums.
- **Infrastructure** — `InMemoryPosAuthorizationStore`: a `ConcurrentDictionary`-backed singleton that persists authorizations across requests within a process lifetime. State is lost on restart.
- **Support** — `PosIdGenerator` for producing `posTransactionId`, `posCaptureId`, `posVoidId`, `authCode`, and `hostReferenceNumber`.

## Payment Flows

**Authorize** (`POST /api/v1/pos/authorize`): resolves response code from `TestCardCatalog` first; falls back to Luhn validation (valid → `00` approved, invalid → `14` declined). `capture=true` is a SALE; `capture=false` is AUTHORIZATION_ONLY and stores the authorization for later capture/void.

**Capture** (`POST /api/v1/pos/capture`): looks up the authorization by `originalTransactionId` and validates all seven identifiers (`merchantId`, `terminalId`, `orderId`, `originalTransactionId`, `originalPosTransactionId`, `authCode`, `hostReferenceNumber`) plus amount and currency. Uses compare-and-swap on the store to prevent double-capture.

**Void** (`POST /api/v1/pos/void`): same identifier validation as capture. Marks the authorization as voided; a voided or captured authorization cannot be voided again.

## Key Domain Rules

- Authorizations expire after 7 days (set in `AuthorizePaymentService`).
- Only `AUTHORIZATION_ONLY` approvals are stored in the authorization store — SALE results are not capturable.
- `posTransactionId`, `authCode`, and `hostReferenceNumber` are `null` on FAILED responses; DECLINED responses include `posTransactionId` and `hostReferenceNumber` but no `authCode`.
- Amount comparison uses `decimal.Parse` with `InvariantCulture` — currency is compared with `Ordinal`.
- Full PAN and CVV must never be logged or stored.

## Documentation

Detailed API contracts and use cases are in `SandboxPay.Api/docs/`:
- `docs/api/mock-pos-authorize-api-contract.md`
- `docs/api/mock-pos-refund-api-contract.md`
- `docs/use-cases/mock-pos-authorize-use-case.md`
- `docs/use-cases/mock-pos-refund-use-case.md`
