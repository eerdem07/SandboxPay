# Mock POS Void API Contract

Status: Draft  
Module: mock-pos  
Related use case: `mock-pos-void-use-case.md`  
Related authorize contract: `mock-pos-authorize-api-contract.md`  
Related capture contract: `mock-pos-capture-api-contract.md`

---

## 1. Endpoint

```http
POST /api/v1/pos/void
```

Reason:

- Mock POS API currently uses operation-based endpoints such as `/authorize` and `/capture`.
- Void operation has its own Gateway transaction id.
- Void request references the original authorization with both Gateway id and POS id.

---

## 2. Purpose

Mira Gateway'in daha once basarili sekilde authorization-only yapilmis, henuz capture edilmemis bir Mock POS provizyonunu iptal etmesini saglar.

Bu endpoint sadece su akis icin kullanilir:

```text
POST /api/v1/pos/authorize
capture=false
status=AUTHORIZED
transactionType=AUTHORIZATION_ONLY
captured=false
voided=false
```

Automatic capture/sale akisinda bu endpoint kullanilmaz:

```text
POST /api/v1/pos/authorize
capture=true
status=APPROVED
transactionType=SALE
```

Captured authorization icin void kullanilmaz. Bu durum ileride refund use case kapsaminda ele alinmalidir.

---

## 3. Headers

| Header | Required | Description |
|---|---:|---|
| `Content-Type` | Yes | `application/json` |
| `X-Request-Id` | No | Request tracing icin kullanilabilir |
| `X-Api-Key` | No | Simdilik opsiyonel |
| `X-Signature` | No | Simdilik opsiyonel |

---

## 4. Request Body

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

---

## 5. Request Fields

| Field | Type | Required | Description |
|---|---|---:|---|
| `merchantId` | string | Yes | Mock POS merchant id |
| `terminalId` | string | Yes | Mock POS terminal id |
| `orderId` | string | Yes | Gateway/Merchant order reference |
| `transactionId` | string | Yes | Gateway void transaction id/reference |
| `originalTransactionId` | string | Yes | Original Gateway authorization attempt id/reference |
| `originalPosTransactionId` | string | Yes | POS transaction id returned by authorization-only response |
| `authCode` | string | Yes | Authorization code returned by authorization-only response |
| `hostReferenceNumber` | string | Yes | Original host reference number returned by authorization-only response |
| `amount` | string decimal | Yes | Original authorization amount |
| `currency` | string | Yes | Currency code. Example: `TRY` |

---

## 6. Validation Rules

| Field | Rule |
|---|---|
| `merchantId` | must not be blank |
| `terminalId` | must not be blank |
| `orderId` | must not be blank |
| `transactionId` | must not be blank |
| `originalTransactionId` | must not be blank |
| `originalPosTransactionId` | must not be blank |
| `authCode` | must not be blank |
| `hostReferenceNumber` | must not be blank |
| `amount` | must be positive |
| `currency` | must not be blank |

---

## 7. Response Body - Approved Void

When:

```text
Original authorization exists
Original authorization status = AUTHORIZED
Original authorization is not captured
Original authorization is not voided
Amount and currency match original authorization
responseCode = 00
```

Response:

```json
{
  "status": "VOIDED",
  "transactionType": "VOID",
  "approved": true,
  "responseCode": "00",
  "responseMessage": "Void approved",
  "transactionId": "void_123",
  "originalTransactionId": "attempt_123",
  "posVoidId": "pos_void_91f3c7a20b",
  "originalPosTransactionId": "pos_txn_8f4c2a1b9d",
  "hostReferenceNumber": "HST20260513084500B8L4",
  "amount": "1250.50",
  "currency": "TRY",
  "voidedAt": null
}
```

Notes:

- `hostReferenceNumber` in the response is the void host reference number.
- The original host reference number is sent in the request and used for validation.
- `posVoidId` is generated for the separate void operation.

---

## 8. Response Body - Invalid Original Authorization

When the referenced authorization cannot be found, is not authorization-only, is already captured, is already voided, is expired, or identifier matching fails:

```json
{
  "status": "FAILED",
  "transactionType": "VOID",
  "approved": false,
  "responseCode": "12",
  "responseMessage": "Invalid transaction",
  "transactionId": "void_123",
  "originalTransactionId": "attempt_123",
  "posVoidId": null,
  "originalPosTransactionId": "pos_txn_8f4c2a1b9d",
  "hostReferenceNumber": null,
  "amount": "1250.50",
  "currency": "TRY",
  "voidedAt": null
}
```

---

## 9. Response Body - Invalid Amount

When the void amount or currency does not match the original authorization:

