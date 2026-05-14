# Mock POS Refund Use Case

Status: Draft  
Module: mock-pos  
Related API contract: [`../api/mock-pos-refund-api-contract.md`](../api/mock-pos-refund-api-contract.md)
Related authorize use case: [`mock-pos-authorize-use-case.md`](mock-pos-authorize-use-case.md)
Related capture use case: [`mock-pos-capture-use-case.md`](mock-pos-capture-use-case.md)
Related void use case: [`mock-pos-void-use-case.md`](mock-pos-void-use-case.md)

---

## 1. Purpose

`RefundPayment` use case'i, Mira Payment Gateway'den gelen iade talebini işler ve daha önce tamamlanmış bir ödemeyi iptal eder.

Bu use case sadece tamamlanmış ödemeler içindir:

- `capture=true` ile yapılmış `SALE` işlemleri iade edilebilir.
- `capture=false` ile yapılmış ve sonrasında capture edilmiş `CAPTURED` işlemler iade edilebilir.

Kapsam:

- SALE ve CAPTURED işlemleri için tam iade (full refund)
- In-memory authorization metadata üzerinden doğrulama
- Refund operasyonu için ayrı POS refund id üretmek
- İade edilen authorization kaydını tekrar iade edilemez hale getirmek
- SALE işlemlerinin de authorization store'a kaydedilmesi

Kapsam dışı:

- AUTHORIZED (uncaptured) işlem iadesi → void use case kapsamındadır
- Kısmi iade (partial refund)
- Çoklu iade (multiple refunds)
- Refund idempotency
- Settlement-aware refund
- Database persistence

Not:

- AUTHORIZED işlemler için iade gereksiniminde void endpoint kullanılmalıdır.

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
POST /api/v1/pos/refund
```

Endpoint detayları için [`../api/mock-pos-refund-api-contract.md`](../api/mock-pos-refund-api-contract.md) dosyasına bakınız.

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

İş kuralı precondition'ları:

- Orijinal işlem ya `capture=true` SALE ya da sonradan capture edilmiş AUTHORIZATION_ONLY olmalıdır.
- Orijinal işlem daha önce void edilmemiş olmalıdır.
- Orijinal işlem daha önce iade edilmemiş olmalıdır.
- Refund amount ve currency, orijinal authorization ile aynı olmalıdır.

---

## 5. Main Flow

1. Gateway, Mock POS'a refund isteği gönderir.

```http
POST /api/v1/pos/refund
```

2. Mock POS request body'yi validate eder.

3. `originalTransactionId` ile in-memory authorization store'da kayıt aranır.

4. Kayıt bulunursa request'teki şu alanlar stored authorization metadata ile karşılaştırılır:

- `merchantId`
- `terminalId`
- `orderId`
- `originalTransactionId`
- `originalPosTransactionId`
- `authCode`
- `hostReferenceNumber`
- `amount`
- `currency`

5. Orijinal authorization refundable ise yeni refund id üretilir.

```text
posRefundId = pos_ref_{random}
```

6. Refund için yeni host reference number üretilir.

```text
hostReferenceNumber = HST{yyyyMMddHHmmss}{shortRandom}
```

7. Orijinal authorization in-memory store'da refunded olarak işaretlenir.

8. Mock POS başarılı refund response döner.

```text
status = REFUNDED
transactionType = REFUND
approved = true
responseCode = 00
responseMessage = Refund approved
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

Orijinal authorization in-memory store'da bulunamazsa HTTP 200 ile POS failure response döner.

```text
status = FAILED
transactionType = REFUND
approved = false
responseCode = 12
responseMessage = Invalid transaction
```

Bu durum şu sebeplerle oluşabilir:

- Yanlış `originalTransactionId`
- Uygulama restart olduğu için in-memory store'un temizlenmesi
- Orijinal authorization hiç başarılı olmamış olması
- Orijinal işlem AUTHORIZATION_ONLY olup henüz capture edilmemiş olması

---

### 6.3 Identifier Mismatch

Orijinal authorization bulunduğu halde request'teki doğrulama alanları stored metadata ile eşleşmezse:

