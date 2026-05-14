# Mock POS Capture Use Case

Status: Draft  
Module: mock-pos  
Related API contract: [`../api/mock-pos-capture-api-contract.md`](../api/mock-pos-capture-api-contract.md)
Related authorize use case: [`mock-pos-authorize-use-case.md`](mock-pos-authorize-use-case.md)

---

## 1. Purpose

`CapturePayment` use case’i, Mira Payment Gateway’den gelen sonradan capture isteğini işler ve daha önce başarılı şekilde alınmış authorization-only provizyonu finalize eder.

Bu use case sadece manual capture akışı içindir.

Kapsam:

- Authorization-only işlemi sonradan capture etmek
- In-memory authorization metadata üzerinden doğrulama yapmak
- Full capture yapmak
- Capture operasyonu için ayrı POS capture id üretmek

Kapsam dışı:

- Automatic capture/sale işlemi için capture kaydı oluşturmak
- Partial capture
- Multiple capture
- Void
- Refund
- Settlement
- Database persistence
- Card data persistence

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
POST /api/v1/pos/capture
```

Endpoint detayları için [`../api/mock-pos-capture-api-contract.md`](../api/mock-pos-capture-api-contract.md) dosyasına bakınız.

Reason:

- Mock POS API operation-based tasarlanmıştır.
- Capture operasyonu kendi Gateway transaction id’sine sahiptir.
- Original authorization hem Gateway id hem POS id ile request body içinde doğrulanır.

---

## 4. Preconditions

Request geçerli olmalıdır:

- `merchantId` zorunlu
- `terminalId` zorunlu
- `orderId` zorunlu
- `transactionId` zorunlu
- `originalTransactionId` zorunlu
- `originalPosTransactionId` zorunlu
- `authCode` zorunlu
- `hostReferenceNumber` zorunlu
- `amount` zorunlu ve pozitif olmalı
- `currency` zorunlu

İş kuralı precondition’ları:

- Original authorization daha önce `capture=false` ile başarılı olmalıdır.
- Original authorization status değeri `AUTHORIZED` olmalıdır.
- Original authorization daha önce capture edilmemiş olmalıdır.
- Capture amount ve currency, original authorization ile aynı olmalıdır.

---

## 5. Main Flow

1. Gateway, Mock POS’a capture isteği gönderir.

```http
POST /api/v1/pos/capture
```

2. Mock POS request body’yi validate eder.

3. `originalTransactionId` ve `originalPosTransactionId` ile in-memory authorization store’da kayıt aranır.

4. Kayıt bulunursa request’teki şu alanlar stored authorization metadata ile karşılaştırılır:

- `merchantId`
- `terminalId`
- `orderId`
- `originalTransactionId`
- `originalPosTransactionId`
- `authCode`
- `hostReferenceNumber`
- `amount`
- `currency`

5. Original authorization capturable ise yeni capture id üretilir.

```text
posCaptureId = pos_cap_{random}
```

6. Capture için yeni host reference number üretilir.

```text
hostReferenceNumber = HST{yyyyMMddHHmmss}{shortRandom}
```

7. Original authorization in-memory store’da captured olarak işaretlenir.

8. Mock POS başarılı capture response döner.

```text
status = CAPTURED
transactionType = CAPTURE
approved = true
responseCode = 00
responseMessage = Capture approved
```

---

## 6. Alternative Flows

### 6.1 Invalid Request

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

### 6.2 Original Authorization Not Found

Original authorization in-memory store’da bulunamazsa HTTP 200 ile POS failure response döner.

```text
status = FAILED
transactionType = CAPTURE
approved = false
responseCode = 12
responseMessage = Invalid transaction
```

Bu durum şu sebeplerle oluşabilir:

- Yanlış `originalTransactionId`
- Yanlış `originalPosTransactionId`
- Uygulama restart olduğu için in-memory store’un temizlenmesi
- Original authorization hiç başarılı olmamış olması

---

### 6.3 Identifier Mismatch

Original authorization bulunduğu halde request’teki doğrulama alanları stored metadata ile eşleşmezse:

```text
status = FAILED
transactionType = CAPTURE
approved = false
responseCode = 12
responseMessage = Invalid transaction
```

Eşleşmesi gereken alanlar:

- `merchantId`
- `terminalId`
- `orderId`
- `authCode`
- `hostReferenceNumber`

---

### 6.4 Already Captured

Original authorization daha önce capture edildiyse:

```text
status = FAILED
transactionType = CAPTURE
approved = false
responseCode = 12
responseMessage = Invalid transaction
```

İlk versiyonda capture idempotency uygulanmaz.

Aynı capture request tekrar gönderilirse yeni başarılı response dönülmez.

---

### 6.5 Invalid Amount Or Currency

Capture amount veya currency original authorization ile aynı değilse:

```text
status = FAILED
transactionType = CAPTURE
approved = false
responseCode = 13
responseMessage = Invalid amount
```

İlk versiyon sadece full capture destekler.

---

### 6.6 Authorization Expired

Original authorization expiry süresi geçmişse:

```text
status = FAILED
transactionType = CAPTURE
approved = false
responseCode = 12
responseMessage = Invalid transaction
```

Not:

- Gateway tarafı `authorizationExpiresAt` değerini set eder.
- Mock POS tarafı ilk implementation’da authorization record üzerinde internal expiry tutabilir.
- Önerilen mock expiry: `authorizedAt + 7 days`.

---

## 7. Postconditions

Başarılı capture işleminde:

```text
status = CAPTURED
transactionType = CAPTURE
approved = true
responseCode = 00
```

Original authorization store’da captured olarak işaretlenir.

Gateway tarafında beklenen state geçişleri:

```text
Capture -> SUCCEEDED
PaymentAttempt -> SUCCEEDED
PaymentIntent -> SUCCEEDED
```

Başarısız capture işleminde:

```text
status = FAILED
transactionType = CAPTURE
approved = false
```

Gateway tarafında ilgili capture operasyonu failed olarak değerlendirilebilir. Original authorization state’i capture edilmemiş olarak kalır.

---

## 8. Business Rules

### BR-001: Capture only applies to authorization-only payments

Capture endpoint sadece şu authorize response için geçerlidir:

```text
status = AUTHORIZED
transactionType = AUTHORIZATION_ONLY
approved = true
responseCode = 00
```

Şu işlem için capture endpoint çağrılmaz:

```text
status = APPROVED
transactionType = SALE
approved = true
responseCode = 00
```

---

### BR-002: Automatic capture does not create Capture entity

`capture=true` ile yapılan authorize işlemi sale flow’dur.

Gateway tarafında:

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

### BR-003: Authorization-only creates capturable state

`capture=false` ile yapılan başarılı authorize işlemi manual capture flow’dur.

Gateway tarafında:

```text
PaymentAttempt -> AUTHORIZED
PaymentIntent -> REQUIRES_CAPTURE
authorizationExpiresAt set edilir
```

Sonrasında:

```http
POST /api/v1/pos/capture
```

---

### BR-004: Full capture only

İlk versiyonda capture amount, original authorization amount ile aynı olmalıdır.

```text
capture.amount == authorization.amount
capture.currency == authorization.currency
```

Partial capture out of scope.

---

### BR-005: Capture is not idempotent in first version

İlk versiyonda aynı authorization için sadece bir başarılı capture yapılabilir.

Original authorization captured olduktan sonra ikinci capture attempt:

```text
responseCode = 12
responseMessage = Invalid transaction
```

---

### BR-006: Use in-memory authorization store

İlk implementation in-memory store kullanabilir.

Authorize use case, sadece başarılı authorization-only işlemde metadata kaydetmelidir.

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

Kaydedilmeyecek alanlar:

- PAN
- CVV
- expiryMonth
- expiryYear
- card holder name

---

## 9. Status Values

### PosCaptureStatus

```java
public enum PosCaptureStatus {
    CAPTURED,
    FAILED
}
```

| Status | Meaning |
|---|---|
| `CAPTURED` | Capture başarılı |
| `FAILED` | Capture başarısız |

---

### PosTransactionType

Capture implementation sonrasında `PosTransactionType` şu değerleri desteklemelidir:

```java
public enum PosTransactionType {
    SALE,
    AUTHORIZATION_ONLY,
    CAPTURE
}
```

| Type | Meaning |
|---|---|
| `SALE` | Auth + capture birlikte |
| `AUTHORIZATION_ONLY` | Sadece authorization/provizyon |
| `CAPTURE` | Önceden alınmış provizyonun sonradan capture edilmesi |

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
    AuthorizedPaymentStore
    AuthorizedPaymentRecord

  domain/
    PosAuthorizeStatus
    PosCaptureStatus
    PosTransactionType
    PosResponseCode

  web/
    PosAuthorizeController
    PosCaptureController

    contracts/
      AuthorizePaymentRequest
      AuthorizePaymentResponse
      CapturePaymentRequest
      CapturePaymentResponse
      ValidationError
      ValidationErrorResponse

  support/
    PosIdGenerator
```

---

## 11. Out of Scope

Bu use case için şimdilik yapılmayacaklar:

- Partial capture
- Multiple capture
- Capture idempotency
- Void
- Refund
- Settlement
- Payout
- Webhook
- Database persistence
- Real bank integration
- Real card storage
- HMAC signature validation
