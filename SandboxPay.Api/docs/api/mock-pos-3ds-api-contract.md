# Mock POS 3DS API Contract

Module: mock-pos

## Authorize Step

3DS akışı `POST /api/v1/pos/authorize` endpoint'i ile başlar. PAN, 3DS test kartı katalogunda yer alıyorsa authorize response'u `PENDING_3DS` döner.

Pending response'ta şu alanlar doludur:

| Field | Notes |
|---|---|
| `status` | `PENDING_3DS` |
| `responseCode` | `PENDING` |
| `threeDsSessionId` | complete request'inde kullanılacak session id |
| `acsUrl` | challenge/frictionless yönlendirme URL'i |
| `threeDsFlow` | `FRICTIONLESS`, `CHALLENGE`, `ATTEMPTED`, `TIMEOUT` |
| `messageVersion` | `2.2.0` |
| `expiresAt` | session expiry timestamp |

## Complete Endpoint

```http
POST /api/v1/pos/3ds/complete
Content-Type: application/json
```

## Complete Request

| Field | Type | Required | Rule |
|---|---|---|---|
| `merchantId` | string | yes | authorize ile aynı olmalı |
| `terminalId` | string | yes | authorize ile aynı olmalı |
| `orderId` | string | yes | authorize ile aynı olmalı |
| `transactionId` | string | yes | complete işlem id |
| `threeDsSessionId` | string | yes | authorize response'tan alınır |

Example:

```json
{
  "merchantId": "mrc_mock_001",
  "terminalId": "term_mock_001",
  "orderId": "ord_20260513_0001",
  "transactionId": "complete_001",
  "threeDsSessionId": "3ds_9f2c4e7b1a"
}
```

## Complete Response

HTTP 200.

| Field | Type | Notes |
|---|---|---|
| `status` | string | `APPROVED`, `AUTHORIZED`, `DECLINED`, `FAILED` |
| `transactionType` | string | `SALE` veya `AUTHORIZATION_ONLY` |
| `approved` | boolean | nihai onay durumu |
| `responseCode` | string | POS response code |
| `responseMessage` | string | POS response message |
| `transactionId` | string | complete request transaction id |
| `originalTransactionId` | string/null | authorize request transaction id |
| `posTransactionId` | string/null | POS transaction id |
| `authCode` | string/null | onay kodu |
| `hostReferenceNumber` | string/null | host referans numarası |
| `amount` | string/null | authorize amount |
| `currency` | string/null | authorize currency |
| `installmentCount` | number/null | authorize installment count |
| `installmentAmount` | string/null | taksitli onayda hesaplanır |
| `authorizedAt` | string | complete timestamp |
| `threeDsSessionId` | string | session id |
| `threeDsStatus` | string/null | `AUTHENTICATED`, `ATTEMPTED`, `FAILED`, `EXPIRED` |
| `eci` | string/null | `05` authenticated, `06` attempted |
| `messageVersion` | string/null | `2.2.0` |

Example:

```json
{
  "status": "AUTHORIZED",
  "transactionType": "AUTHORIZATION_ONLY",
  "approved": true,
  "responseCode": "00",
  "responseMessage": "Approved",
  "transactionId": "complete_001",
  "originalTransactionId": "attempt_3ds_001",
  "posTransactionId": "pos_txn_8f4c2a1b9d",
  "authCode": "A12345",
  "hostReferenceNumber": "HST202605130001",
  "amount": "1250.50",
  "currency": "TRY",
  "installmentCount": 1,
  "installmentAmount": null,
  "authorizedAt": "2026-05-13T09:05:00Z",
  "threeDsSessionId": "3ds_9f2c4e7b1a",
  "threeDsStatus": "AUTHENTICATED",
  "eci": "05",
  "messageVersion": "2.2.0"
}
```

## Validation Error

HTTP 400 with `status=FAILED`, `responseCode=30`, `responseMessage=Format error`.
