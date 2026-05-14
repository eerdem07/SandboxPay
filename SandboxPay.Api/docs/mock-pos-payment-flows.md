# Mock POS Payment Flows

Module: mock-pos
Bu doküman Mock POS'un tüm ödeme akışlarını ve bunların kombinasyonlarını açıklar.
Endpoint spesifikasyonları için `api/`, use case detayları için `use-cases/` altındaki dosyalara bakınız.

---

## 1. Endpoint Özeti

| Method | Path | Açıklama |
|---|---|---|
| `POST` | `/api/v1/pos/authorize` | Ödeme authorize veya sale başlatır |
| `POST` | `/api/v1/pos/3ds/complete` | Bekleyen 3DS oturumunu tamamlar |
| `POST` | `/api/v1/pos/capture` | Authorization-only işlemi capture eder |
| `POST` | `/api/v1/pos/void` | Authorization-only işlemi iptal eder |
| `POST` | `/api/v1/pos/refund` | Tamamlanmış ödemeyi iade eder |

---

## 2. Temel Kavramlar

### capture flag'i

`capture=true` → **SALE**: authorize ve capture tek işlemde tamamlanır. Sonradan capture çağrısı yapılmaz.

`capture=false` → **AUTHORIZATION_ONLY**: yalnızca provizyon alınır. Sonrasında capture veya void çağrısı beklenir.

### installmentCount

`installmentCount=1` → tek çekim. Taksit kataloğu kontrolü yapılmaz.

`installmentCount≥2` → taksitli işlem. Önce installment kataloğu, sonra standart kart kataloğu kontrol edilir. Response'ta `installmentAmount` döner.

### In-memory store

Yalnızca onaylanan işlemler store'a kaydedilir:

- SALE (`capture=true`, `status=APPROVED`): `Captured=true` olarak kaydedilir, refund için uygundur.
- AUTHORIZATION_ONLY (`capture=false`, `status=AUTHORIZED`): `Captured=false` olarak kaydedilir, capture veya void için uygundur.
- DECLINED ve FAILED işlemler store'a kaydedilmez.

Uygulama yeniden başlatıldığında tüm kayıtlar silinir.

---

## 3. Akış Ağacı

```
POST /api/v1/pos/authorize
│
├── 3DS kart mı?
│   └── Evet → status=PENDING_3DS
│               └── POST /api/v1/pos/3ds/complete
│                   ├── APPROVED  (capture=true)  → [SALE akışı]
│                   ├── AUTHORIZED (capture=false) → [AUTH akışı]
│                   ├── DECLINED                  → son
│                   └── FAILED                    → son
│
├── Taksit kartı mı? (installmentCount≥2)
│   └── Evet → status=DECLINED (62 Restricted card) → son
│
├── capture=true → status=APPROVED (SALE)
│   └── POST /api/v1/pos/refund → status=REFUNDED → son
│
├── capture=false → status=AUTHORIZED
│   ├── POST /api/v1/pos/capture → status=CAPTURED
│   │   └── POST /api/v1/pos/refund → status=REFUNDED → son
│   └── POST /api/v1/pos/void → status=VOIDED → son
│
├── DECLINED (kart bazlı red) → son
└── FAILED  (teknik hata)    → son
```

---

## 4. Akış Kombinasyonları

### 4.1 Tek Çekim Sale

```
installmentCount=1, capture=true
```

| Adım | Endpoint | Request | Response status |
|---|---|---|---|
| 1 | `POST /authorize` | `capture=true`, `installmentCount=1` | `APPROVED` |

`installmentAmount=null`. Store'a `Captured=true` olarak kaydedilir.

---

### 4.2 Taksitli Sale

```
installmentCount≥2, capture=true
```

| Adım | Endpoint | Request | Response status |
|---|---|---|---|
| 1 | `POST /authorize` | `capture=true`, `installmentCount=3` | `APPROVED` |

`installmentAmount` hesaplanarak döner. Store'a `Captured=true` olarak kaydedilir.

---

### 4.3 Authorization-Only → Capture

```
capture=false → AUTHORIZED → /capture → CAPTURED
```

| Adım | Endpoint | Request | Response status |
|---|---|---|---|
| 1 | `POST /authorize` | `capture=false` | `AUTHORIZED` |
| 2 | `POST /capture` | authorize response'undaki identifier'lar | `CAPTURED` |

Capture request'inde `originalTransactionId`, `originalPosTransactionId`, `authCode`, `hostReferenceNumber`, `amount`, `currency` zorunludur. Capture yalnızca tam tutar üzerinden yapılabilir.

