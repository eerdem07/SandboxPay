# Mock POS Installment API Contract

Module: mock-pos

## Endpoint

Installment ayrı bir endpoint değildir. `POST /api/v1/pos/authorize` request'indeki `installmentCount` alanı ile çalışır.

```http
POST /api/v1/pos/authorize
Content-Type: application/json
```

## Request Rules

| Field | Type | Required | Rule |
|---|---|---|---|
| `installmentCount` | number | yes | 1-12 arası |
| `amount` | string | yes | pozitif decimal |
| `capture` | boolean | yes | sale veya authorization-only akışı |
| `card.pan` | string | yes | taksit katalogu veya standart kart katalogu ile çözülür |

`installmentCount=1` tek çekimdir. `installmentCount>=2` taksitli işlemdir.

## Response Rules

Authorize response contract'i `mock-pos-authorize-api-contract.md` ile aynıdır.

| Scenario | Response |
|---|---|
| `installmentCount=1` | `installmentAmount=null` |
| `installmentCount>=2` ve işlem onaylandı | `installmentAmount=amount/installmentCount` |
| kart taksit desteklemiyor | `status=DECLINED`, `responseCode=62` |
| kart maksimum taksit limitini aşıyor | `status=DECLINED`, `responseCode=62` |
| `installmentCount>12` | HTTP 400, `responseCode=30` |

Example approved installment:

```json
{
  "status": "APPROVED",
  "transactionType": "SALE",
  "approved": true,
  "responseCode": "00",
  "responseMessage": "Approved",
  "transactionId": "attempt_installment_001",
  "posTransactionId": "pos_txn_8f4c2a1b9d",
  "authCode": "A12345",
  "hostReferenceNumber": "HST202605130001",
  "amount": "1250.50",
  "currency": "TRY",
  "installmentCount": 3,
  "installmentAmount": "416.83",
  "authorizedAt": "2026-05-13T09:00:00Z",
  "threeDsSessionId": null,
  "acsUrl": null,
  "threeDsFlow": null,
  "messageVersion": null,
  "expiresAt": null
}
```

Example restricted installment:

```json
{
  "status": "DECLINED",
  "transactionType": "SALE",
  "approved": false,
  "responseCode": "62",
  "responseMessage": "Restricted card",
  "transactionId": "attempt_installment_002",
  "posTransactionId": "pos_txn_8f4c2a1b9d",
  "authCode": null,
  "hostReferenceNumber": "HST202605130002",
  "amount": "1250.50",
  "currency": "TRY",
  "installmentCount": 6,
  "installmentAmount": null,
  "authorizedAt": null,
  "threeDsSessionId": null,
  "acsUrl": null,
  "threeDsFlow": null,
  "messageVersion": null,
  "expiresAt": null
}
```

## Test Cards

| PAN | MaxInstallmentCount | Scenario |
|---|---|---|
| `4000000000006000` | `0` | Debit kart, taksit desteklenmez; tek çekim onaylanır |
| `4000000000006003` | `3` | En fazla 3 taksit; limit içinde onaylanır |

## Validation Error

HTTP 400 with `status=FAILED`, `responseCode=30`, `responseMessage=Format error`.
