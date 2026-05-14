# Mock POS Void Use Case

Status: Draft  
Module: mock-pos  
Related API contract: [`../api/mock-pos-void-api-contract.md`](../api/mock-pos-void-api-contract.md)
Related authorize use case: [`mock-pos-authorize-use-case.md`](mock-pos-authorize-use-case.md)
Related capture use case: [`mock-pos-capture-use-case.md`](mock-pos-capture-use-case.md)

---

## 1. Purpose

`VoidPayment` use case'i, Mira Payment Gateway'den gelen provizyon iptal istegini isler ve daha once basarili sekilde alinmis, henuz capture edilmemis authorization-only provizyonu iptal eder.

Bu use case sadece uncaptured manual capture akisi icindir.

Kapsam:

- Authorization-only provizyonu void etmek
- In-memory authorization metadata uzerinden dogrulama yapmak
- Void operasyonu icin ayri POS void id uretmek
- Void edilen authorization kaydini capture edilemez hale getirmek

Kapsam disi:

- Automatic capture/sale void
- Captured authorization void
- Refund
- Reversal
- Settlement-aware void
- Void idempotency
- Database persistence
- Card data persistence

Not:

- `capture=true` sale islemleri ve `captured=true` authorization islemleri refund use case kapsamindadir.

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
POST /api/v1/pos/void
```

Endpoint detaylari icin [`../api/mock-pos-void-api-contract.md`](../api/mock-pos-void-api-contract.md) dosyasina bakiniz.

Reason:

- Mock POS API operation-based tasarlanmistir.
- Void operasyonu kendi Gateway transaction id'sine sahiptir.
- Original authorization hem Gateway id hem POS id ile request body icinde dogrulanir.

---

## 4. Preconditions

Request gecerli olmalidir:

- `merchantId` zorunlu
- `terminalId` zorunlu
- `orderId` zorunlu
- `transactionId` zorunlu
- `originalTransactionId` zorunlu
- `originalPosTransactionId` zorunlu
- `authCode` zorunlu
- `hostReferenceNumber` zorunlu
- `amount` zorunlu ve pozitif olmali
- `currency` zorunlu

Is kurali precondition'lari:

- Original authorization daha once `capture=false` ile basarili olmalidir.
- Original authorization status degeri `AUTHORIZED` olmalidir.
- Original authorization daha once capture edilmemis olmalidir.
- Original authorization daha once void edilmemis olmalidir.
- Void amount ve currency, original authorization ile ayni olmalidir.

---

## 5. Main Flow

1. Gateway, Mock POS'a void istegi gonderir.

```http
POST /api/v1/pos/void
```

2. Mock POS request body'yi validate eder.

3. `originalTransactionId` ve `originalPosTransactionId` ile in-memory authorization store'da kayit aranir.

4. Kayit bulunursa request'teki su alanlar stored authorization metadata ile karsilastirilir:

- `merchantId`
- `terminalId`
- `orderId`
- `originalTransactionId`
- `originalPosTransactionId`
- `authCode`
- `hostReferenceNumber`
- `amount`
- `currency`

5. Original authorization voidable ise yeni void id uretilir.

```text
posVoidId = pos_void_{random}
```

6. Void icin yeni host reference number uretilir.

```text
hostReferenceNumber = HST{yyyyMMddHHmmss}{shortRandom}
```

7. Original authorization in-memory store'da voided olarak isaretlenir.

8. Mock POS basarili void response doner.

```text
status = VOIDED
transactionType = VOID
approved = true
responseCode = 00
responseMessage = Void approved
```

---

## 6. Alternative Flows

### 6.1 Invalid Request

Request validation basarisiz olursa HTTP 400 doner.

Ornek:

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

### 6.2 Original Authorization Not Found

Original authorization in-memory store'da bulunamazsa HTTP 200 ile POS failure response doner.

```text
status = FAILED
transactionType = VOID
approved = false
responseCode = 12
responseMessage = Invalid transaction
```

Bu durum su sebeplerle olusabilir:

- Yanlis `originalTransactionId`
- Yanlis `originalPosTransactionId`
- Uygulama restart oldugu icin in-memory store'un temizlenmesi
- Original authorization hic basarili olmamis olmasi
- Original authorization `capture=true` sale akisi olmasi

---

### 6.3 Identifier Mismatch

Original authorization bulundugu halde request'teki dogrulama alanlari stored metadata ile eslesmezse:

```text
status = FAILED
transactionType = VOID
approved = false
responseCode = 12
responseMessage = Invalid transaction
```

Eslesmesi gereken alanlar:

- `merchantId`
- `terminalId`
- `orderId`
- `authCode`
- `hostReferenceNumber`

---

### 6.4 Already Captured

Original authorization daha once capture edildiyse:

```text
status = FAILED
transactionType = VOID
approved = false
responseCode = 12
responseMessage = Invalid transaction
```

Captured authorization icin void uygulanmaz. Bu durum ileride refund use case kapsaminda ele alinmalidir.

---

### 6.5 Already Voided

Original authorization daha once void edildiyse:

```text
status = FAILED
transactionType = VOID
approved = false
responseCode = 12
responseMessage = Invalid transaction
```

Ilk versiyonda void idempotency uygulanmaz.

Ayni void request tekrar gonderilirse yeni basarili response donulmez.

Gelecek versiyonda basarili void operasyon metadata'si saklanarak ayni request icin idempotent response donulebilir.

---

### 6.6 Invalid Amount Or Currency

Void amount veya currency original authorization ile ayni degilse:

```text
status = FAILED
transactionType = VOID
approved = false
responseCode = 13
responseMessage = Invalid amount
```

---

### 6.7 Authorization Expired

Original authorization expiry suresi gecmisse:

```text
status = FAILED
transactionType = VOID
approved = false
responseCode = 12
responseMessage = Invalid transaction
```

Not:

- Gateway tarafi `authorizationExpiresAt` degerini set eder.
- Mock POS tarafi ilk implementation'da authorization record uzerinde internal expiry tutabilir.
- Onerilen mock expiry: `authorizedAt + 7 days`.

---

## 7. Postconditions

Basarili void isleminde:

```text
status = VOIDED
transactionType = VOID
approved = true
responseCode = 00
```

Original authorization store'da voided olarak isaretlenir.

Void edilen authorization daha sonra capture edilemez.

Gateway tarafinda beklenen state gecisleri:

```text
Void -> SUCCEEDED
PaymentAttempt -> CANCELED
PaymentIntent -> CANCELED
```

Basarisiz void isleminde:

```text
status = FAILED
transactionType = VOID
approved = false
```

Gateway tarafinda ilgili void operasyonu failed olarak degerlendirilebilir. Original authorization state'i degismeden kalir.

---

## 8. Business Rules

### BR-001: Void only applies to uncaptured authorization-only payments

Void endpoint sadece su authorize response icin gecerlidir:

```text
status = AUTHORIZED
transactionType = AUTHORIZATION_ONLY
approved = true
responseCode = 00
captured = false
voided = false
```

Su islemler icin void endpoint cagrilmaz:

```text
status = APPROVED
transactionType = SALE
approved = true
responseCode = 00
```

```text
status = CAPTURED
transactionType = CAPTURE
approved = true
responseCode = 00
```

---

### BR-002: Sale and captured payments are refund candidates

`capture=true` ile yapilan authorize islemi sale flow'dur.

`captured=true` olan authorization artik tamamlanmis odeme kabul edilir.

Bu islemler icin iptal/iade ihtiyaci refund use case kapsaminda ele alinmalidir.

---

### BR-003: Void requires amount and currency match

Ilk versiyonda void amount ve currency, original authorization amount ve currency degerleri ile ayni olmalidir.

```text
void.amount == authorization.amount
void.currency == authorization.currency
```

---

### BR-004: Void is not idempotent in first version

Ilk versiyonda ayni authorization icin sadece bir basarili void yapilabilir.

Original authorization voided olduktan sonra ikinci void attempt:

```text
responseCode = 12
responseMessage = Invalid transaction
```

Gelecek versiyonda void idempotency eklenebilir.

---

### BR-005: Voided authorization cannot be captured

Void edilen authorization icin sonradan capture cagrisi basarisiz olmalidir.

```text
responseCode = 12
responseMessage = Invalid transaction
```

---

### BR-006: Use in-memory authorization store

Ilk implementation in-memory store kullanabilir.

Authorize use case, basarili authorization-only islemde void icin gerekli metadata'yi da kaydetmelidir.

Kaydedilecek alanlar:

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

Kaydedilmeyecek alanlar:

- PAN
- CVV
- expiryMonth
- expiryYear
- card holder name

---

## 9. Status Values

### PosVoidStatus

```java
public enum PosVoidStatus {
    VOIDED,
    FAILED
}
```

| Status | Meaning |
|---|---|
| `VOIDED` | Void basarili |
| `FAILED` | Void basarisiz |

---

### PosTransactionType

Void implementation sonrasinda `PosTransactionType` su degerleri desteklemelidir:

```java
public enum PosTransactionType {
    SALE,
    AUTHORIZATION_ONLY,
    CAPTURE,
    VOID
}
```

| Type | Meaning |
|---|---|
| `SALE` | Auth + capture birlikte |
| `AUTHORIZATION_ONLY` | Sadece authorization/provizyon |
| `CAPTURE` | Onceden alinmis provizyonun sonradan capture edilmesi |
| `VOID` | Henuz capture edilmemis provizyonun iptal edilmesi |

---

## 10. Suggested Implementation Structure

```text
mockpos/
  application/
    AuthorizePaymentService
    AuthorizePaymentCommand
    AuthorizePaymentResult
    CapturePaymentService
    CapturePaymentCommand
    CapturePaymentResult
    VoidPaymentService
    VoidPaymentCommand
    VoidPaymentResult
    AuthorizedPaymentStore
    AuthorizedPaymentRecord

  domain/
    PosAuthorizeStatus
    PosCaptureStatus
    PosVoidStatus
    PosTransactionType
    PosResponseCode

  web/
    PosAuthorizeController
    PosCaptureController
    PosVoidController

    contracts/
      AuthorizePaymentRequest
      AuthorizePaymentResponse
      CapturePaymentRequest
      CapturePaymentResponse
      VoidPaymentRequest
      VoidPaymentResponse
      ValidationError
      ValidationErrorResponse

  support/
    PosIdGenerator
```

---

## 11. Out of Scope

Bu use case icin simdilik yapilmayacaklar:

- Sale void
- Captured authorization void
- Void idempotency
- Refund
- Reversal
- Settlement-aware void
- Partial capture
- Multiple capture
- Capture idempotency
- Payout
- Webhook
- Database persistence
- Real bank integration
- Real card storage
- HMAC signature validation
