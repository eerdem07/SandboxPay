# Mock POS 3DS Use Case

Status: Draft
Module: mock-pos
Related API contract: [`../api/mock-pos-3ds-api-contract.md`](../api/mock-pos-3ds-api-contract.md)
Related authorize use case: `mock-pos-authorize-use-case.md`

---

## 1. Purpose

`3DS Payment` use case'i, Mira Payment Gateway'den gelen 3D Secure destekli ödeme talebini iki adımda işler:

1. `/api/v1/pos/authorize` → kart 3DS gerektiriyorsa `PENDING_3DS` döner, 3DS session başlatılır.
2. `/api/v1/pos/3ds/complete` → Gateway, challenge veya frictionless akışı tamamladıktan sonra çağırır; nihai POS sonucu döner.

Desteklenen 3DS versiyonu: **3DS2** (`messageVersion=2.2.0`). 3DS1 kapsam dışıdır.

Desteklenen akışlar:

- Frictionless approved — kullanıcı etkileşimi gerekmez, session doğrudan tamamlanır.
- Challenge approved — kullanıcı mock ACS'de doğrulama yapar, complete sonrası onaylanır.
- Challenge failed auth — kullanıcı doğrulamayı tamamlayamaz, ödeme reddedilir.
- 3DS unavailable / attempted — ACS erişilemiyor, fallback ile authorization devam eder (eci=06).
- Challenge timeout — ACS hiç yanıt vermez, session expire olur, ödeme başarısız olur.
- Frictionless declined — 3DS authentication başarılı ama authorization reddedilir.

Non-3DS kartlar mevcut `/authorize` akışından geçmeye devam eder. Bu use case sadece 3DS test kartlarını kapsar.

---

## 2. Actor

Primary actor:

- Mira Payment Gateway Backend

External boundary:

```text
Mira Gateway Backend <-> Mock BankPOS
```

---

## 3. Endpoints

```http
POST /api/v1/pos/authorize          (mevcut, 3DS dalı ekleniyor)
POST /api/v1/pos/3ds/complete       (yeni)
```

Endpoint detayları için [`../api/mock-pos-3ds-api-contract.md`](../api/mock-pos-3ds-api-contract.md) dosyasına bakınız.

---

## 4. Preconditions

### 4.1 Authorize preconditions

Mevcut `/authorize` precondition'ları geçerlidir:

- Tüm zorunlu alanlar dolu olmalıdır.
- `card.pan` 3DS test katalogunda yer almalıdır.

### 4.2 Complete preconditions

Request geçerli olmalıdır:

- `merchantId` zorunlu
- `terminalId` zorunlu
- `orderId` zorunlu
- `transactionId` zorunlu
- `threeDsSessionId` zorunlu

İş kuralı precondition'ları:

- Belirtilen `threeDsSessionId` in-memory session store'da bulunmalıdır.
- Session daha önce tamamlanmamış olmalıdır.
- Request'teki `merchantId`, `terminalId`, `orderId` session'daki değerlerle eşleşmelidir.

---

## 5. Main Flow (Challenge Approved)

1. Gateway, Mock POS'a authorize isteği gönderir.

```http
POST /api/v1/pos/authorize
capture: false
card.pan: 4000000000003014  (CHALLENGE_APPROVED)
```

2. Mock POS kartı 3DS katalogunda tanır. Yeni bir 3DS session oluşturur.

```text
threeDsSessionId = 3ds_{random}
expiresAt = şimdiki zaman + 15 dakika
threeDsFlow = CHALLENGE
```

3. Mock POS `PENDING_3DS` response döner.

```text
status = PENDING_3DS
transactionType = AUTHORIZATION_ONLY
approved = false
responseCode = PENDING
threeDsSessionId = 3ds_9f2c4e7b1a
acsUrl = http://localhost:5102/mock-acs?sessionId=3ds_9f2c4e7b1a
threeDsFlow = CHALLENGE
messageVersion = 2.2.0
expiresAt = 2026-05-13T09:15:00Z
```

4. Gateway, kullanıcıyı `acsUrl`'e yönlendirir. Kullanıcı mock ACS'de "doğrulama yapar".

5. Gateway, Mock POS'a complete isteği gönderir.

```http
POST /api/v1/pos/3ds/complete
threeDsSessionId: 3ds_9f2c4e7b1a
transactionId: complete_001
```

