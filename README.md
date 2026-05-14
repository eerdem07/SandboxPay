# SandboxPay

SandboxPay is a mock PSP / mock POS API project for payment gateway development and testing.

It returns deterministic responses based on test card numbers and does not process real payments. Card data is never stored or logged.

## Requirements

- .NET 10 SDK
- Docker optional

## Run Locally

```bash
dotnet restore SandboxPay.sln
dotnet run --project SandboxPay.Api
```

Swagger is available in development:

```text
http://localhost:5102/swagger
https://localhost:7247/swagger
```

## Endpoints

### Authorize

```http
POST /api/v1/pos/authorize
Content-Type: application/json
```

`capture=true` is a SALE — auth and capture complete in one operation.  
`capture=false` is AUTHORIZATION_ONLY — the authorization can later be captured or voided.

Example request:

```json
{
  "merchantId": "mrc_mock_001",
  "terminalId": "term_mock_001",
  "orderId": "ord_20260504_0001",
  "transactionId": "attempt_123",
  "amount": "1250.50",
  "currency": "TRY",
  "installmentCount": 1,
  "capture": true,
  "card": {
    "holderName": "AHMET ERDEM",
    "pan": "4111111111111111",
    "expiryMonth": "12",
    "expiryYear": "2030",
    "cvv": "123"
  }
}
```

If the card PAN is in the 3DS catalog, the response will have `status=PENDING_3DS` and include a `threeDsSessionId`. Complete the flow via the `/3ds/complete` endpoint.

### Capture

```http
POST /api/v1/pos/capture
Content-Type: application/json
```

Captures a previously authorized (`capture=false`) transaction. Uses identifiers echoed from the authorize response.

```json
{
  "merchantId": "mrc_mock_001",
  "terminalId": "term_mock_001",
  "orderId": "ord_20260504_0001",
  "transactionId": "capture_123",
  "originalTransactionId": "attempt_123",
  "originalPosTransactionId": "pos_txn_8f4c2a1b9d",
  "authCode": "A12345",
  "hostReferenceNumber": "HST202605040001",
  "amount": "1250.50",
  "currency": "TRY"
}
```

### Void

```http
POST /api/v1/pos/void
Content-Type: application/json
```

Voids a previously authorized, uncaptured transaction. Same request shape as capture.

```json
{
  "merchantId": "mrc_mock_001",
  "terminalId": "term_mock_001",
  "orderId": "ord_20260504_0001",
  "transactionId": "void_123",
  "originalTransactionId": "attempt_123",
  "originalPosTransactionId": "pos_txn_8f4c2a1b9d",
  "authCode": "A12345",
  "hostReferenceNumber": "HST202605040001",
  "amount": "1250.50",
  "currency": "TRY"
}
```

### Refund

```http
POST /api/v1/pos/refund
Content-Type: application/json
```

Refunds a completed payment (SALE or CAPTURED authorization-only). Uses identifiers from the original **authorization** response, not the capture response.

```json
{
  "merchantId": "mrc_mock_001",
  "terminalId": "term_mock_001",
  "orderId": "ord_20260504_0001",
  "transactionId": "refund_123",
  "originalTransactionId": "attempt_123",
  "originalPosTransactionId": "pos_txn_8f4c2a1b9d",
  "authCode": "A12345",
  "hostReferenceNumber": "HST202605040001A7K2",
  "amount": "1250.50",
  "currency": "TRY"
}
```

### 3DS Complete

```http
POST /api/v1/pos/3ds/complete
Content-Type: application/json
```

Completes a pending 3DS session. The `threeDsSessionId` comes from the `PENDING_3DS` authorize response.

```json
{
  "merchantId": "mrc_mock_001",
  "terminalId": "term_mock_001",
  "orderId": "ord_20260513_0001",
  "transactionId": "complete_001",
  "threeDsSessionId": "3ds_9f2c4e7b1a"
}
```

