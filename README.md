# PillFrenzy

Bir mimari demosu olarak yazılmış küçük bir mobil oyun. Amaç yayınlamak değil — üstüne
prodüksiyon dertleri bindikçe temiz kalabilen bir runtime: Addressables, pooling, save, IAP,
analytics, sahne akışı ve oynanışı bilmeyen bir UI katmanı.

Unity 6 (6000.3.21f1)

---

## 30 saniyede oyun

Konveyörde renkli kapsüller ilerler, dokununca eşleşen hedef kutuya uçar, kutular dolunca level
biter. Altın ve kombo puan verir, zehir can götürür. Bant hızlanır. Bilerek çıkılan levellar ve üst üste alınan zehirler kalp götürür ve kalpler oturumu sınırlar, özel güçler son levellardaki karmaşada nefes aldırır. Döngü bilerek basit — mimariye ana odakta.

## Akış

BootInit servisleri ve LevelManifest'i kurar, GameRunner tek tick sürücüsüdür.
Menü / Play sahne başına bir composition root (BootMenu, BootGameplay) sistemleri bağlar ve
Shutdown ile geri toplar. Oynanış düz C# sınıflarıdır; UI yalnızca EB.Presentation dinler.

Bağımlılıklar tek yönlü:

```text
PillFrenzy.Core        → (proje içi yok)
PillFrenzy.Gameplay    → Core
PillFrenzy.UI          → Core, Gameplay
PillFrenzy.Bootstrap   → Core, Gameplay, UI
PillFrenzy.Editor      → hepsi  [Editor only]
```

## İçerik

Level sırası LevelManifestSO'da. Definition + layout AssetReference'i mevcut.
Yeni level seçili definition'da Create Next Level ile kolaylaştırıldı.

---

## Aldığım kararlar ve gerekçeleri

### Assembly definition, sadece namespace değil

Katman ihlali review yorumu değil derleme hatası. Composition root'ları Bootstrap'te tutmak
Core/Gameplay'in sahneye geri uzanmasını engeller.

### Service locator, DI konteyneri değil

Dokuz servis ve sabit boot sırası, Zenject/VContainer overkill olurdu. Ve service locator DI kullanmamış bir developer için alışması daha kolay. ServiceProvider arayüz kaydı, init ve tick kaydını tek yerden yapıyor.

### Tek tick döngüsü, MB'siz oynanış

Tek MonoBehaviour ve GameRunner. Sistemler ITickable/ILateTickable kullanıyor. Skor, kalp, path
matematiği sahnesiz test edilebilir. Bir tickable'ın exception'ı diğerlerini öldürmez.

### Üç kanallı event bus

EB.Gameplay, EB.Presentation, EB.Analytics HUD ya da TMP_Text bilmez. CapsuleSystem
LevelSystem'e değil ILevelRunState + event'lere bağlı.

### Sahne başına composition root

Kurulum tek dosyada, yukarıdan aşağı. BootGameplay beş sistemi kurar, bağlar, tick'e sokar,
iptal / pool / addressable release ile kapatır.

### İzole level test sahnesi

Test.unity + BootLevelTest Build Settings'te yok. Kendi mini boot'unu yapar (servisler,
manifest, UI), inspector'dan level numarası alır, menü / save ilerlemesine dokunmadan aynı
oynanış zincirini çalıştırır. Level ve feedback iterasyonu için Init → Menu → Play turunu
beklememek bilinçli bir ergonomi kararı; prod boot yolunu kirletmez.

### ScriptableObject her yerde

Level manifest, definitonlar, kapsül, renk, hedef, güç, ses, IAP, UI panelleri, global ve feel
ayarları. Tasarımcı geri bildirimini OnValidate/Inspector'dan alır, feel sayıları
FeedbackSettingsSO'da — build istemez.

### UniTask + yayılan iptal

Coroutine yok. GameContext uygulama token'ı, boot sahne token'ı. Tween ortasında run biterse
await sonrası faz yeniden kontrol edilir; aksi halde havuzlanmış kapsül iki sahibe kalır.

### Addressables ve havuz arayüz arkasında

IAssetProvider/IGameObjectPool. Handle takibi, warmup (MaxActive + 2), LoadAsset
fırlatmaz — eksik adres boot'u değil özelliği bozar.

---

## Entegrasyonlar

| | | |
| --- | --- | --- |
| Asset | IAssetProvider | Addressables, cache, null-güvenli |
| Pool | IGameObjectPool | Adres anahtarlı yığın, warmup |
| Scene | ISceneService | Enum + ISceneLoadingUi |
| Input | IInputService | Yeni Input System, tek mandallı tap |
| Sound | IAudioService | Katalog, ayrı SFX/müzik |
| Save | ISaveService | Dirty-flag flush, atomik yazma, versiyon |
| IAP | IIAPService | Katalog → ödül |
| Analytics | IAnalyticsSystem | Bus → N sağlayıcı |