6. Mock POS session'ı bulur, `CHALLENGE_APPROVED` senaryosunu uygular. Authorization gerçekleştirilir.

7. Mock POS nihai sonucu döner.

```text
status = AUTHORIZED
transactionType = AUTHORIZATION_ONLY
approved = true
responseCode = 00
threeDsStatus = AUTHENTICATED
eci = 05
messageVersion = 2.2.0
```

---

## 6. Alternative Flows

### 6.1 Frictionless Approved

Kart: `4000000000003006`

`/authorize` → `PENDING_3DS` with `threeDsFlow=FRICTIONLESS`

Gateway kullanıcıyı yönlendirmez, doğrudan `/3ds/complete` çağırır.

`/3ds/complete` →

```text
status = AUTHORIZED (veya APPROVED, capture=true ise)
threeDsStatus = AUTHENTICATED
eci = 05
```

---

### 6.2 Challenge Failed Auth

Kart: `4000000000003022`

`/authorize` → `PENDING_3DS` with `threeDsFlow=CHALLENGE`

Gateway kullanıcıyı yönlendirir. Kullanıcı doğrulamayı tamamlayamaz.

`/3ds/complete` →

```text
status = DECLINED
approved = false
responseCode = 3DS_AUTH_FAILED
responseMessage = 3DS authentication failed
threeDsStatus = FAILED
eci = null
posTransactionId = null
authCode = null
hostReferenceNumber = null
```

Not: 3DS authentication başarısız olduğunda authorization hiç gönderilmez; bu nedenle POS identifier üretilmez.

---

### 6.3 3DS Unavailable / Attempted

Kart: `4000000000003030`

`/authorize` → `PENDING_3DS` with `threeDsFlow=ATTEMPTED`

ACS erişilemiyor. Gateway doğrudan `/3ds/complete` çağırır.

`/3ds/complete` →

```text
status = AUTHORIZED (veya APPROVED, capture=true ise)
approved = true
responseCode = 00
threeDsStatus = ATTEMPTED
eci = 06
```

Not: Gateway tarafı, ATTEMPTED sonucu kendi risk politikasına göre değerlendirmelidir. Mock POS bu kararı Gateway'e bırakır; authorization devam ettirir. İleride `strict3ds` policy eklenirse aynı senaryo `DECLINED` dönebilir.

---

### 6.4 Challenge Timeout / Session Expired

Kart: `4000000000003048`

`/authorize` → `PENDING_3DS` with `threeDsFlow=TIMEOUT`

ACS hiç yanıt vermez. Gateway `/3ds/complete` çağırır.

`/3ds/complete` →

```text
status = FAILED
approved = false
responseCode = 3DS_TIMEOUT
responseMessage = 3DS session expired
threeDsStatus = EXPIRED
eci = null
posTransactionId = null
```

Not: Mock implementasyonunda, `threeDsFlow=TIMEOUT` olan her session complete çağrısında her zaman `FAILED/EXPIRED` döner. Gerçek zamanlayıcı gerekmez.

---

### 6.5 Frictionless Declined

Kart: `4000000000003055`

`/authorize` → `PENDING_3DS` with `threeDsFlow=FRICTIONLESS`

Gateway doğrudan `/3ds/complete` çağırır. 3DS authentication başarılı, ancak authorization issuer tarafından reddedilir.

`/3ds/complete` →

```text
status = DECLINED
approved = false
responseCode = 05
responseMessage = Do not honor
threeDsStatus = AUTHENTICATED
eci = 05
posTransactionId = pos_txn_{random}   (declined'da üretilir)
authCode = null
hostReferenceNumber = HST{...}        (declined'da üretilir)
```

Not: 3DS authentication başarılı olduğu için authorization gönderildi. Issuer reddetti. Bu nedenle `posTransactionId` ve `hostReferenceNumber` üretilir ama `authCode` üretilmez.

---

### 6.6 Validation Error

Request zorunlu alanlardan biri boşsa HTTP 400 döner.

```json
{
  "status": "FAILED",
  "approved": false,
  "responseCode": "30",
  "responseMessage": "Format error",
  "errors": [
    {
      "field": "threeDsSessionId",
      "message": "threeDsSessionId must not be blank"
    }
  ]
}
```

---

### 6.7 Session Not Found

`threeDsSessionId` in-memory store'da bulunamazsa HTTP 200 ile failure döner.

