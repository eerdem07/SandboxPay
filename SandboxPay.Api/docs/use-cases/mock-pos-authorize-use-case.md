# Mock POS Authorize Use Case

Status: Draft  
Module: mock-pos  
Related API contract: `mock-pos-authorize-api-contract.md`

---

## 1. Purpose

`AuthorizePayment` use case’i, Mira Payment Gateway’den gelen ödeme denemesini işler ve test kartı numarasına göre deterministik bir POS sonucu döner.

Bu use case gerçek banka POS entegrasyonunu simüle eder.

Şimdilik kapsam:

- 3D Secure yok
- Refund yok
- Capture endpoint’i yok
- Void yok
- Settlement yok
- Payout yok
- Database persistence yok

Tek sorumluluk:

```text
Gateway -> Mock POS authorize request
Mock POS -> deterministic authorize response
```

---

## 2. Actor

Primary actor:

- Mira Payment Gateway Backend

External boundary:

```text
Mira Gateway Backend <-> Mock BankPOS
```

---

## 3. Endpoint

```http
POST /api/v1/pos/authorize
```

Endpoint detayları için:

```text
mock-pos-authorize-api-contract.md
```

---

## 4. Preconditions

Request geçerli olmalıdır:

- `merchantId` zorunlu
- `terminalId` zorunlu
- `orderId` zorunlu
- `transactionId` zorunlu
- `amount` zorunlu ve pozitif olmalı
- `currency` zorunlu
- `installmentCount` en az `1` olmalı
- `capture` zorunlu
- `card.holderName` zorunlu
- `card.pan` zorunlu
- `card.expiryMonth` zorunlu
- `card.expiryYear` zorunlu
- `card.cvv` zorunlu

---

## 5. Main Flow

1. Gateway, Mock POS’a authorize isteği gönderir.

```http
POST /api/v1/pos/authorize
```

2. Mock POS request body’yi validate eder.

3. `card.pan` normalize edilir.

Örnek:

```text
"4111 1111 1111 1111" -> "4111111111111111"
```

4. PAN, test kartı katalogunda aranır.

5. PAN katalogda varsa ilgili POS response code seçilir.

6. PAN katalogda yoksa Luhn validation çalışır.

7. Luhn invalid ise:

```text
responseCode = 14
status = DECLINED
responseMessage = Invalid card number
```

8. Luhn valid ve katalogda yoksa default olarak başarılı işlem döner:

```text
responseCode = 00
```

9. `capture=true` ise başarılı işlem:

```text
status = APPROVED
transactionType = SALE
```

10. `capture=false` ise başarılı işlem:

```text
status = AUTHORIZED
transactionType = AUTHORIZATION_ONLY
```

11. Mock POS response üretir.

---

## 6. Alternative Flows

### 6.1 Card Declined

Test kartı decline response code’a map edilmişse:

```text
status = DECLINED
approved = false
```

Örnek response code’lar:

- `05` Do not honor
- `14` Invalid card number
- `51` Insufficient funds
- `54` Expired card
- `57` Transaction not permitted
- `61` Exceeds amount limit

---

### 6.2 Technical/System Failure

Test kartı technical failure response code’a map edilmişse:

```text
status = FAILED
approved = false
```

Örnek response code’lar:

- `12` Invalid transaction
- `13` Invalid amount
- `30` Format error
- `58` Transaction not permitted to terminal
- `91` Issuer unavailable
- `96` System malfunction
- `TIMEOUT` Bank POS timeout

---

### 6.3 Invalid Request

Request validation başarısız olursa HTTP 400 döner.

Örnek:

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

## 7. Postconditions

Başarılı sale işleminde:

```text
status = APPROVED
transactionType = SALE
approved = true
responseCode = 00
```

Başarılı authorization-only işleminde:

```text
status = AUTHORIZED
transactionType = AUTHORIZATION_ONLY
approved = true
responseCode = 00
```

Declined işlemde:

```text
status = DECLINED
approved = false
```

System failure işlemde:

```text
status = FAILED
approved = false
```

---

## 8. Business Rules

### BR-001: Test card catalog has priority

PAN önce test kartı katalogunda aranmalıdır.

```text
1. Check test card catalog
2. If not found, run Luhn validation
3. If Luhn valid, approve by default
```

---

### BR-002: Unknown valid card is approved

PAN katalogda yoksa ama Luhn valid ise default response:

```text
responseCode = 00
```

---

### BR-003: Unknown invalid card is declined

PAN katalogda yoksa ve Luhn invalid ise:

```text
responseCode = 14
status = DECLINED
```

---

### BR-004: Capture field determines transaction type

```text
capture=true  -> SALE
capture=false -> AUTHORIZATION_ONLY
```

Başarılı durumda:

```text
capture=true  -> status=APPROVED
capture=false -> status=AUTHORIZED
```

---

### BR-005: Do not store card data

Mock POS card data saklamamalıdır.

Özellikle saklanmamalı:

- PAN
- CVV
- expiryMonth
- expiryYear

Log gerekiyorsa PAN maskelenmelidir.

Örnek:

```text
411111******1111
```

---

## 9. Status Values

### PosAuthorizeStatus

```java
public enum PosAuthorizeStatus {
    APPROVED,
    AUTHORIZED,
    DECLINED,
    FAILED
}
```

| Status | Meaning |
|---|---|
| `APPROVED` | `capture=true` ve ödeme başarılı |
| `AUTHORIZED` | `capture=false` ve provizyon başarılı |
| `DECLINED` | Kart/issuer/business kaynaklı red |
| `FAILED` | Teknik/sistem/format/config hatası |

---

### PosTransactionType

```java
public enum PosTransactionType {
    SALE,
    AUTHORIZATION_ONLY
}
```

| Type | Meaning |
|---|---|
| `SALE` | Auth + capture birlikte |
| `AUTHORIZATION_ONLY` | Sadece authorization/provizyon |

---

## 10. Suggested Implementation Structure

```text
mockpos/
  application/
    AuthorizePaymentService
    AuthorizePaymentCommand
    AuthorizePaymentResult

  domain/
    PosAuthorizeStatus
    PosTransactionType
    PosResponseCode
    TestCardScenario
    CardNumber
    LuhnValidator

  web/
    PosAuthorizeController
    AuthorizePaymentRequest
    AuthorizePaymentResponse
    CardRequest
    ValidationErrorResponse

  support/
    PosIdGenerator
```

---

## 11. Out of Scope

Bu use case için şimdilik yapılmayacaklar:

- 3D Secure
- `/3ds/complete`
- Capture endpoint
- Void endpoint
- Refund endpoint
- Settlement
- Payout
- Webhook
- Database persistence
- Real bank integration
- Real card storage
- HMAC signature validation
