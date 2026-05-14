# Mock POS Capture API Contract

Status: Draft  
Module: mock-pos  
Related use case: `mock-pos-capture-use-case.md`  
Related authorize contract: `mock-pos-authorize-api-contract.md`

---

## 1. Endpoint

```http
POST /api/v1/pos/capture
```

Reason:

- Mock POS API currently uses operation-based endpoints such as `/authorize`.
- Capture request already carries `originalTransactionId` and `originalPosTransactionId`.
- Capture operation has its own Gateway transaction id, so it is modeled as a separate operation endpoint.

---

## 2. Purpose

Mira Gateway’in daha önce başarılı şekilde authorization-only yapılmış bir Mock POS işlemini sonradan capture etmesini sağlar.

Bu endpoint sadece şu akış için kullanılır:

```text
POST /api/v1/pos/authorize
capture=false
status=AUTHORIZED
transactionType=AUTHORIZATION_ONLY
```

Automatic capture/sale akışında bu endpoint çağrılmaz:

```text
POST /api/v1/pos/authorize
capture=true
status=APPROVED
transactionType=SALE
```

---

## 3. Headers

| Header | Required | Description |
|---|---:|---|
| `Content-Type` | Yes | `application/json` |
| `X-Request-Id` | No | Request tracing için kullanılabilir |
| `X-Api-Key` | No | Şimdilik opsiyonel |
| `X-Signature` | No | Şimdilik opsiyonel |

---

## 4. Request Body

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

---

## 5. Request Fields

| Field | Type | Required | Description |
|---|---|---:|---|
| `merchantId` | string | Yes | Mock POS merchant id |
| `terminalId` | string | Yes | Mock POS terminal id |
| `orderId` | string | Yes | Gateway/Merchant order reference |
| `transactionId` | string | Yes | Gateway capture transaction id/reference |
| `originalTransactionId` | string | Yes | Original Gateway authorization attempt id/reference |
| `originalPosTransactionId` | string | Yes | POS transaction id returned by authorization-only response |
| `authCode` | string | Yes | Authorization code returned by authorization-only response |
| `hostReferenceNumber` | string | Yes | Original host reference number returned by authorization-only response |
| `amount` | string decimal | Yes | Capture amount. First version supports full capture only |
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

## 7. Response Body - Approved Capture

When:

```text
Original authorization exists
Original authorization status = AUTHORIZED
Original authorization is not captured yet
Amount and currency match original authorization
responseCode = 00
```

Response:

```json
{
  "status": "CAPTURED",
  "transactionType": "CAPTURE",
  "approved": true,
  "responseCode": "00",
  "responseMessage": "Capture approved",
  "transactionId": "capture_123",
  "originalTransactionId": "attempt_123",
  "posCaptureId": "pos_cap_91f3c7a20b",
  "originalPosTransactionId": "pos_txn_8f4c2a1b9d",
  "hostReferenceNumber": "HST20260513083000A7K2",
  "amount": "1250.50",
  "currency": "TRY",
  "capturedAt": null
}
```

Notes:

- `hostReferenceNumber` in the response is the capture host reference number.
- The original host reference number is sent in the request and used for validation.
- `posCaptureId` is generated for the separate capture operation.

---

## 8. Response Body - Invalid Original Authorization

When the referenced authorization cannot be found, is not authorization-only, or identifier matching fails:

```json
{
  "status": "FAILED",
  "transactionType": "CAPTURE",
  "approved": false,
  "responseCode": "12",
  "responseMessage": "Invalid transaction",
  "transactionId": "capture_123",
  "originalTransactionId": "attempt_123",
  "posCaptureId": null,
  "originalPosTransactionId": "pos_txn_8f4c2a1b9d",
  "hostReferenceNumber": null,
  "amount": "1250.50",
  "currency": "TRY",
  "capturedAt": null
}
```

---

## 9. Response Body - Invalid Amount

First version supports full capture only.

When the capture amount or currency does not match the original authorization:

```json
{
  "status": "FAILED",
  "transactionType": "CAPTURE",
  "approved": false,
  "responseCode": "13",
  "responseMessage": "Invalid amount",
  "transactionId": "capture_123",
  "originalTransactionId": "attempt_123",
  "posCaptureId": null,
  "originalPosTransactionId": "pos_txn_8f4c2a1b9d",
  "hostReferenceNumber": null,
  "amount": "1000.00",
  "currency": "TRY",
  "capturedAt": null
}
```

---

## 10. Response Body - Already Captured

When the original authorization was already captured:

```json
{
  "status": "FAILED",
  "transactionType": "CAPTURE",
  "approved": false,
  "responseCode": "12",
  "responseMessage": "Invalid transaction",
  "transactionId": "capture_124",
  "originalTransactionId": "attempt_123",
  "posCaptureId": null,
  "originalPosTransactionId": "pos_txn_8f4c2a1b9d",
  "hostReferenceNumber": null,
  "amount": "1250.50",
  "currency": "TRY",
  "capturedAt": null
}
```

---

## 11. Validation Error Response

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

## 12. Response Fields

| Field | Type | Nullable | Description |
|---|---|---:|---|
| `status` | string | No | `CAPTURED` or `FAILED` |
| `transactionType` | string | No | `CAPTURE` |
| `approved` | boolean | No | Whether POS approved the capture |
| `responseCode` | string | No | POS response code |
| `responseMessage` | string | No | POS response message |
| `transactionId` | string | No | Echoed Gateway capture transaction id |
| `originalTransactionId` | string | No | Echoed original Gateway authorization attempt id |
| `posCaptureId` | string | Yes | Mock POS capture id |
| `originalPosTransactionId` | string | No | Echoed original POS transaction id |
| `hostReferenceNumber` | string | Yes | Capture host reference number |
| `amount` | string decimal | No | Echoed capture amount |
| `currency` | string | No | Echoed currency |
| `capturedAt` | string datetime | Yes | POS capture response timestamp; null on failure |

---

## 13. POS Response Codes

| Code | Message | Status | Scenario |
|---|---|---|---|
| `00` | Capture approved | `CAPTURED` | Capture succeeded |
| `12` | Invalid transaction | `FAILED` | Original authorization not found, not capturable, identifier mismatch, or already captured |
| `13` | Invalid amount | `FAILED` | Amount or currency does not match original authorization |
| `30` | Format error | `FAILED` | Request validation failed |
| `91` | Issuer or switch unavailable | `FAILED` | Future technical failure simulation |
| `96` | System malfunction | `FAILED` | Future technical failure simulation |
| `TIMEOUT` | Bank POS timeout | `FAILED` | Future timeout simulation |

---

## 14. ID Generation Rules

### `posCaptureId`

Format:

```text
pos_cap_{random}
```

Example:

```text
pos_cap_91f3c7a20b
```

### `hostReferenceNumber`

Capture response gets a new host reference number.

Format:

```text
HST{yyyyMMddHHmmss}{shortRandom}
```

Example:

```text
HST20260513083000A7K2
```

---

## 15. Storage Notes

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

Do not store card data:

- PAN
- CVV
- expiryMonth
- expiryYear

Because storage is in-memory, application restart clears authorization records. A capture request after restart should fail with:

```text
responseCode = 12
responseMessage = Invalid transaction
```

---

## 16. Out of Scope

Do not implement yet:

- Partial capture
- Multiple captures for one authorization
- Capture reversal
- Void
- Refund
- Settlement
- Payout
- Webhook
- Database persistence
- Real bank integration
- Real card storage
- HMAC signature validation
