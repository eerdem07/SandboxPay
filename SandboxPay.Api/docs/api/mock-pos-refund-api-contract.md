# Mock POS Refund API Contract

Status: Draft  
Module: mock-pos  
Related use case: `mock-pos-refund-use-case.md`  
Related authorize contract: `mock-pos-authorize-api-contract.md`  
Related capture contract: `mock-pos-capture-api-contract.md`  
Related void contract: `mock-pos-void-api-contract.md`

---

## 1. Endpoint

```http
POST /api/v1/pos/refund
```

---

## 2. Purpose

Allows Mira Gateway to refund a previously completed payment through Mock BankPOS.

Eligible transactions:

- `capture=true` authorization with `status=APPROVED, transactionType=SALE`
- `capture=false` authorization that was subsequently captured, with `captured=true`

Not eligible:

- `AUTHORIZED` (uncaptured) authorizations → use void endpoint instead
- Already refunded transactions

---

## 3. Headers

| Header | Required | Description |
|---|---:|---|
| `Content-Type` | Yes | `application/json` |
| `X-Request-Id` | No | Request tracing |
| `X-Api-Key` | No | Optional for now |
| `X-Signature` | No | Optional for now |

---

## 4. Request Body

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

Notes:

- `transactionId` is the refund operation's own Gateway transaction id.
- `originalTransactionId` is the Gateway transaction id from the original authorization.
- `hostReferenceNumber` in the request is the **original authorization's** host reference number.
- For a SALE, use identifiers directly from the authorization response.
- For a CAPTURED authorization, use identifiers from the **original authorization** response, not the capture response.

---

## 5. Request Fields

| Field | Type | Required | Description |
|---|---|---:|---|
| `merchantId` | string | Yes | Mock POS merchant id |
| `terminalId` | string | Yes | Mock POS terminal id |
| `orderId` | string | Yes | Gateway/Merchant order reference |
| `transactionId` | string | Yes | Gateway refund transaction id/reference |
| `originalTransactionId` | string | Yes | Gateway transaction id from the original authorization |
| `originalPosTransactionId` | string | Yes | POS transaction id returned by the original authorization |
| `authCode` | string | Yes | Authorization code returned by the original authorization |
| `hostReferenceNumber` | string | Yes | Host reference number returned by the original authorization |
| `amount` | string decimal | Yes | Refund amount (must equal the original authorization amount) |
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

## 7. Response Body - Approved Refund

When:

```text
Original authorization exists
Original authorization is captured (SALE or CAPTURED)
Original authorization is not already refunded
All identifiers match
Amount and currency match original authorization
```

Response:

```json
{
  "status": "REFUNDED",
  "transactionType": "REFUND",
  "approved": true,
  "responseCode": "00",
  "responseMessage": "Refund approved",
  "transactionId": "refund_123",
  "originalTransactionId": "attempt_123",
  "posRefundId": "pos_ref_91f3c7a20b",
  "originalPosTransactionId": "pos_txn_8f4c2a1b9d",
  "hostReferenceNumber": "HST20260513090000C3M7",
  "amount": "1250.50",
  "currency": "TRY",
  "refundedAt": "2026-05-13T09:00:00Z"
}
```

Notes:

- `hostReferenceNumber` in the response is the **refund's own** host reference number, not the original authorization's.
- `posRefundId` is generated for the refund operation.

Gateway side expected state:

```text
Refund -> SUCCEEDED
PaymentAttempt -> REFUNDED
PaymentIntent -> REFUNDED
```

---

## 8. Response Body - Invalid Transaction

When the referenced authorization cannot be found, is not in a refundable state (not captured or sale), is already refunded, is expired, or identifier matching fails:

```json
{
  "status": "FAILED",
  "transactionType": "REFUND",
  "approved": false,
  "responseCode": "12",
  "responseMessage": "Invalid transaction",
  "transactionId": "refund_123",
  "originalTransactionId": "attempt_123",
  "posRefundId": null,
  "originalPosTransactionId": "pos_txn_8f4c2a1b9d",
  "hostReferenceNumber": null,
  "amount": "1250.50",
  "currency": "TRY",
  "refundedAt": null
}
```

---

## 9. Response Body - Invalid Amount

When the refund amount or currency does not match the original authorization:

```json
{
  "status": "FAILED",
  "transactionType": "REFUND",
  "approved": false,
  "responseCode": "13",
  "responseMessage": "Invalid amount",
  "transactionId": "refund_123",
  "originalTransactionId": "attempt_123",
  "posRefundId": null,
  "originalPosTransactionId": "pos_txn_8f4c2a1b9d",
  "hostReferenceNumber": null,
  "amount": "1000.00",
  "currency": "TRY",
  "refundedAt": null
}
```

