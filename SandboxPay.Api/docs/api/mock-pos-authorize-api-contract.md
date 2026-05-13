# Mock POS Authorize API Contract

Status: Draft  
Module: mock-pos  
Related use case: `mock-pos-authorize-use-case.md`  
Related capture contract: `mock-pos-capture-api-contract.md`

---

## 1. Endpoint

```http
POST /api/v1/pos/authorize
```

---

## 2. Purpose

Mira Gateway’in Mock BankPOS üzerinden ödeme authorize veya sale denemesi başlatmasını sağlar.

Bu endpoint test kartı numarasına göre deterministik response döner.

`capture=true` sale flow’dur. Auth ve capture aynı işlemde tamamlanır.

`capture=false` authorization-only/manual capture flow’dur. Başarılı response sonrasında `/api/v1/pos/capture` çağrılabilir.

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

---

## 5. Request Fields

| Field | Type | Required | Description |
|---|---|---:|---|
| `merchantId` | string | Yes | Mock POS merchant id |
| `terminalId` | string | Yes | Mock POS terminal id |
| `orderId` | string | Yes | Gateway/Merchant order reference |
| `transactionId` | string | Yes | Gateway PaymentAttempt id/reference |
| `amount` | string decimal | Yes | Transaction amount |
| `currency` | string | Yes | Currency code. Example: `TRY` |
| `installmentCount` | integer | Yes | `1` means no installment |
| `capture` | boolean | Yes | `true` sale, `false` authorization only |
| `card.holderName` | string | Yes | Card holder name |
| `card.pan` | string | Yes | Card number |
| `card.expiryMonth` | string | Yes | `01` - `12` |
| `card.expiryYear` | string | Yes | Four-digit year recommended |
| `card.cvv` | string | Yes | 3 or 4 digits |

---

## 6. Validation Rules

| Field | Rule |
|---|---|
| `merchantId` | must not be blank |
| `terminalId` | must not be blank |
| `orderId` | must not be blank |
| `transactionId` | must not be blank |
| `amount` | must be positive |
| `currency` | must not be blank |
| `installmentCount` | must be `>= 1` |
| `capture` | must not be null |
| `card.holderName` | must not be blank |
| `card.pan` | must not be blank |
| `card.expiryMonth` | must be between `01` and `12` |
| `card.expiryYear` | must not be blank |
| `card.cvv` | must be 3 or 4 digits |

---

## 7. Response Body - Approved Sale

When:

```text
capture = true
responseCode = 00
```

Response:

```json
{
  "status": "APPROVED",
  "transactionType": "SALE",
  "approved": true,
  "responseCode": "00",
  "responseMessage": "Approved",
  "transactionId": "attempt_123",
  "posTransactionId": "pos_txn_8f4c2a1b9d",
  "authCode": "A12345",
  "hostReferenceNumber": "HST202605040001",
  "amount": "1250.50",
  "currency": "TRY",
  "installmentCount": 1,
  "authorizedAt": "2026-05-04T19:30:00Z"
}
```

Gateway side expected state:

```text
PaymentAttempt -> SUCCEEDED
PaymentIntent -> SUCCEEDED
Capture kaydı oluşturulmaz
```

Reason:

```text
Capture domain’i sonradan yapılan capture operasyonunu temsil eder.
Sale transaction zaten PaymentAttempt üzerinde tamamlanır.
```

---

## 8. Response Body - Approved Authorization Only

When:

```text
capture = false
responseCode = 00
```

Response:

```json
{
  "status": "AUTHORIZED",
  "transactionType": "AUTHORIZATION_ONLY",
  "approved": true,
  "responseCode": "00",
  "responseMessage": "Approved",
  "transactionId": "attempt_123",
  "posTransactionId": "pos_txn_8f4c2a1b9d",
  "authCode": "A12345",
  "hostReferenceNumber": "HST202605040001",
  "amount": "1250.50",
  "currency": "TRY",
  "installmentCount": 1,
  "authorizedAt": "2026-05-04T19:30:00Z"
}
```

This response is capturable through:

```http
POST /api/v1/pos/capture
```

Gateway side expected state:

```text
PaymentAttempt -> AUTHORIZED
PaymentIntent -> REQUIRES_CAPTURE
authorizationExpiresAt set edilir
```

Capture request should use these values from the authorization response:

| Capture Field | Source |
|---|---|
| `originalTransactionId` | `transactionId` |
| `originalPosTransactionId` | `posTransactionId` |
| `authCode` | `authCode` |
| `hostReferenceNumber` | `hostReferenceNumber` |
| `amount` | `amount` |
| `currency` | `currency` |