```text
status = FAILED
transactionType = REFUND
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

### 6.4 Not Refundable (AUTHORIZED)

Orijinal authorization `captured=false` ise iade edilemez. Void endpoint kullanılmalıdır.

```text
status = FAILED
transactionType = REFUND
approved = false
responseCode = 12
responseMessage = Invalid transaction
```

---

### 6.5 Already Voided

Orijinal authorization daha önce void edildiyse:

```text
status = FAILED
transactionType = REFUND
approved = false
responseCode = 12
responseMessage = Invalid transaction
```

---

### 6.6 Already Refunded

Orijinal authorization daha önce iade edildiyse:

```text
status = FAILED
transactionType = REFUND
approved = false
responseCode = 12
responseMessage = Invalid transaction
```

İlk versiyonda refund idempotency uygulanmaz.

Gelecek versiyonda başarılı refund operasyon metadata'sı saklanarak aynı request için idempotent response dönülebilir.

---

### 6.7 Invalid Amount Or Currency

Refund amount veya currency orijinal authorization ile aynı değilse:

```text
status = FAILED
transactionType = REFUND
approved = false
responseCode = 13
responseMessage = Invalid amount
```

---

## 7. Postconditions

Başarılı refund işleminde:

```text
status = REFUNDED
transactionType = REFUND
approved = true
responseCode = 00
```

Orijinal authorization store'da refunded olarak işaretlenir.

İade edilen authorization tekrar iade edilemez.

Gateway tarafında beklenen state geçişleri:

```text
Refund -> SUCCEEDED
PaymentAttempt -> REFUNDED
PaymentIntent -> REFUNDED
```

Başarısız refund işleminde:

```text
status = FAILED
transactionType = REFUND
approved = false
```

Gateway tarafında ilgili refund operasyonu failed olarak değerlendirilebilir. Orijinal authorization state'i değişmeden kalır.

---

## 8. Business Rules

### BR-001: Refund only applies to captured or sale payments

Void endpoint sadece şu authorize response için geçerlidir:

```text
captured = true (SALE veya sonradan CAPTURED)
voided = false
refunded = false
```

Şu işlemler için refund endpoint çağrılmaz:

```text
status = AUTHORIZED
transactionType = AUTHORIZATION_ONLY
captured = false
```

Bu işlemler için void endpoint kullanılmalıdır.

---

### BR-002: SALE transactions must be stored at authorization time

`capture=true` ile yapılmış authorize işlemleri, ileride iade edilebilmesi için authorization store'a `Captured=true` olarak kaydedilmelidir.

Bu, mevcut `ShouldStoreAuthorization` mantığının genişletilmesini gerektirir.

```text
Önceki kural: sadece AUTHORIZATION_ONLY kaydet
Yeni kural: AUTHORIZATION_ONLY ve SALE kaydet
SALE için Captured=true ile kaydet
```

---

### BR-003: Refund requires amount and currency match

İlk versiyonda refund amount ve currency, orijinal authorization amount ve currency değerleri ile aynı olmalıdır.

```text
refund.amount == authorization.amount
refund.currency == authorization.currency
```

---

### BR-004: Refund is not idempotent in first version

İlk versiyonda aynı authorization için sadece bir başarılı refund yapılabilir.

Orijinal authorization refunded olduktan sonra ikinci refund attempt:

```text
responseCode = 12
responseMessage = Invalid transaction
```

Gelecek versiyonda refund idempotency eklenebilir.

---

### BR-005: Voided authorization cannot be refunded

Void edilen authorization için sonradan refund çağrısı başarısız olmalıdır.

```text
responseCode = 12
responseMessage = Invalid transaction
```

---

### BR-006: Use in-memory authorization store

İlk implementation in-memory store kullanır.

Authorize use case, başarılı SALE işlemlerde de refund için gerekli metadata'yı kaydetmelidir.

Store'da `refunded` flag eklenmesi gerekir.

Kaydedilecek alanlar (mevcut alanlara ek):

- `refunded`

Kaydedilmeyecek alanlar:

- PAN
- CVV
- expiryMonth
- expiryYear
- card holder name

---

### BR-007: Use original authorization identifiers for refund

Hem SALE hem CAPTURED işlemler için refund request'inde orijinal authorization response'undaki identifier'lar kullanılmalıdır.

CAPTURED işlem için capture response'undaki `posCaptureId` veya capture'ın `hostReferenceNumber`'ı değil, orijinal authorization'ın identifier'ları kullanılır.

---

## 9. Status Values

### PosRefundStatus

```csharp
public enum PosRefundStatus
{
    REFUNDED,
    FAILED
}
```

| Status | Meaning |
|---|---|
| `REFUNDED` | İade başarılı |
| `FAILED` | İade başarısız |

---

### PosTransactionType

Refund implementation sonrasında `PosTransactionType` şu değerleri desteklemelidir:

```csharp
public enum PosTransactionType
{
    SALE,
    AUTHORIZATION_ONLY,
    CAPTURE,
    VOID,
    REFUND
}
```

| Type | Meaning |
|---|---|
| `SALE` | Auth + capture birlikte |
| `AUTHORIZATION_ONLY` | Sadece authorization/provizyon |
| `CAPTURE` | Önceden alınmış provizyonun sonradan capture edilmesi |
| `VOID` | Henüz capture edilmemiş provizyonun iptal edilmesi |
| `REFUND` | Tamamlanmış ödemenin iadesi |

---

## 10. Store Changes Required

Refund implementasyonu için mevcut authorization store'da şu değişiklikler gereklidir:

### PosAuthorization record

`Refunded` bool field eklenmesi:

```csharp
public sealed record PosAuthorization(
    string MerchantId,
    string TerminalId,
    string OrderId,
    string TransactionId,
    string PosTransactionId,
    string AuthCode,
    string HostReferenceNumber,
    string Amount,
    string Currency,
    DateTimeOffset AuthorizedAt,
    DateTimeOffset AuthorizationExpiresAt,
    bool Captured,
    bool Voided,
    bool Refunded);
