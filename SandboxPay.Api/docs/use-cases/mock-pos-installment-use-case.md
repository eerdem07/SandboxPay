# Mock POS Installment Use Case

Status: Draft
Module: mock-pos
Related API contract: [`../api/mock-pos-installment-api-contract.md`](../api/mock-pos-installment-api-contract.md)
Related authorize use case: `mock-pos-authorize-use-case.md`

---

## 1. Purpose

Bu use case, `AuthorizePayment` use case'inin taksitli ödeme davranışını tanımlar.

`installmentCount = 1` tek çekimdir. `installmentCount >= 2` taksitli işlemdir.

Taksitli işlem ayrı bir endpoint değildir. `POST /api/v1/pos/authorize` request'indeki `installmentCount` değerine göre devreye girer.

Bu use case şunları kapsar:

- `installmentAmount` hesaplaması ve response'ta dönmesi
- Taksiti desteklemeyen kart senaryoları (`62 Restricted card`)
- Kart bazlı maksimum taksit sayısı kontrolü
- Request seviyesinde maksimum taksit sayısı validasyonu (max 12)

---

## 2. Actor

Primary actor:

- Mira Payment Gateway Backend

---

## 3. Preconditions

`mock-pos-authorize-use-case.md` precondition'larına ek olarak:

- `installmentCount` `1` ile `12` arasında olmalıdır.

---

## 4. Main Flow — Taksitli İşlem

`installmentCount >= 2` olduğunda:

1. Gateway, Mock POS'a authorize isteği gönderir.

2. Mock POS request body'yi validate eder.

   `installmentCount > 12` ise:

   ```text
   HTTP 400
   responseCode = 30
   responseMessage = Format error
   ```

3. PAN normalize edilir.

4. Normalize PAN, taksit test kartı katalogunda aranır.

5. Kart `MaxInstallmentCount = 0` ise:

   ```text
   responseCode = 62
   status = DECLINED
   responseMessage = Restricted card
   ```

6. Kart `MaxInstallmentCount = N` ve `installmentCount > N` ise:

   ```text
   responseCode = 62
   status = DECLINED
   responseMessage = Restricted card
   ```

7. Taksit katalogunda eşleşme yoksa standart kart çözümleme akışı devam eder (`mock-pos-authorize-use-case.md` Main Flow, adım 4).

8. Onaylanan işlemde `installmentAmount` hesaplanır:

   ```text
   installmentAmount = amount / installmentCount
   ```

   İki ondalık basamağa yuvarlanır.

9. Mock POS response üretir. `installmentAmount` dahil edilir.

---

## 5. Main Flow — Tek Çekim

`installmentCount = 1` olduğunda:

1. Taksit katalog kontrolü atlanır.
2. Standart kart çözümleme akışı çalışır.
3. Response'ta `installmentAmount = null` döner.

---

## 6. Alternative Flows

### 6.1 Taksit Desteklemeyen Kart

Test kartının `MaxInstallmentCount = 0` olduğu durum:

```text
responseCode = 62
status = DECLINED
approved = false
```

Geçerli kart örnekleri: `4000000000006000` (debit kart simülasyonu)

`installmentCount = 1` (tek çekim) ile aynı kart kullanıldığında:

```text
Taksit kataloğu kontrolü atlanır.
Standart kart çözümleme akışı çalışır.
Debit kart simülasyonu yalnızca taksitli işlemleri reddeder.
```

### 6.2 Kart Maksimum Taksit Sayısını Aşma

Test kartının `MaxInstallmentCount = N` ve `installmentCount > N` olduğu durum:

```text
responseCode = 62
status = DECLINED
approved = false
```

Örnek: `MaxInstallmentCount = 3` olan kartla `installmentCount = 6` gönderilirse reddedilir.

### 6.3 Request Validasyon Hatası (Max 12)

`installmentCount > 12` olduğu durum:

```text
HTTP 400
responseCode = 30
status = FAILED
```

---

## 7. Postconditions

Başarılı taksitli işlemde (`installmentCount >= 2`):