```text
status = FAILED
responseCode = 12
responseMessage = Invalid transaction
```

Bu durum şu sebeplerle oluşabilir:

- Yanlış `threeDsSessionId`
- Uygulama restart, store temizlendi
- Session daha önce tamamlandı (double complete)

---

### 6.8 Identifier Mismatch

Complete request'indeki `merchantId`, `terminalId` veya `orderId`, session'dakiyle eşleşmezse:

```text
status = FAILED
responseCode = 12
responseMessage = Invalid transaction
```

---

## 7. Postconditions

### Başarılı 3DS + Authorization

```text
status = AUTHORIZED (capture=false) veya APPROVED (capture=true)
threeDsStatus = AUTHENTICATED veya ATTEMPTED
eci = 05 veya 06
```

`capture=false` ise session tamamlandıktan sonra authorization store'a kaydedilir (mevcut capture/void/refund akışı için).

`capture=true` ise SALE olarak kaydedilir (mevcut refund akışı için).

### Başarısız 3DS veya Authorization

```text
status = DECLINED veya FAILED
approved = false
```

Authorization store'a kayıt yapılmaz.

Gateway tarafında beklenen state geçişleri:

```text
AUTHORIZED → PaymentAttempt: AUTHORIZED, PaymentIntent: REQUIRES_CAPTURE
APPROVED   → PaymentAttempt: SUCCEEDED, PaymentIntent: SUCCEEDED
DECLINED   → PaymentAttempt: FAILED (declined)
FAILED     → PaymentAttempt: FAILED (system)
```

---

## 8. Business Rules

### BR-001: 3DS kartı tespiti authorize'da yapılır

`/authorize` isteğinde kart 3DS katalogunda bulunursa 3DS akışı başlatılır. Non-3DS katalogdaki kartlar mevcut akışa devam eder. Her iki katalogda da olmayan Luhn-valid kartlar non-3DS akışla onaylanır.

---

### BR-002: Tüm 3DS kartları PENDING_3DS ile başlar

Frictionless kartlar dahil tüm 3DS kartlar `/authorize`'da `PENDING_3DS` döner. Gateway her zaman `/3ds/complete` çağırır. Frictionless akışta kullanıcı yönlendirmesi yapılmaz ama complete adımı atlanmaz.

---

### BR-003: threeDsFlow gateway'i yönlendirir

`PENDING_3DS` response'unda dönen `threeDsFlow` değeri Gateway'in nasıl devam edeceğini belirtir:

| threeDsFlow | Gateway davranışı |
|---|---|
| `FRICTIONLESS` | Kullanıcıyı yönlendirme, doğrudan complete çağır |
| `CHALLENGE` | Kullanıcıyı `acsUrl`'e yönlendir, sonra complete çağır |
| `ATTEMPTED` | Kullanıcıyı yönlendirme, doğrudan complete çağır |
| `TIMEOUT` | Kullanıcıyı yönlendirme, doğrudan complete çağır (FAILED dönecek) |

---

### BR-004: Session TTL 15 dakikadır

3DS session `expiresAt = initiatedAt + 15 dakika` olarak set edilir. Mock implementasyonunda gerçek zamanlayıcı çalışmaz; timeout senaryosu `threeDsFlow=TIMEOUT` ile sabit olarak simüle edilir.

---

### BR-005: Complete tek seferliktir

Bir session sadece bir kez tamamlanabilir. İkinci complete çağrısı:

```text
responseCode = 12
responseMessage = Invalid transaction
```

---

### BR-006: 3DS auth başarısızsa authorization gönderilmez

`threeDsStatus=FAILED` veya `EXPIRED` durumlarında POS authorization isteği simüle edilmez. `posTransactionId`, `authCode`, `hostReferenceNumber` üretilmez.

---

### BR-007: ATTEMPTED authorization devam eder

`threeDsStatus=ATTEMPTED` (3DS unavailable) durumunda authorization devam eder ve `eci=06` ile onaylanır. Gateway tarafı ATTEMPTED sonucunu kendi risk politikasına göre değerlendirmelidir.

---

### BR-008: Authorization store entegrasyonu

3DS complete'te başarılı AUTHORIZED veya APPROVED sonucu oluştuğunda authorization store'a kayıt yapılır. Bu kayıt mevcut capture, void ve refund akışlarının aynı şekilde çalışmasını sağlar.