```json
{
  "status": "FAILED",
  "transactionType": "VOID",
  "approved": false,
  "responseCode": "13",
  "responseMessage": "Invalid amount",
  "transactionId": "void_123",
  "originalTransactionId": "attempt_123",
  "posVoidId": null,
  "originalPosTransactionId": "pos_txn_8f4c2a1b9d",
  "hostReferenceNumber": null,
  "amount": "1000.00",
  "currency": "TRY",
  "voidedAt": null
}
```

---

## 10. Response Body - Already Voided

When the original authorization was already voided:

```json
{
  "status": "FAILED",
  "transactionType": "VOID",
  "approved": false,
  "responseCode": "12",
  "responseMessage": "Invalid transaction",
  "transactionId": "void_124",
  "originalTransactionId": "attempt_123",
  "posVoidId": null,
  "originalPosTransactionId": "pos_txn_8f4c2a1b9d",
  "hostReferenceNumber": null,
  "amount": "1250.50",
  "currency": "TRY",
  "voidedAt": null
}
```

First version does not implement void idempotency.

Future version may make repeated void requests idempotent by storing and returning the original successful void result.

---

## 11. Response Body - Already Captured

When the original authorization was already captured:

```json
{
  "status": "FAILED",
  "transactionType": "VOID",
  "approved": false,
  "responseCode": "12",
  "responseMessage": "Invalid transaction",
  "transactionId": "void_125",
  "originalTransactionId": "attempt_123",
  "posVoidId": null,
  "originalPosTransactionId": "pos_txn_8f4c2a1b9d",
  "hostReferenceNumber": null,
  "amount": "1250.50",
  "currency": "TRY",
  "voidedAt": null
}
```

Captured authorizations should be handled by refund.

---

## 12. Validation Error Response

HTTP status:

```http
400 Bad Request
```

Response:

```json
{
  "status": "FAILED",
  "approved": false,
  "responseCode": "30",
  "responseMessage": "Format error",
  "errors": [
    {
      "field": "amount",
      "message": "amount must be greater than 0"
    }
  ]
}
```

---

## 13. Response Fields

| Field | Type | Nullable | Description |
|---|---|---:|---|
| `status` | string | No | `VOIDED` or `FAILED` |
| `transactionType` | string | No | `VOID` |
| `approved` | boolean | No | Whether POS approved the void |
| `responseCode` | string | No | POS response code |
| `responseMessage` | string | No | POS response message |
| `transactionId` | string | No | Echoed Gateway void transaction id |
| `originalTransactionId` | string | No | Echoed original Gateway authorization attempt id |
| `posVoidId` | string | Yes | Mock POS void id |
| `originalPosTransactionId` | string | No | Echoed original POS transaction id |
| `hostReferenceNumber` | string | Yes | Void host reference number |
| `amount` | string decimal | No | Echoed void amount |
| `currency` | string | No | Echoed currency |
| `voidedAt` | string datetime | Yes | POS void response timestamp; null on failure |

---

## 14. POS Response Codes

| Code | Message | Status | Scenario |
|---|---|---|---|
| `00` | Void approved | `VOIDED` | Void succeeded |
| `12` | Invalid transaction | `FAILED` | Original authorization not found, not voidable, identifier mismatch, expired, already captured, or already voided |
| `13` | Invalid amount | `FAILED` | Amount or currency does not match original authorization |
| `30` | Format error | `FAILED` | Request validation failed |
| `91` | Issuer or switch unavailable | `FAILED` | Future technical failure simulation |
| `96` | System malfunction | `FAILED` | Future technical failure simulation |
| `TIMEOUT` | Bank POS timeout | `FAILED` | Future timeout simulation |

---

## 15. ID Generation Rules

### `posVoidId`

Format:

```text
pos_void_{random}
```

Example:

```text
pos_void_91f3c7a20b
```

### `hostReferenceNumber`

Void response gets a new host reference number.

Format:

```text
HST{yyyyMMddHHmmss}{shortRandom}
```

Example:

```text
HST20260513084500B8L4
```

---

## 16. Storage Notes

First implementation may use in-memory storage.

The store should keep only non-sensitive authorization metadata:

- `merchantId`
- `terminalId`
- `orderId`
- `transactionId`
- `posTransactionId`
- `authCode`
- `hostReferenceNumber`
- `amount`
- `currency`
- `authorizedAt`
- `authorizationExpiresAt`
- `captured`
- `voided`

Do not store card data:

- PAN
- CVV
- expiryMonth
- expiryYear

Because storage is in-memory, application restart clears authorization records. A void request after restart should fail with:

```text
responseCode = 12
responseMessage = Invalid transaction
```

Void idempotency is out of scope for the first version, but may be added later by storing successful void operation metadata.

---

## 17. Out of Scope

Do not implement yet:

- Sale void
- Captured authorization void
- Void idempotency
- Refund
- Reversal
- Settlement-aware void
- Partial capture
- Multiple captures for one authorization
- Payout
- Webhook
- Database persistence
- Real bank integration
- Real card storage
- HMAC signature validation