---

### 4.4 Taksitli Authorization-Only → Capture

```
installmentCount≥2, capture=false → AUTHORIZED → /capture → CAPTURED
```

| Adım | Endpoint | Request | Response status |
|---|---|---|---|
| 1 | `POST /authorize` | `capture=false`, `installmentCount=6` | `AUTHORIZED` |
| 2 | `POST /capture` | authorize response'undaki identifier'lar | `CAPTURED` |

`installmentAmount` authorize response'unda döner; capture response'unda yer almaz.

---

### 4.5 Authorization-Only → Void

```
capture=false → AUTHORIZED → /void → VOIDED
```

| Adım | Endpoint | Request | Response status |
|---|---|---|---|
| 1 | `POST /authorize` | `capture=false` | `AUTHORIZED` |
| 2 | `POST /void` | authorize response'undaki identifier'lar | `VOIDED` |

Void yalnızca capture edilmemiş, void edilmemiş authorization'lara uygulanabilir.

---

### 4.6 Sale → Refund

```
capture=true → APPROVED → /refund → REFUNDED
```

| Adım | Endpoint | Request | Response status |
|---|---|---|---|
| 1 | `POST /authorize` | `capture=true` | `APPROVED` |
| 2 | `POST /refund` | authorize response'undaki identifier'lar | `REFUNDED` |

Refund request'inde `originalTransactionId`, `originalPosTransactionId`, `authCode`, `hostReferenceNumber` authorize response'undan alınır.

---

### 4.7 Authorization-Only → Capture → Refund

```
capture=false → AUTHORIZED → /capture → CAPTURED → /refund → REFUNDED
```

| Adım | Endpoint | Request | Response status |
|---|---|---|---|
| 1 | `POST /authorize` | `capture=false` | `AUTHORIZED` |
| 2 | `POST /capture` | authorize response'undaki identifier'lar | `CAPTURED` |
| 3 | `POST /refund` | **authorize** response'undaki identifier'lar | `REFUNDED` |

Refund request'indeki identifier'lar capture response'undan değil, **orijinal authorize response'undan** alınır.

---

### 4.8 3DS → Sale

```
3DS kart, capture=true → PENDING_3DS → /3ds/complete → APPROVED
```

| Adım | Endpoint | Request | Response status |
|---|---|---|---|
| 1 | `POST /authorize` | 3DS kart, `capture=true` | `PENDING_3DS` |
| 2 | `POST /3ds/complete` | `threeDsSessionId` | `APPROVED` |

`threeDsSessionId` adım 1 response'undan alınır. Adım 2 response'u standart authorize response ile aynı identifier'ları taşır.

---

### 4.9 3DS → Authorization-Only → Capture

```
3DS kart, capture=false → PENDING_3DS → /3ds/complete → AUTHORIZED → /capture → CAPTURED
```

| Adım | Endpoint | Request | Response status |
|---|---|---|---|
| 1 | `POST /authorize` | 3DS kart, `capture=false` | `PENDING_3DS` |
| 2 | `POST /3ds/complete` | `threeDsSessionId` | `AUTHORIZED` |
| 3 | `POST /capture` | 3DS complete response'undaki identifier'lar | `CAPTURED` |

---

### 4.10 Taksit Reddi

```
installmentCount≥2, MaxInstallmentCount aşıldı → DECLINED (62)
```

| Adım | Endpoint | Request | Response status |
|---|---|---|---|
| 1 | `POST /authorize` | taksit-kısıtlı kart, `installmentCount=6` | `DECLINED` |

`installmentAmount=null`. Store'a kaydedilmez.

Aynı kart `installmentCount=1` ile kullanıldığında normal akış işler.

---

## 5. Durum Geçiş Tablosu

Authorization store'daki bir kaydın alabileceği durumlar:

| Captured | Voided | Refunded | Geçerli sonraki işlem |
|---|---|---|---|
| `false` | `false` | `false` | capture veya void |
| `true` | `false` | `false` | refund |
| `false` | `true` | `false` | — (terminal) |
| `true` | `false` | `true` | — (terminal) |

SALE olarak oluşturulan kayıtlar `Captured=true` ile başlar; doğrudan refund için uygundur.

### Geçersiz işlemler

| Durum | Girişim | Sonuç |
|---|---|---|
| `Captured=true` | void | `FAILED` — `12 Invalid transaction` |
| `Voided=true` | capture | `FAILED` — `12 Invalid transaction` |
| `Voided=true` | void | `FAILED` — `12 Invalid transaction` |
| `Captured=false` | refund | `FAILED` — `12 Invalid transaction` |
| `Refunded=true` | refund | `FAILED` — `12 Invalid transaction` |
| Store'da yok | capture/void/refund | `FAILED` — `12 Invalid transaction` |