Store key: authorize isteğindeki `transactionId` (session'da saklı).

---

### BR-009: 3DS session kart verisi saklamaz

Session şunları saklamaz:

- PAN
- CVV
- expiryMonth / expiryYear
- card holder name

Session yalnızca non-sensitive authorization metadata'sı ve 3DS senaryo bilgisini saklar.

---

## 9. Status ve Enum Değerleri

### PosAuthorizeStatus (genişletilmiş)

```csharp
public enum PosAuthorizeStatus
{
    APPROVED,
    AUTHORIZED,
    PENDING_3DS,    // yeni
    DECLINED,
    FAILED
}
```

### ThreeDsStatus

```csharp
public enum ThreeDsStatus
{
    AUTHENTICATED,  // tam 3DS2 authentication, eci=05
    ATTEMPTED,      // 3DS unavailable fallback, eci=06
    FAILED,         // authentication başarısız
    EXPIRED         // session timeout
}
```

### ThreeDsFlow

```csharp
public enum ThreeDsFlow
{
    FRICTIONLESS,   // challenge ekranı gerekmez
    CHALLENGE,      // ACS challenge gerekir
    ATTEMPTED,      // 3DS unavailable, fallback
    TIMEOUT         // ACS hiç yanıt vermez
}
```

### ECI Değerleri

| eci | Anlam |
|---|---|
| `05` | Fully authenticated (AUTHENTICATED) |
| `06` | Attempted / unavailable (ATTEMPTED) |
| `null` | Failed veya expired |

---

## 10. Test Kart Kataloğu (3DS)

| PAN | Senaryo | threeDsFlow | threeDsStatus | Final Status |
|---|---|---|---|---|
| `4000000000003006` | Frictionless approved | `FRICTIONLESS` | `AUTHENTICATED` | `APPROVED` / `AUTHORIZED` |
| `4000000000003014` | Challenge approved | `CHALLENGE` | `AUTHENTICATED` | `APPROVED` / `AUTHORIZED` |
| `4000000000003022` | Challenge failed auth | `CHALLENGE` | `FAILED` | `DECLINED` |
| `4000000000003030` | 3DS unavailable / attempted | `ATTEMPTED` | `ATTEMPTED` | `APPROVED` / `AUTHORIZED` |
| `4000000000003048` | Challenge timeout | `TIMEOUT` | `EXPIRED` | `FAILED` |
| `4000000000003055` | Frictionless declined | `FRICTIONLESS` | `AUTHENTICATED` | `DECLINED` (responseCode=05) |

Tüm 3DS kartlar Luhn-valid olarak üretilmiştir.

---

## 11. Suggested Implementation Structure

```text
mockpos/
  application/
    AuthorizePaymentService       (modified: 3DS katalog kontrolü, session oluşturma)
    AuthorizePaymentResult        (modified: 3DS alanları nullable eklendi)
    Complete3DsCommand            (new)
    Complete3DsResult             (new)
    Complete3DsService            (new)
    IPos3DsSessionStore           (new)
    Pos3DsSession                 (new)

  domain/
    PosAuthorizeStatus            (modified: PENDING_3DS eklendi)
    ThreeDsStatus                 (new)
    ThreeDsFlow                   (new)
    ThreeDsCardCatalog            (new — 6 kartlık katalog)
    PosResponseCode               (modified: 3DS_AUTH_FAILED, 3DS_TIMEOUT eklendi)

  infrastructure/
    InMemoryPos3DsSessionStore    (new)

  web/
    Pos3DsCompleteController      (new — POST /api/v1/pos/3ds/complete)

    contracts/
      Complete3DsRequest          (new)
      Complete3DsResponse         (new)

  support/
    PosIdGenerator                (modified: Generate3DsSessionId eklendi)
```

---

## 12. Out of Scope

Bu use case için şimdilik yapılmayacaklar:

- 3DS1 (PAReq/PARes)
- Gerçek ACS entegrasyonu
- Gerçek zamanlayıcı ile session expiry
- 3DS method fingerprinting
- Partial authentication (eci=07 non-3DS)
- `strict3ds` policy (ATTEMPTED durumunda decline)
- Complete idempotency
- Capture / void / refund için bağımsız 3DS değişikliği
- Webhook
- Database persistence
- HMAC signature validation