Save bilerek en ağır parça. Yazmalar her değişiklikte diske gitmez dirty flag ile
toplanır ve LateTick, uygulama duraklatma, focus kaybı veya çıkışta flush edilir.
Dosya önce geçici bir yola yazılır, sonra File.Replace ile atomik olarak takas edilir —
yazma ortasında uygulama ölse bile kayıt bozulmaz. Okuma başarısız olursa önce .bak
kopyasına o da yoksa varsayılanlara düşülür. SaveData.Version alanını şema değişince
migrate edebilmek için ekledim.

---

## Bilerek eksik bırakılanlar

Gerçek IAP. Ödül şu an OnPurchasePending içinde, onaydan önce veriliyor. Yayın için
ödülü confirm'a taşımak, makbuz doğrulaması, sunucu tarafı check, idempotency defteri ve
Restore Purchases gerekir.

Analytics. Sağlayıcı arayüzü ve bus hazır AnalyticsLog yalnızca Debug.Log yazıyor.
Firebase eklemek BootInit içinde bir Register çağrısı.

Mağaza fiyatları. Katalogda elle yazılmış fiyat metni var. Gerçek build mağazadan
localizedPriceString okumalı — hem bölge hem politika için.

Yerelleştirme. Oyuncu metinleri view sınıflarında sabit İngilizce. Çözüm Unity
Localization ya da custom localization. UI oturmadan yapmak boşa emek olurdu.

Test ve CI. Assembly ayrımı EditMode testlerini kolaylaştırıyor henüz ne test
assembly'si ne workflow dosyası var.

Reklam. IAdService yok. Casual F2P'de rewarded video IAP kadar kritik. aynı ödül
yoluyla (ISaveService) oturur.

Sunucu otoriteli zaman. Kalp yenilenmesi ve ölümsüzlük DateTimeOffset.UtcNow
okuyor cihaz saatini ileri almak ikisini de bedavaya verir. Gerçek çözüm sunucu saati.

---

## Vaktim olsa sırada ne var

Level layout design tool. Path noktaları, kamera ve spawn / exit anchor'ları bugün prefab
üzerinde elle kuruluyor. İstediğim şey bir Editor penceresi waypoint çizmek, LevelLayout
doğrulamak, prefab ile Addressable üretmek ve tanımı manifesta bağlamak. Level designer
Scene view'da kaybolmadan arena kurabilmeli.

LevelSystem parçalamak. Faz, spawn temposu, hız artışı, skor ve can hala aynı sınıfta.
Skorlama ve spawn yönetimi aynı olayları dinleyen ayrı sistemler olmalı.

MMFeedbacks. GameplayFeedback, Feel'in elle yazılmış bir alt kümesi. MMF_Player'a
geçmek ses, shake, VFX ve haptik'i tek bir inspector asset'inde toplar; sınıfı da siler.

Cihazda profil. Pooling, warmup ve maskeli raycast varsayımları kod alışkanlığı ve çözümlemesiyle geldi.
Bu repodaki hiçbir performans sayısı henüz gerçek donanımda profiler'dan çıkmadı.

---

## Proje yapısı

```text
Assets/_Main/
├── Source/
│   ├── Core/        Servisler, EB, game loop, save, IAP, ILevelCatalog
│   ├── Gameplay/    Level, kapsül, hedef, spawn, güç, path, feel
│   ├── UI/          Panel yöneticisi, canvas / HUD
│   ├── Bootstrap/   BootInit, BootMenu, BootGameplay, BootLevelTest
│   └── Editor/      Prefab tool'ları, Create Next Level, Addressables ensure
├── SO/              ScriptableObject'ler (LevelManifest, tanımlar, kataloglar)
├── Prefab/          Gameplay, level layout, target, UI, VFX
└── Scene/           Init, Menu, Gameplay, Test (build dışı)
```

## Çalıştırma

Projeyi Unity 6000.3.21f1 ile açıp Init sahnesinden Play'e basmanız yeterli. Diğer sahneler
BootInit'in kurduğu servis konteynerini bekler doğrudan girilirse hata loglarlar.

Level ve feel iterasyonu için Test sahnesini kullanabilirsiniz. Build Settings'te değildir
inspector'dan level numarası seçilir, menü ve save ilerlemesine dokunulmaz.

Menü çubuğundan PillFrenzy/Scene Loader sahneler arası geçiş, PillFrenzy/Reset Save
ilerlemeyi sıfırlama içindir.

---

## Kapsam

Bu repo bir portföy işi. On beş level ve iki layout ile oynanacak kadar içerik var; asıl
odak oyun değil, runtime. Servislerin nasıl ayağa kalktığı, oynanışın UI'dan nasıl
ayrıldığı, yeni bir şey eklerken nereye dokunmam gerektiği bunları göstermek için
yazdım. IAP'yi gerçekten yayınlamaya hazır bırakmadım bilerek öyle. Neyi eksik
bıraktığımı da yazdım çünkü o da mesele.