---

## 6. Test Kartı Hızlı Referansı

### Standart Kartlar

| PAN | Response Code | Status | Senaryo |
|---|---|---|---|
| `4111111111111111` | `00` | APPROVED/AUTHORIZED | Onaylı |
| `4000000000000002` | `05` | DECLINED | Do not honor |
| `4000000000000012` | `12` | FAILED | Invalid transaction |
| `4000000000000013` | `13` | FAILED | Invalid amount |
| `4000000000000014` | `14` | DECLINED | Invalid card number |
| `4000000000000030` | `30` | FAILED | Format error |
| `4000000000000041` | `41` | DECLINED | Lost card |
| `4000000000000043` | `43` | DECLINED | Stolen card |
| `4000000000000051` | `51` | DECLINED | Insufficient funds |
| `4000000000000054` | `54` | DECLINED | Expired card |
| `4000000000000057` | `57` | DECLINED | Transaction not permitted to cardholder |
| `4000000000000058` | `58` | FAILED | Transaction not permitted to terminal |
| `4000000000000061` | `61` | DECLINED | Exceeds amount limit |
| `4000000000000065` | `65` | DECLINED | Exceeds frequency limit |
| `4000000000000091` | `91` | FAILED | Issuer unavailable |
| `4000000000000096` | `96` | FAILED | System malfunction |
| `4000000000009995` | `TIMEOUT` | FAILED | Bank POS timeout |

Katalogda olmayan Luhn-geçerli PAN → `00` onaylı. Luhn-geçersiz → `14` declined.

### Taksit Kartları

| PAN | MaxInstallmentCount | Senaryo |
|---|---|---|
| `4000000000006000` | `0` | Debit kart — taksit desteklenmiyor (`62`) |
| `4000000000006003` | `3` | Max 3 taksit — fazlası reddedilir (`62`) |

Her iki kart da `installmentCount=1` ile kullanıldığında standart akışa girer.

### 3DS Kartları

| PAN | Flow | threeDsStatus | Final Status |
|---|---|---|---|
| `4000000000003006` | `FRICTIONLESS` | `AUTHENTICATED` | APPROVED / AUTHORIZED |
| `4000000000003014` | `CHALLENGE` | `AUTHENTICATED` | APPROVED / AUTHORIZED |
| `4000000000003022` | `CHALLENGE` | `FAILED` | DECLINED (`3DS_AUTH_FAILED`) |
| `4000000000003030` | `ATTEMPTED` | `ATTEMPTED` | APPROVED / AUTHORIZED (eci=06) |
| `4000000000003048` | `TIMEOUT` | `EXPIRED` | FAILED (`3DS_TIMEOUT`) |
| `4000000000003055` | `FRICTIONLESS` | `AUTHENTICATED` | DECLINED (`05`) |

APPROVED / AUTHORIZED ayrımı orijinal authorize request'indeki `capture` flag'ine göre belirlenir.

---

## 7. Identifier Referansı

Downstream işlemlerin (capture, void, refund) hangi değerleri authorize response'undan alması gerektiği:

| Alan | Kaynak |
|---|---|
| `originalTransactionId` | authorize → `transactionId` |
| `originalPosTransactionId` | authorize → `posTransactionId` |
| `authCode` | authorize → `authCode` |
| `hostReferenceNumber` | authorize → `hostReferenceNumber` |
| `amount` | authorize → `amount` |
| `currency` | authorize → `currency` |

3DS akışında bu değerler `/3ds/complete` response'undan alınır.

Refund, captured authorization-only için de orijinal **authorize** response'undaki identifier'ları kullanır; capture response'undakini değil.

---

## 8. Kısıtlamalar

| Konu | Durum |
|---|---|
| Sale void | Desteklenmiyor — satış için void yoktur |
| Partial capture | Desteklenmiyor — yalnızca tam tutar |
| Çoklu capture | Desteklenmiyor |
| Partial refund | Desteklenmiyor — yalnızca tam tutar |
| Çoklu refund | Desteklenmiyor |
| Void idempotency | Desteklenmiyor |
| Refund idempotency | Desteklenmiyor |
| Taksit başına iade | Desteklenmiyor |
| 3DS + taksit özel senaryosu | Yok — 3DS akışı `installmentCount`'u taşır, ayrı senaryo gerekmez |
| Database persistence | Yok — uygulama restart'ında tüm kayıtlar silinir |
