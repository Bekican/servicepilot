# ADR 0025: Prioritize Explicit and Recoverable User Workflows

- Status: Accepted
- Date: 2026-08-11

## Context

ServicePilot'ın altyapı, güvenlik, tenant isolation, observability ve deployment
temelleri tamamlandı. Ancak mevcut kullanıcı akışlarının bazıları teknik olarak
çalışsa da kullanıcıya yeterli açıklama ve düzeltme imkânı vermiyor.

Form hataları çoğunlukla formun üstünde genel bir mesaj olarak gösteriliyor.
Bazı sunucu hata kodlarının Türkçe karşılığı bulunmuyor. Bireysel ve kurumsal
müşteri alanları aynı anda gösteriliyor. Pasifleştirilen müşteri ve adresler
yeniden aktifleştirilemiyor. E-postası olmayan müşteriler için oluşturulan
hatırlatmaların neden gönderilmediği yeterince açık değil.

Randevu ve müşteri listeleri bütün kayıtları uygulamaya yükleyerek filtreliyor.
Bu davranış veri büyüdükçe performans ve kullanılabilirlik sorunu oluşturur.
Ayrıca kullanıcıların kendi parolalarını güvenli biçimde sıfırlayabileceği bir
hesap kurtarma akışı bulunmuyor.

## Decision

ServicePilot, ilk canlı sürümden önce kullanıcı akışlarını açık, düzeltilebilir
ve veri büyüklüğünden bağımsız hâle getirecektir.

### Validation and Problem Details

API, doğrulama hatalarını RFC 7807 uyumlu Problem Details cevabı içinde güvenli
alan hata kodlarıyla döndürecektir.

Alan isimleri API sözleşmesindeki lower camel case isimlerle aynı olacaktır.
Örnekler:

- `organizationSlug`
- `firstName`
- `email`
- `phone`
- `startAt`
- `defaultDurationMinutes`

Hata değerleri kullanıcı girdisi veya teknik exception mesajı içermeyen sabit
kodlar olacaktır. Next.js BFF bu kodları Türkçe kullanıcı mesajlarına
çevirecek ve hatayı ilgili alanın altında gösterecektir.

Formun üstündeki genel hata alanı; alanla ilişkilendirilemeyen conflict,
authorization, availability ve beklenmeyen sistem hataları için korunacaktır.
Correlation ID yalnız destek kodu olarak gösterilmeye devam edecektir.

Frontend doğrulaması kullanıcıya erken geri bildirim sağlar ancak güvenlik
sınırı değildir. Aynı iş kuralları API ve domain/application katmanında
uygulanmaya devam edecektir.

### Form Behavior

Müşteri türü seçimine göre yalnız ilgili alanlar gösterilecektir:

- Bireysel müşteri için ad ve soyad
- Kurumsal müşteri için şirket adı ve opsiyonel iletişim kişisi

E-posta ve telefon alanlarının opsiyonel olduğu açıkça belirtilecektir.
Türkiye telefon numaraları `0555...` veya `+90555...` biçiminde kabul edilecek,
ancak veritabanında E.164 `+90...` biçiminde saklanacaktır.

Randevu zamanı organizasyonun saat dilimine göre doğrulanacaktır.
`datetime-local` minimum değeri UTC metniyle değil kullanıcının yerel zamanı
ile üretilecektir. Hata sonrasında müşteri, hizmet, teknisyen ve zaman seçimleri
korunacaktır.

E-postası olmayan müşteri için randevu oluşturulmasına izin verilecektir.
Randevu formu ve detay ekranı bu müşteriye e-posta hatırlatması
gönderilmeyeceğini açıkça gösterecektir.

### Recoverable Status Changes

Pasifleştirme bir silme işlemi değildir. Müşteri ve adres kayıtları yeniden
aktifleştirilebilir olacaktır.

Geçmiş randevu, audit ve ilişki kayıtları korunacaktır. Yeniden aktifleştirilen
adres otomatik olarak birincil adres olmayacaktır. Kullanıcı gerekirse adresi
ayrı bir işlemle birincil yapacaktır.

### Operational Lists

Müşteri, randevu ve hatırlatma listeleri sunucu tarafında aranacak, filtrelenecek
ve sayfalanacaktır.

Varsayılan sayfa numarası `1`, varsayılan sayfa boyutu `20`, izin verilen en
yüksek sayfa boyutu `100` olacaktır. API toplam kayıt sayısını ve toplam sayfa
sayısını içeren bir sayfalı cevap döndürecektir.