```

### IPosAuthorizationStore interface

`TryMarkRefunded` method eklenmesi:

```csharp
bool TryMarkRefunded(string transactionId, string posTransactionId, out PosAuthorization? authorization);
```

### AuthorizePaymentService

`ShouldStoreAuthorization` genişletilmesi — SALE işlemleri de `Captured=true` ile store'a kaydedilmeli:

```text
Önceki: sadece AUTHORIZATION_ONLY ve Approved
Yeni: AUTHORIZATION_ONLY ve Approved → Captured=false
      SALE ve Approved → Captured=true
```

---

## 11. Suggested Implementation Structure

```text
mockpos/
  application/
    AuthorizePaymentService     (modified: store SALE with Captured=true)
    CapturePaymentService
    VoidPaymentService
    RefundPaymentService        (new)
    RefundPaymentCommand        (new)
    RefundPaymentResult         (new)
    IPosAuthorizationStore      (modified: TryMarkRefunded)
    PosAuthorization            (modified: Refunded field)

  domain/
    PosAuthorizeStatus
    PosCaptureStatus
    PosVoidStatus
    PosRefundStatus             (new)
    PosTransactionType          (modified: REFUND added)
    PosResponseCode

  web/
    PosAuthorizeController
    PosCaptureController
    PosVoidController
    PosRefundController         (new)

    contracts/
      RefundPaymentRequest      (new)
      RefundPaymentResponse     (new)

  infrastructure/
    InMemoryPosAuthorizationStore (modified: TryMarkRefunded, Refunded field)

  support/
    PosIdGenerator              (modified: GeneratePosRefundId)
```

---

## 12. Out of Scope

Bu use case için şimdilik yapılmayacaklar:

- Kısmi iade (partial refund)
- Aynı işlem için çoklu iade
- Refund idempotency
- AUTHORIZED işlem iadesi (void endpoint kullanılmalı)
- Sale void
- Settlement-aware refund
- Payout
- Webhook
- Database persistence
- Real bank integration
- Real card storage
- HMAC signature validation
