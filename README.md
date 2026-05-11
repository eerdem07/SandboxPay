# SandboxPay

SandboxPay is a mock PSP / mock POS API project for payment gateway development and testing.

The current implementation exposes a Mock POS authorization endpoint that returns deterministic responses based on test card numbers. It does not process real payments and must not store card data.

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

## Endpoint

```http
POST /api/v1/pos/authorize
Content-Type: application/json
```

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

## Test Cards

| PAN | Response Code | Scenario |
|---|---|---|
| `4111111111111111` | `00` | Approved |
| `4000000000000002` | `05` | Do not honor |
| `4000000000000051` | `51` | Insufficient funds |
| `4000000000000054` | `54` | Expired card |
| `4000000000000096` | `96` | System malfunction |
| `4000000000009995` | `TIMEOUT` | Bank POS timeout |

Full test card catalog is documented in:

```text
SandboxPay.Api/docs/api/mock-pos-authorize-api-contract.md
```

## Docker

Build with Docker Compose:

```bash
docker compose build
```

Run locally with `dotnet run` during development. The current `compose.yaml` builds the API image but does not publish a host port yet.

## Documentation

- API contract: `SandboxPay.Api/docs/api/mock-pos-authorize-api-contract.md`
- Use case: `SandboxPay.Api/docs/use-cases/mock-pos-authorize-use-case.md`

## Notes

- This project is for sandbox and development usage only.
- Do not use real card data.
- Full PAN and CVV must not be logged or stored.