Randevular ekranı varsayılan olarak organizasyonun yerel gün başlangıcından
itibaren bugün ve gelecekteki yaklaşan randevuları gösterecektir. Geçmiş
randevular tarih filtresiyle erişilebilir olmaya devam edecektir.

Dashboard bütün randevu geçmişini yüklemeyecek; yalnız organizasyonun bugünkü
UTC zaman aralığını API'ye gönderecektir.

### Turkish First Release

İlk canlı sürümün arayüzü, hata mesajları ve sistem e-postaları Türkçe
olacaktır. Teknik enum ve API değerleri İngilizce kalabilir ancak kullanıcıya
Türkçe karşılıkları gösterilecektir.

Çeviri metinleri merkezi tutulacak, fakat ilk sürümde organizasyon dili ayarı
ve İngilizce arayüz eklenmeyecektir.

Davet, parola sıfırlama ve randevu hatırlatma e-postaları Türkçe olacaktır.
Tarih ve saatler organizasyonun saat diliminde gösterilecek; UTC metni
kullanıcıya gönderilmeyecektir.

### Password Recovery

Kullanıcı organizasyon kısa adı ve e-posta adresiyle parola sıfırlama talebi
oluşturabilecektir.

Talep endpoint'i kullanıcı veya organizasyonun varlığını açıklamamak için her
durumda aynı başarılı cevabı döndürecektir. Token:

- Kriptografik olarak rastgele üretilecek
- Veritabanında yalnız SHA-256 hash olarak saklanacak
- Tek kullanımlık olacak
- 30 dakika sonra geçersiz olacak
- Yeni talep oluşturulduğunda önceki kullanılmamış token'lar geçersiz olacak

Parola başarıyla değiştirildiğinde kullanıcının session version değeri
artırılacak ve daha önce oluşturulmuş JWT oturumları geçersiz olacaktır.

E-posta doğrulama, refresh token, randevu yeniden planlama ve no-show durumu bu
kararın kapsamı dışındadır.

## Consequences

- Kullanıcılar hatanın hangi alanda olduğunu ve nasıl düzelteceklerini görebilir.
- Yanlışlıkla pasifleştirilen kayıtlar veri kaybı olmadan geri açılabilir.
- E-postasız müşterilerin reminder davranışı sürpriz oluşturmaz.
- Liste performansı kayıt sayısıyla kontrolsüz biçimde büyümez.
- Parolasını unutan kullanıcı yönetici müdahalesi olmadan hesabını kurtarabilir.
- Validation, pagination ve password reset için API sözleşmeleri genişler.
- Password reset token tablosu ve kullanıcı session version alanı için migration
  gerekir.
- Türkçe metinlerin merkezi tutulması ek bakım gerektirir ancak gelecekteki
  İngilizce desteğini kolaylaştırır.

## Verification

Otomatik testler en az şu davranışları kanıtlayacaktır:

- Alan hata kodları doğru form alanının altında Türkçe gösterilir.
- Genel sistem hataları formun üstünde ve destek koduyla gösterilir.
- Müşteri türü değişince yalnız ilgili alanlar gönderilir.
- Yerel Türkiye telefon numarası E.164 biçimine dönüştürülür.
- E-postasız müşteri randevusu oluşur ve reminder uyarısı gösterilir.
- Müşteri ve adres pasifleştirilip yeniden aktifleştirilebilir.
- Randevu minimum zamanı organizasyon saat dilimine göre hesaplanır.
- Liste sorguları tenant sınırını koruyarak sayfalanır.
- Varsayılan randevu listesi bugün ve gelecekteki kayıtları gösterir.
- Parola sıfırlama talebi hesap varlığını dışarı sızdırmaz.
- Süresi geçmiş veya kullanılmış reset token kabul edilmez.
- Parola değişiminden önceki JWT oturumları reddedilir.
- Davet, reminder ve parola sıfırlama e-postaları Türkçe ve doğru saat
  dilimindedir.

## Reconsideration Triggers

Bu karar şu durumlarda yeniden değerlendirilecektir:

- İngilizce veya kullanıcı bazlı dil tercihi gerektiğinde
- Refresh token veya merkezi session store eklendiğinde
- Randevu yeniden planlama ve no-show akışları tasarlandığında
- Listeleme ihtiyaçları cursor pagination gerektirecek ölçeğe ulaştığında