```text
installmentAmount döner (hesaplanmış değer)
```

Tek çekimde (`installmentCount = 1`):

```text
installmentAmount = null
```

Taksit reddi durumunda:

```text
status = DECLINED
responseCode = 62
installmentAmount = null
approved = false
```

Capture, void ve refund response'larında `installmentAmount` yer almaz.

---

## 8. Business Rules

### BR-001: installmentCount = 1 tek çekimdir

```text
installmentCount = 1  → tek çekim, taksit kataloğu kontrolü yapılmaz
installmentCount >= 2 → taksitli işlem, taksit kataloğu kontrolü yapılır
```

---

### BR-002: installmentAmount yalnızca taksitli işlemlerde dönmez

```text
installmentCount = 1  → installmentAmount = null
installmentCount >= 2 → installmentAmount = amount / installmentCount
```

---

### BR-003: Request seviyesinde maksimum 12 taksit

```text
installmentCount > 12 → 400 Bad Request, responseCode = 30
```

Kart bazlı limitler bu validasyondan bağımsız olarak taksit katalogunda tutulur.

---

### BR-004: Taksit kataloğu standart kart kataloguna önceliklidir

Bir PAN hem standart hem taksit katalogunda varsa taksit kataloğu önce kontrol edilir.

```text
1. Taksit kataloğunda ara
2. installmentCount >= 2 ve MaxInstallmentCount aşılmışsa → 62
3. Eşleşme yoksa standart kart çözümleme akışına geç
```

---

### BR-005: installmentAmount iki ondalık basamağa yuvarlanır

```text
amount = "1250.50", installmentCount = 3
installmentAmount = 1250.50 / 3 = 416.83 (standart yuvarlama)
```

---

### BR-006: installmentCount authorization store'a kaydedilir

`capture=false` başarılı işlemde authorization store'a `installmentCount` eklenir. Capture/void/refund bu değeri okuyabilir; ancak kendi response'larında döndürmez.

---

### BR-007: Debit kart taksit reddi yalnızca taksitli işlem için geçerlidir

`MaxInstallmentCount = 0` olan kart, `installmentCount = 1` ile kullanıldığında normal akışa girer. Red yalnızca `installmentCount >= 2` durumunda uygulanır.

---

## 9. Test Card Catalog (Installment)

| PAN | MaxInstallmentCount | Senaryo |
|---|---|---|
| `4000000000006000` | `0` | Debit kart — taksit desteklenmiyor, tek çekim onaylanır |
| `4000000000006003` | `3` | Max 3 taksit — limit içinde onaylanır, fazlası reddedilir |

`MaxInstallmentCount = null` → limit yok, standart akış geçerlidir (taksit katalogunda bu kart yer almaz).

---

## 10. Out of Scope

- Kısmi taksit (partial installment capture)
- Taksit başına iade (per-installment refund)
- Taksit erken kapatma
- Taksit faizi / komisyon hesaplama
- Taksit tipi (interestFree, withInterest vb.)
- Taksit sayısına göre farklı yanıt süresi simülasyonu
- Çoklu taksit planı seçeneği
- 3DS ile taksit kombinasyonu için özel senaryo (mevcut 3DS akışı `installmentCount`'u taşır, ayrı senaryo gerekmez)
- Database persistence

---

## 11. Suggested Implementation Notes

Taksit kataloğu ayrı bir domain nesnesi olarak tutulur:

```text
domain/
  InstallmentCardCatalog   (PAN → MaxInstallmentCount mapping)
```

`TestCardCatalog` ayrı kalır — taksit ve standart katalog birbirinden bağımsızdır.

Çözümleme sırası `AuthorizePaymentService`'te şöyle olur:

```text
1. installmentCount > 12 → validation error
2. installmentCount >= 2 → InstallmentCardCatalog'a bak → 62 kontrolü
3. TestCardCatalog'a bak → mapped response code
4. Luhn validation
5. installmentAmount hesapla (installmentCount >= 2 ise)
```