## Payment Flows

```text
SALE:
  POST /authorize  (capture=true)  →  APPROVED

AUTHORIZATION_ONLY + CAPTURE:
  POST /authorize  (capture=false)  →  AUTHORIZED
  POST /capture                     →  CAPTURED

AUTHORIZATION_ONLY + VOID:
  POST /authorize  (capture=false)  →  AUTHORIZED
  POST /void                        →  VOIDED

REFUND (after SALE or CAPTURE):
  POST /refund                      →  REFUNDED

3DS:
  POST /authorize                   →  PENDING_3DS  (threeDsSessionId issued)
  POST /3ds/complete                →  AUTHORIZED / APPROVED / DECLINED / FAILED
```

## Test Cards

### Standard Cards

| PAN | Response Code | Scenario |
|---|---|---|
| `4111111111111111` | `00` | Approved |
| `4000000000000002` | `05` | Do not honor |
| `4000000000000012` | `12` | Invalid transaction |
| `4000000000000013` | `13` | Invalid amount |
| `4000000000000014` | `14` | Invalid card number |
| `4000000000000030` | `30` | Format error |
| `4000000000000041` | `41` | Lost card |
| `4000000000000043` | `43` | Stolen card |
| `4000000000000051` | `51` | Insufficient funds |
| `4000000000000054` | `54` | Expired card |
| `4000000000000057` | `57` | Transaction not permitted to cardholder |
| `4000000000000058` | `58` | Transaction not permitted to terminal |
| `4000000000000061` | `61` | Exceeds amount limit |
| `4000000000000065` | `65` | Exceeds frequency limit |
| `4000000000000091` | `91` | Issuer unavailable |
| `4000000000000096` | `96` | System malfunction |
| `4000000000009995` | `TIMEOUT` | Bank POS timeout |

Unknown PANs: if Luhn-valid → `00` approved; if Luhn-invalid → `14` declined.

### 3DS Cards

| PAN | Flow | Final Status |
|---|---|---|
| `4000000000003006` | `FRICTIONLESS` — authenticated | `APPROVED` / `AUTHORIZED` |
| `4000000000003014` | `CHALLENGE` — authenticated | `APPROVED` / `AUTHORIZED` |
| `4000000000003022` | `CHALLENGE` — auth failed | `DECLINED` |
| `4000000000003030` | `ATTEMPTED` — 3DS unavailable | `APPROVED` / `AUTHORIZED` (eci=06) |
| `4000000000003048` | `TIMEOUT` — session expired | `FAILED` |
| `4000000000003055` | `FRICTIONLESS` — authorization declined | `DECLINED` |

## Docker

```bash
docker compose build
```

Run locally with `dotnet run` during development. The current `compose.yaml` builds the API image but does not publish a host port yet.

## Documentation

API contracts and use cases are in `SandboxPay.Api/docs/`:

| Doc | Path |
|---|---|
| Authorize API | `docs/api/mock-pos-authorize-api-contract.md` |
| Capture API | `docs/api/mock-pos-capture-api-contract.md` |
| Void API | `docs/api/mock-pos-void-api-contract.md` |
| Refund API | `docs/api/mock-pos-refund-api-contract.md` |
| 3DS API | `docs/api/mock-pos-3ds-api-contract.md` |
| Authorize Use Case | `docs/use-cases/mock-pos-authorize-use-case.md` |
| Capture Use Case | `docs/use-cases/mock-pos-capture-use-case.md` |
| Void Use Case | `docs/use-cases/mock-pos-void-use-case.md` |
| Refund Use Case | `docs/use-cases/mock-pos-refund-use-case.md` |
| 3DS Use Case | `docs/use-cases/mock-pos-3ds-use-case.md` |

## Notes

- This project is for sandbox and development usage only.
- Do not use real card data.
- Full PAN and CVV must not be logged or stored.
- Authorization store is in-memory. Restart clears all records.