---

## 10. Response Body - Already Refunded

When the original authorization was already refunded:

```json
{
  "status": "FAILED",
  "transactionType": "REFUND",
  "approved": false,
  "responseCode": "12",
  "responseMessage": "Invalid transaction",
  "transactionId": "refund_124",
  "originalTransactionId": "attempt_123",
  "posRefundId": null,
  "originalPosTransactionId": "pos_txn_8f4c2a1b9d",
  "hostReferenceNumber": null,
  "amount": "1250.50",
  "currency": "TRY",
  "refundedAt": null
}
```

First version does not implement refund idempotency.

Future version may make repeated refund requests idempotent by storing and returning the original successful refund result.

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
| `status` | string | No | `REFUNDED` or `FAILED` |
| `transactionType` | string | No | `REFUND` |
| `approved` | boolean | No | Whether POS approved the refund |
| `responseCode` | string | No | POS response code |
| `responseMessage` | string | No | POS response message |
| `transactionId` | string | No | Echoed Gateway refund transaction id |
| `originalTransactionId` | string | No | Echoed original Gateway authorization transaction id |
| `posRefundId` | string | Yes | Mock POS refund id; null on failure |
| `originalPosTransactionId` | string | No | Echoed original POS transaction id |
| `hostReferenceNumber` | string | Yes | Refund host reference number; null on failure |
| `amount` | string decimal | No | Echoed refund amount |
| `currency` | string | No | Echoed currency |
| `refundedAt` | string datetime | Yes | POS refund timestamp; null on failure |

---

## 13. POS Response Codes

| Code | Message | Status | Scenario |
|---|---|---|---|
| `00` | Refund approved | `REFUNDED` | Refund succeeded |
| `12` | Invalid transaction | `FAILED` | Original authorization not found, not refundable, identifier mismatch, expired, or already refunded |
| `13` | Invalid amount | `FAILED` | Amount or currency does not match original authorization |
| `30` | Format error | `FAILED` | Request validation failed |
| `91` | Issuer or switch unavailable | `FAILED` | Future technical failure simulation |
| `96` | System malfunction | `FAILED` | Future technical failure simulation |
| `TIMEOUT` | Bank POS timeout | `FAILED` | Future timeout simulation |

---

## 14. ID Generation Rules

### `posRefundId`

Format:

```text
pos_ref_{random}
```

Example:

```text
pos_ref_91f3c7a20b
```

### `hostReferenceNumber`

Refund response gets a new host reference number.

Format:

```text
HST{yyyyMMddHHmmss}{shortRandom}
```

Example:

```text
HST20260513090000C3M7
```

---

## 15. Original Transaction Identifiers

For a **SALE** transaction, use identifiers from the authorization response:

| Refund Field | Source |
|---|---|
| `originalTransactionId` | `transactionId` from authorization response |
| `originalPosTransactionId` | `posTransactionId` from authorization response |
| `authCode` | `authCode` from authorization response |
| `hostReferenceNumber` | `hostReferenceNumber` from authorization response |

For a **CAPTURED** authorization-only transaction, use identifiers from the **original authorization** response, not the capture response:

| Refund Field | Source |
|---|---|
| `originalTransactionId` | `transactionId` from authorization response |
| `originalPosTransactionId` | `posTransactionId` from authorization response |
| `authCode` | `authCode` from authorization response |
| `hostReferenceNumber` | `hostReferenceNumber` from authorization response |

---

## 16. Storage Notes

SALE authorizations (`capture=true`, `status=APPROVED`) must also be stored in the authorization store with `Captured=true` set at authorization time, making them eligible for refund.

The authorization store tracks:

- All previously tracked authorization metadata
- `captured` — `true` for SALE (set at authorization time) and for AUTHORIZATION_ONLY after capture
- `voided` — whether the authorization has been voided
- `refunded` — whether the authorization has been refunded

Do not store card data:

- PAN
- CVV
- expiryMonth
- expiryYear

Because storage is in-memory, application restart clears all records. A refund request after restart fails with:

```text
responseCode = 12
responseMessage = Invalid transaction
```

---

## 17. Out of Scope

Do not implement yet:

- Partial refund
- Multiple refunds for the same authorization
- Refund idempotency
- AUTHORIZED void (handled by void endpoint)
- Sale void
- Settlement-aware refund
- Payout
- Webhook
- Database persistence
- Real bank integration
- Real card storage
- HMAC signature validation