`authorizationExpiresAt` is Gateway-owned state. It is not returned in this POS response.

---

## 9. Response Body - Declined

Example for insufficient funds:

```json
{
  "status": "DECLINED",
  "transactionType": "SALE",
  "approved": false,
  "responseCode": "51",
  "responseMessage": "Insufficient funds",
  "transactionId": "attempt_123",
  "posTransactionId": "pos_txn_8f4c2a1b9d",
  "authCode": null,
  "hostReferenceNumber": "HST202605040002",
  "amount": "1250.50",
  "currency": "TRY",
  "installmentCount": 1,
  "authorizedAt": "2026-05-04T19:30:00Z"
}
```

---

## 10. Response Body - Failed

Example for system malfunction:

```json
{
  "status": "FAILED",
  "transactionType": "SALE",
  "approved": false,
  "responseCode": "96",
  "responseMessage": "System malfunction",
  "transactionId": "attempt_123",
  "posTransactionId": null,
  "authCode": null,
  "hostReferenceNumber": null,
  "amount": "1250.50",
  "currency": "TRY",
  "installmentCount": 1,
  "authorizedAt": "2026-05-04T19:30:00Z"
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
| `status` | string | No | `APPROVED`, `AUTHORIZED`, `DECLINED`, `FAILED` |
| `transactionType` | string | No | `SALE` or `AUTHORIZATION_ONLY` |
| `approved` | boolean | No | Whether POS approved the payment |
| `responseCode` | string | No | POS response code |
| `responseMessage` | string | No | POS response message |
| `transactionId` | string | No | Echoed Gateway attempt id |
| `posTransactionId` | string | Yes | Mock POS transaction id |
| `authCode` | string | Yes | Authorization code for successful transactions |
| `hostReferenceNumber` | string | Yes | Mock host reference number |
| `amount` | string decimal | No | Echoed amount |
| `currency` | string | No | Echoed currency |
| `installmentCount` | integer | No | Echoed installment count |
| `authorizedAt` | string datetime | No | POS response timestamp |

---

## 13. POS Response Codes

| Code | Message | Status |
|---|---|---|
| `00` | Approved | `APPROVED` or `AUTHORIZED` |
| `05` | Do not honor | `DECLINED` |
| `12` | Invalid transaction | `FAILED` |
| `13` | Invalid amount | `FAILED` |
| `14` | Invalid card number | `DECLINED` |
| `30` | Format error | `FAILED` |
| `41` | Lost card | `DECLINED` |
| `43` | Stolen card | `DECLINED` |
| `51` | Insufficient funds | `DECLINED` |
| `54` | Expired card | `DECLINED` |
| `57` | Transaction not permitted to cardholder | `DECLINED` |
| `58` | Transaction not permitted to terminal | `FAILED` |
| `61` | Exceeds amount limit | `DECLINED` |
| `65` | Exceeds frequency limit | `DECLINED` |
| `91` | Issuer or switch unavailable | `FAILED` |
| `96` | System malfunction | `FAILED` |
| `TIMEOUT` | Bank POS timeout | `FAILED` |

For `00`:

```text
capture=true  -> status=APPROVED, transactionType=SALE
capture=false -> status=AUTHORIZED, transactionType=AUTHORIZATION_ONLY
```

---

## 14. Test Card Catalog

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

---

## 15. Test Card Resolution Rule

Resolution order:

```text
1. Normalize PAN
2. Check test card catalog
3. If found, return mapped response
4. If not found, run Luhn validation
5. If Luhn invalid, return responseCode=14
6. If Luhn valid, return responseCode=00
```

---

## 16. ID Generation Rules

### `posTransactionId`

Format:

```text
pos_txn_{random}
```

Example:

```text
pos_txn_8f4c2a1b9d
```

### `hostReferenceNumber`

Format:

```text
HST{yyyyMMddHHmmss}{shortRandom}
```

Example:

```text
HST20260504193000A7K2
```

### `authCode`

Format:

```text
A{5 random digits}
```

Example:

```text
A12345
```

---

## 17. Security Notes

Mock POS must not store card data.

Do not log:

- Full PAN
- CVV

If PAN must be logged, mask it:

```text
411111******1111
```

---

## 18. Out of Scope

Do not implement yet in this contract:

- 3D Secure
- `/api/v1/pos/3ds/complete`
- Partial capture
- Multiple capture
- Void endpoint
- Refund endpoint
- Settlement
- Payout
- Webhook
- Database persistence
- Real card storage
- HMAC signature validation
