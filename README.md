# PillFrenzy

Mimari demosu olarak yazdığım küçük bir mobil oyun. Amacım yayınlamak değil, üstüne prodüksiyon
dertleri (Addressables, pooling, save, IAP, analytics, tutorial, sahne akışı) bindikçe temiz
kalabilen bir runtime göstermek. Bu doküman, koda bakmadan mimarinin **ne**, **nasıl** ve
**neden** yaptığını anlatmak için yazıldı.

Unity 6 (6000.3.21f1) · UniTask · DOTween · Addressables · Input System · Unity Splines · Unity IAP 5

---

## İçindekiler

1. [Oyun, 30 saniyede](#oyun-30-saniyede)
2. [Çalıştırma](#çalıştırma)
3. [Büyük resim](#büyük-resim)
4. [Uygulama yaşam döngüsü](#uygulama-yaşam-döngüsü)
5. [Çekirdek altyapı](#çekirdek-altyapı)
6. [Oynanış sistemleri](#oynanış-sistemleri)
7. [Tutorial sistemi](#tutorial-sistemi)
8. [UI katmanı](#ui-katmanı)
9. [Servisler](#servisler)
10. [İçerik hattı ve editör araçları](#içerik-hattı-ve-editör-araçları)
11. [Performans notları](#performans-notları)
12. [Kararlar ve gerekçeleri](#kararlar-ve-gerekçeleri)
13. [Yeni bir şey eklerken](#yeni-bir-şey-eklerken)
14. [Bilerek eksik bırakılanlar](#bilerek-eksik-bırakılanlar)
15. [Proje yapısı](#proje-yapısı)

---

## Oyun, 30 saniyede

Bir ya da birden fazla konveyörde renkli kapsüller ilerler. Dokunulan kapsül, rengine uyan hedef
kutuya uçar. Kutular bir kuyruktan sırayla gelir, sahnede aynı anda en fazla üç kutu durur, dolan
kutu çıkar, diğerleri kayar, sıradaki girer. Kuyruk bitince level biter.

- **Altın** kapsül kombo çarpanıyla puan verir, **zehir** kombo'yu sıfırlar ve can götürür.
- Bant zamanla hızlanır, spawn aralığı hızla birlikte kısalır.
- Canı biten run kaybedilir ve bir **kalp** harcanır. Run ortasında menüye dönmek de kalp yakar.
  Kalpler 30 dakikada bir dolar, sıfırken oynanamaz.
- 5. levelda açılan **özel güç** (bandı yavaşlatma) son levellardaki karmaşada nefes aldırır.

Döngü bilerek basit. Odak mimaride. İçerik: 16 level, 7 layout, 3 tutorial, 3 IAP ürünü.

Zorluk levellar boyunca tek yönde artar. Hedef kuyruğundaki toplam kapsül her levelda 2 artar
(8'den 38'e), bant temposu da her levelda biraz hızlanır. Layout'lar da buna eşlik eder: ilk
levellarda tek bant (düz, yükseltili, eğimli, uzun yılan), ortada iki bant (ters yönlü paralel
şeritler, ortada birleşen kavisli bantlar), son levellarda üç şerit. İki ve üç bantlı layout'larda
her bantın kendi renk ağırlığı vardır, böylece hangi rengin nereden geleceği bant bant değişir.

---

## Çalıştırma

- Projeyi Unity 6000.3.21f1 ile açıp **Init** sahnesinden Play'e basın. Menu ve Gameplay
  sahneleri Init'in kurduğu servisleri bekler. Doğrudan açılırlarsa hata loglayıp durur.
- İlk açılışta menü atlanır, oyuncu doğrudan level 1'e düşer. İlk level bitince açılış menüden olur.
- **Test** sahnesi (Build Settings dışında) level ve feel iterasyonu içindir. Kendi mini boot'unu
  yapar, inspector'dan level numarası alır, kalpleri otomatik doldurur ve kazan/kaybet sonrası
  aynı sahnede bir sonraki ya da aynı levelı yeniden başlatır.
- Menü çubuğunda: `PillFrenzy/Level Editor`, `PillFrenzy/Scene Loader`, `PillFrenzy/Reset Save`.

---

## Büyük resim

### Katmanlar

Her katman bir assembly definition. Bağımlılıklar tek yönlü ve derleyici tarafından korunur:

```text
PillFrenzy.Core        → (proje içi referans yok)     servisler, event bus, game loop, save, IAP
PillFrenzy.Gameplay    → Core                          level, kapsül, hedef, spawn, güç, tutorial mantığı
PillFrenzy.UI          → Core, Gameplay                panel yöneticisi, canvas'lar, presenter'lar
PillFrenzy.Bootstrap   → Core, Gameplay, UI            composition root'lar
PillFrenzy.Editor      → hepsi  [yalnızca Editor]      level editörü, inspector'lar, araçlar
```

Alt katmanın üst katmana ihtiyaç duyduğu yerlerde bağımlılık **arayüzle ters çevrilir**. Arayüz alt
katmanda tanımlanır, implementasyon üst katmanda yazılır, ikisini Bootstrap bağlar:

| Arayüz | Tanımlandığı yer | Implementasyon | Neden |
| --- | --- | --- | --- |
| `ILevelCatalog` | Core | `LevelManifestSO` (Gameplay) | Core'daki `GameContext` level listesini tutar ama level tiplerini bilemez |
| `ISceneLoadingUi` | Core | `SceneLoadingUi` (UI) | Sahne servisi yükleme ekranını gösterir ama UI'ı bilemez |
| `ITutorialPresenter` | Gameplay | `TutorialPresenter` (UI) | Tutorial mantığı modal paneli açar ama canvas'ı bilemez |
| `ITutorialMomentPresenter` | Gameplay | `TutorialMomentPresenter` (UI) | Oyun içi overlay için aynı ayrım |
| `ICapsuleTapInterceptor` | Gameplay | `TutorialRunSystem` (Gameplay) | Kapsül sistemi tutorial'ı bilmeden dokunuşu ona önce gösterir |
| `IAdProvider` | Core | `DummyAdProvider` (UI) | Reklam servisi kuralları yönetir, reklamı gösteren SDK ya da dummy overlay ayrı takılır |

### Çalışma zamanı nesne grafiği

```text
GameRunner (tek MonoBehaviour, DontDestroyOnLoad)
└── GameContext                       uygulama ömrü
    ├── GameLoop                      Tick / FixedTick / LateTick dağıtıcısı
    ├── ServiceProvider               Asset, Pool, Scene, Input, Audio, Save, IAP, Ads, Analytics
    ├── CancellationToken             uygulama token'ı
    ├── GlobalSettings, LevelCatalog  boot'ta yüklenen global veriler
    └── GameplayLevelIndex            sahneler arası "hangi level oynanacak" bilgisi

Sahne başına composition root
├── BootMenu      menü panelleri, mağaza, ayarlar
├── BootGameplay  GameplaySession + GameplayPauseMenu + sonuç ekranı
└── BootLevelTest aynı session, test sahnesi akışı

GameplaySession (bir level denemesi ömrü, kendi CancellationToken'ı)
└── Level, Targets, Capsules, Spawn, Pacing, Score, Powers, Feedback, Vfx, CameraFramer, Tutorial
```

Oynanış sistemleri düz C# sınıflarıdır. MonoBehaviour yalnızca görsel uçlarda (`CapsuleController`,
`TargetController`, view'lar, canvas'lar) kullanılır. Sistemler birbirleriyle ya constructor'dan
gelen referansla ya da event bus üzerinden konuşur. UI yalnızca event dinler ve callback çağırır.

---

## Uygulama yaşam döngüsü

### 1. Boot (Init sahnesi)

`BootInit` ilk sahnede tek başına çalışır. Zaten bir `GameRunner` varsa (sahne yeniden yüklenmişse)
kendini yok eder.

1. **`AppInstaller.CreateContext`**: `GameLoop`, `ServiceProvider` ve `GameContext` oluşturulur,
   `GameRunner` host objeye eklenip context'e bağlanır. Servisler sırayla kaydedilir. Kayıt anında
   `Initialize` çağrılır ve tick arayüzü uygulayan servisler loop'a girer.
2. **`AppInstaller.LoadGlobalsAsync`** sabit bir sırayla async kurulumu yapar:
   Addressables init → Input actions → ses kataloğu → IAP (katalog + mağaza bağlantısı) →
   reklam ayarları ve provider →
   `GlobalSettingsSO` (frame rate, kalp ayarları) → `LevelManifestSO` → UI panel kataloğu →
   `EventSystem` ve `UIRoot` hazırlanır.
3. Save'e bakılır: ilk level tamamlanmadıysa doğrudan Gameplay'e, tamamlandıysa Menu'ye geçilir.

Aynı `AppInstaller` hem `BootInit` hem `BootLevelTest` tarafından kullanılır. Test sahnesi prod boot
yolunun bir kopyası değil, aynısıdır.

### 2. Sahne geçişi

`ISceneService.Load` önce yükleme panelini açar (kilitli, en üst katman), sahneyi async yükler ve
ilerlemeyi 0–0.7 aralığında raporlar. Kalan 0.7–1.0 aralığını yeni sahnenin composition root'u
doldurur. Yükleme paneli, yeni sahne kendi HUD'unu bağladıktan sonra kapatılır. Böylece oyuncu
hiçbir zaman yarım kurulmuş bir sahne görmez.

`GameContext.GameplayLevelIndex` sahneler arası tek taşınan durumdur: menü "şu levelı oyna" der,
Gameplay sahnesi okur. `-1` "save'deki mevcut level" anlamına gelir.

### 3. Bir level denemesi (GameplaySession)

`GameplaySession` bir level denemesinin baştan sona sahibidir. Her yeni deneme (retry, next) sahneyi
yeniden yükler. Session yeniden kullanılmaz. Bu sayede "önceki run'dan kalan state" sınıfı hatalar
yapısal olarak imkânsız.

```text
LoadAsync        katalog, level tanımı, layout prefab'ı, ayarlar yüklenir
                 sistemler kurulur ve birbirine bağlanır, pool ve VFX ısıtılır
                 sistemler GameLoop'a kaydedilir
(HUD bağlanır, yükleme paneli kapanır)
StartAsync       faz → Intro, PreRun adımları (run öncesi modal tutorial'lar) çalışır
                 faz → Playing, RunStarted yayınlanır
WaitForRunEnd    RunEnded (kazanma/kaybetme) beklenir
(Sonuç paneli)   Continue / Retry / Menu → sahne yeniden yüklenir
Shutdown         token iptal, sistemler loop'tan çıkar ve kapanır, kapsüller havuza döner,
                 layout instance'ı Addressables'a iade edilir
```

**Yükleme hata politikası:** Zorunlu bir asset yüklenemezse session hatayı loglar, yükleme ekranını
kapatır ve `false` döner. Ancak yükleme iptal nedeniyle yarıda kaldıysa (sahneden çıkılmışsa) hiçbir
şey loglanmaz. İptal bir hata değildir. VFX ve tutorial katalogları opsiyoneldir, yoksa oyun onlarsız
çalışır.

**Kapanış sırası** kurulumun tersidir ve bilinçlidir: önce token iptal edilir (havadaki tüm await'ler
uyanıp çıkar), sonra sistemler loop'tan çıkarılır, sonra kapsüller havuza iade edilir, en son
layout bırakılır. Havuza iade, layout'tan önce yapılmak zorunda. Kapsüller layout'un altında
yaşar ve layout yok edilirse onlarla birlikte yok olurlar.

Aynı nedenle session, sahne değişmeden **önce** kapatılır: `BootGameplay` önce yükleme ekranını açar,
sonra session'ı kapatır, en son yeni sahneyi yükler. Böylece kapsüller, kutular ve VFX sahneyle
birlikte yok olmak yerine havuza döner. `OnDestroy`'daki kapatma yalnızca yedektir (uygulamadan çıkış,
Play modunun durması). O yolda sahne objeleri Unity tarafından zaten yok edilmiş olabileceği için
havuza iade noktaları yok edilmiş objeleri atlar.

### 4. Pause

Level'ın pause durumu bir **flags enum**'dır: `Menu`, `Application`, `Tutorial`. Her kaynak kendi
bayrağını koyar ve kaldırır. Level ancak hiçbir bayrak kalmadığında devam eder. Böylece örneğin
tutorial açıkken uygulama arka plana atılıp geri geldiğinde, "Application" bayrağının kalkması
tutorial'ın koyduğu pause'u yanlışlıkla bozmaz.

`GameplayPauseMenu` hem ayarlar butonunu hem uygulama duraklatmasını yönetir. Uygulama arka plandan
dönünce oyun kaldığı yerden devam etmez, ayarlar paneli açık olarak bekler. Oyuncu hazır olunca
kapatır.

---

## Çekirdek altyapı

### ServiceProvider

Basit bir service locator. `Register<T>(service)` şunları yapar:

- servisi `Initialize` eder ve kayıt sırasını saklar,
- servis `ITickable`/`IFixedTickable`/`ILateTickable` uyguluyorsa `GameLoop`'a ekler,
- servisi hem `T` hem de servisin uyguladığı tüm `IService` türevi arayüzler altında erişilebilir yapar.

`Dispose` servisleri **kayıt sırasının tersine** kapatır, çünkü sonra kaydedilen servis öncekine bağımlı
olabilir. Servisler `GameContext.Services.Get<T>()` ile alınır. Oynanış sistemlerine ise
composition root tarafından constructor'dan verilir. Oynanış kodu locator'ı hiç görmez.

### GameLoop ve GameRunner

Projede Update alan tek sınıf `GameRunner`. Her frame `GameLoop.Tick/FixedTick/LateTick` çağrılır,
loop da kayıtlı sistemlere dağıtır.

- Dağıtım, kayıtlı sistemlerin bir anlık görüntüsü (dizi) üzerinden yapılır. Görüntü yalnızca bir sistem
  eklenip çıkarıldığında yenilenir, her frame kopyalanmaz. Tick sırasında sistem eklemek/çıkarmak
  güvenlidir, değişiklik bir sonraki tick'te geçerli olur.
- Her tickable ayrı try/catch içinde çağrılır. Unity'nin MonoBehaviour başına verdiği izolasyonu
  merkezi loop'ta korumak için: bozuk bir sistem her frame exception atsa bile diğerleri çalışmaya
  devam eder, oyun donmaz.

`GameRunner` ayrıca `OnApplicationPause`'da save'i flush eder ve `ApplicationPauseChanged` event'ini
yayınlar.

### Event bus (EB)

Tip bazlı, struct event'li, allocation'sız bir bus. Üç ayrı kanal var. Kanal, event'in **kime**
gittiğini söyler:

| Kanal | Kim dinler | Örnek event'ler |
| --- | --- | --- |
| `EB.Gameplay` | Oynanış sistemleri | `RunStarted`, `RunFinished`, `CapsuleSpawned`, `CapsuleResolved`, `AllTargetsFilled`, `RunHealthDepleted`, `TutorialFinished` |
| `EB.Presentation` | UI, ses ve composition root'lar | `RunHudChanged`, `RunTargetFillChanged`, `RunEnded`, `SpecialPowerHudChanged`, panel aç/kapa istekleri ve cevapları, `LoadingProgressChanged`, `ApplicationPauseChanged`, `AdStarted`, `AdFinished` |
| `EB.Analytics` | Yalnızca `AnalyticsSystem` | `MatchStart/Win/Lose`, `SpecialPowerUse`, `TutorialStart/Page/End`, `AdShow` |

Kurallar:

- Oynanış, UI'ı asla doğrudan çağırmaz. HUD'un ihtiyaç duyduğu her şey Presentation kanalına yayınlanır.
- Oynanış sistemleri birbirini mümkün olduğunca event ile duyar. Örneğin skor, spawn ve tutorial
  sistemleri `LevelSystem`'i çağırmaz. `RunStarted`/`RunFinished`'i dinler.
- Listener'lar senkron çalışır ve exception yutulmaz. Bir listener'ın fırlatması, aynı event'in sonraki
  listener'larını keser. Listener'lar fırlatmamalıdır.
- Kanallar `GameContext.Dispose` ile temizlenir.

### Async ve iptal modeli

Coroutine yok. Tüm async akış UniTask. İki seviyeli token var:

- **Uygulama token'ı** (`GameContext.CancellationToken`): sahne yüklemeleri ve global işler.
- **Session token'ı** (`GameplaySession.Token`): level denemesine ait her şey. Session kapanınca iptal olur.

Uyulan kurallar:

1. **Her await'ten sonra dünya değişmiş olabilir.** Kapsül uçuşu, kutu çıkışı gibi await'lerden sonra
   token'a ve level fazına tekrar bakılır. Aksi halde run bittikten sonra kutuya oturan bir kapsül,
   havuza zaten iade edilmiş bir objeyi ikinci kez sahiplenir.
2. **İptal exception olarak yukarı taşınmaz.** `SuppressCancellationThrow` ile `(canceled, result)`
   çiftine çevrilir, akış sessizce çıkar.
3. **Tween'ler de iptale uyar.** `WaitForEnd` uzantısı bir tween'in bitmesini ya da öldürülmesini bekler.
   Token iptal olursa tween'i öldürür. Uçuş ve kutu çıkışı bu yolla beklenir.
4. **Pause await'lere de yansır.** Uçuşu biten kapsül, level pause'daysa kutuya oturmadan önce
   pause'un bitmesini bekler.

### Assetler ve havuz

`IAssetProvider` Addressables'ı sarar:

- `LoadAsset<T>(key)` handle'ı anahtar başına tutar ve **referans sayar**. Aynı anahtar ikinci kez
  yüklenmez, yalnızca sayacı artar. `ReleaseAsset(key)` sayacı azaltır, sayaç sıfıra inince asset
  Addressables'a bırakılır. Kural basit: her `LoadAsset` çağrısının sahibi, işi bitince bir kez
  `ReleaseAsset` çağırır.
- Hata ya da iptal durumunda exception fırlatmaz, `null` döner (hata loglanır). Eksik bir adres
  boot'u değil, yalnızca o özelliği bozar.
- `Instantiate` ile oluşturulan instance'ların handle'ları tutulur, `ReleaseInstance` ile iade edilir.
- Servis kapanırken tüm handle'lar bırakılır.

`IGameObjectPool` Addressables adresi anahtarlı bir havuzdur. Pasif objeler `DontDestroyOnLoad`
edilmiş kapalı bir kök altında bekler. `Release` idempotent'tir. Zaten havuzdaki bir objeyi ikinci
kez iade etmek zararsızdır (run biterken aynı kapsül iki yoldan iade edilebilir). `ReleaseInactive(key)`
bir anahtarın havuzda bekleyen tüm instance'larını Addressables'a iade eder.

Asset'lerin sahipliği ömürlerine göre ayrılmıştır:

| Sahip | Ne tutar | Ne zaman bırakır |
| --- | --- | --- |
| Servisler ve `AppInstaller` | Global ayarlar, manifest, ses/IAP/reklam/panel katalogları, input actions | Uygulama kapanırken |
| `GameplaySession` | Level tanımı, hedef/VFX/tutorial/güç katalogları, feedback ayarları, layout instance'ı | Session kapanırken (her retry/next'te) |
| `TargetSystem` / `VfxSystem` | Kutu ve VFX havuzları | Session kapanırken havuzları boşaltır. Kapsül havuzu her levelda kullanıldığı için kalır |
| `UIRoot` | Açılan her panelin prefab'ı | Panel instance'ı gerçekten yok edildiğinde (önbellekten atılınca ya da katman temizlenince) |
| `BootMenu` | Menüde kullandığı IAP ve güç katalogları | Menü sahnesinden çıkılınca |

Level yüklenirken kapsül havuzu ısıtılır:
`MaxActive + (görünür kutu sayısı × en büyük kutu kapasitesi) + 2`. Kutuya oturan kapsüller banttan
çıkmış ama havuza dönmemiş olduğu için ikinci terim gerekiyor.

---

## Oynanış sistemleri

`GameplaySession` şu sistemleri kurar ve bağlar:

| Sistem | Sorumluluk |
| --- | --- |
| `LevelSystem` | Faz makinesi, geçen süre, pause bayrakları, run sonucu (save + analytics + `RunEnded`) |
| `RunScoreSystem` | Skor, kombo, en iyi kombo, can. Can bitince `RunHealthDepleted` |
| `CapsuleSystem` | Aktif kapsüllerin hareketi, dokunma algılama, uçuş ve oturma akışı |
| `SpawnSystem` / `CapsuleFactory` | Kapsülü havuzdan alıp başlatma, kayıt ve iade |
| `SpawnPacingSystem` | Ne zaman, hangi path'ten, hangi tür kapsül ve hangi hızla spawn edileceği |
| `TargetSystem` / `TargetFactory` | Hedef kutu kuyruğu, görünür kutular, kayma ve doldurma |
| `SpecialPowerSystem` | Özel güçlerin kilidi, gizlenmesi, aktivasyonu ve süresi |
| `GameplayFeedback` / `VfxSystem` | Ses, kamera sarsıntısı ve havuzlanmış VFX |
| `LevelCameraFramer` | Kamerayı layout'a ve ekran oranına göre konumlama |
| `TutorialRunSystem` | Run içi tutorial'lar (aşağıda ayrı bölüm) |

### Faz makinesi

```text
None ──► Intro ──► Playing ──► Complete
  │                  ▲    └──► Fail
  └──────────────────┘
```

İzin verilmeyen geçişler reddedilir ve uyarı loglanır. `Intro`, run öncesi adımların (modal
tutorial'lar) çalıştığı fazdır. "Simüle ediliyor" durumu `Playing` fazı **ve** hiç pause bayrağı
olmaması demektir. Kapsül hareketi, spawn ve süre sayacı yalnızca bu durumda ilerler.

Run sonu: `AllTargetsFilled` → Complete (save'e skor ve ilerleme yazılır), `RunHealthDepleted` →
Fail (bir kalp harcanır). İkisi de önce `RunFinished`'i (oynanış sistemleri kendini durdursun diye),
sonra `RunEnded`'i (UI sonuç ekranını açsın diye) yayınlar.

### Dokunuştan kutuya

1. Dokunuş UI'ın üstündeyse yok sayılır (EventSystem raycast).
2. Fizik raycast'i kapsül katmanında en yakın kapsülü bulur.
3. Varsa tutorial interceptor'ı dokunuşu önce görür ve isterse yutar.
4. **Normal kapsül:** rengine uyan ve boş yeri olan görünür kutu aranır. Kutuda bir slot **rezerve**
   edilir, sonra kapsül yay çizerek o slota uçar ve oturur. Rezervasyon, uçuş sürerken aynı son slota
   ikinci bir kapsülün yönelmesini engeller. Uçuş iptal olursa rezervasyon geri alınır.
5. **Altın / zehir:** kapsül yukarı doğru uçar, sonucu (`CapsuleResolved`) yayınlanır, havuza döner.
6. Kutu dolunca çıkış animasyonu oynar, içindeki kapsüller havuza döner, kalan kutular kayar,
   kuyruktan yenisi girer. Kuyruk ve görünür kutular bitince `AllTargetsFilled` yayınlanır.

### Spawn temposu

- Her path'in kendi zamanlayıcısı vardır. Başlangıçta kademeli ayarlanır ki path'ler aynı anda
  spawn etmesin. Her path aynı anda en fazla bir spawn işini yürütür.
- Bant hızı: `min(MaxConveyorSpeed, ConveyorSpeed + geçen süre × SpeedRamp) × özel güç çarpanı`.
- Spawn aralığı, bant hızının başlangıç–maksimum aralığındaki ilerlemesine göre `SpawnInterval`'dan
  `MinSpawnInterval`'a doğru daralır. Hız ve yoğunluk tek bir eğriden türer.
- Sahnedeki kapsül sayısı `MaxActive`'e ulaşınca spawn bekler.
- Kapsül türü `PoisonChance` / `GoldChance` ile seçilir. Normal kapsülün rengi, path'in kendi
  renk ağırlıklarından seçilir. Tutorial'lar belirli spawn sıralarına tür zorlayabilir
  ("5. kapsül zehir olsun").

### Skor ve can

Doğru kapsül `ScorePerCorrect × kombo` puan verir ve kombo'yu artırır. Altın `ScorePerCorrect ×
max(1, kombo)` verir, kombo'yu bozmaz. Zehir kombo'yu sıfırlar ve bir can götürür. Ölümsüzlük
(IAP ile alınır) aktifken can gitmez.

### Özel güçler

Katalogda her güç için bir açılma leveli ve başlangıç şarjı var. Oyuncu açılma leveline ulaştığında
başlangıç şarjı **bir kez** verilir (save'de işaretlenir). "Ulaşılan level", save'deki ilerleme ile o an
oynanan leveldan büyük olanıdır. Normal akışta ikisi aynıdır, test sahnesinde ise save sıfırken de
oynanan levelın gücü ve tutorial'ı çalışır. Güç açılmış olsa bile, onu tanıtan
tutorial henüz gösterilmediyse o run için **gizlenir** ve tutorial bitince görünür hâle gelir. Böylece
oyuncu butonu, ne işe yaradığını öğrenmeden görmez.

Aynı anda tek güç aktif olabilir. Mevcut tek güç (Slow) `SpawnPacingSystem`'in hız çarpanını düşürür.
Süre pause sırasında akmaz.

### Kamera

Kamera layout prefab'ında değil, sahnedeki camera rig'dedir. Level tanımı yalnızca açı, FOV, kenar
payı ve ek uzaklık verir. `LevelCameraFramer` her path'ten örnek noktalar alır ve rig'i, tüm path'ler
HUD'un altı ile kutu şeridinin üstü arasındaki alana her en-boy oranında sığacak şekilde konumlar.
Kutu şeridi kameraya göre yerleştirilir. Mesafe değişse de kutuların ekrandaki yeri ve boyutu sabit
kalır. Bu yüzden layout'taki `TargetSpawnPoint` sahnede elle taşınamaz, konumu her kadrajda yeniden
hesaplanır. Şeridin ekrandaki dikey yeri, yan boşluğu ve en yakın mesafesi `GlobalSettingsSO`'nun
Camera Framing alanlarından, kutu aralığı ve görünen kutu sayısı `TargetCatalogSO`'dan gelir.

Editörde kadraj her frame yeniden hesaplanır, böylece bu değerler Play modunda canlı ayarlanabilir.
Build'de değerler değişmediği için kadraj yalnızca ekran oranı değiştiğinde (örneğin cihaz
döndürüldüğünde) yeniden hesaplanır.

---

## Tutorial sistemi

Tutorial'lar tamamen veriyle tanımlanır (`TutorialDefinitionSO`), kod değişikliği istemez. Her tanım
dört soruya cevap verir:

| Soru | Seçenekler |
| --- | --- |
| **Ne zaman uygun?** (Condition) | Belirli level, belirli level + o levelda belirli kapsül türü var, özel gücün açıldığı level |
| **Hangi anda?** (Moment) | Run başlamadan önce, belirli türde kapsül ekrana girdiğinde, N kapsül spawn olduğunda, N kapsül çözüldüğünde |
| **Nasıl gösterilir?** (Presentation) | Modal panel (sayfalar, ileri/atla) ya da oyun içi overlay (işaretçi + metin) |
| **Nasıl kapanır?** (Completion) | Herhangi bir yere dokununca, işaret edilen kapsüle dokununca |

Akış:

- Yükleme sırasında o levelda gösterilecek tutorial'lar önceden belirlenir. Tanıttıkları özel güç
  gizlenir ve istedikleri zorlanmış spawn'lar pacing sistemine verilir.
- **Run öncesi** tutorial'lar `PreRunSequence` içinde modal olarak gösterilir. PreRun, run başlamadan
  önce sırayla çalışan adımlar listesidir. İleride "günlük ödül", "level hedefi" gibi ekranlar da
  aynı yere adım olarak eklenir.
- **Run içi** tutorial'ları `TutorialRunSystem` her tick'te kontrol eder. Tetiklenince level
  `Tutorial` bayrağıyla pause edilir, overlay açılır. Kapsül dokunuşları interceptor olarak önce
  tutorial'a gider. Sayfa değiştirir ya da tutorial'ı bitirir. "İşaretli kapsüle dokun" tamamlamasında
  o dokunuş yutulmaz, kapsül normal şekilde kutuya uçar. Oyuncu mekaniği yaparak öğrenir.
- Bir sayfa açıldıktan sonra kısa bir süre dokunuş kabul edilmez, hızlı dokunan oyuncu metni
  okumadan geçmesin diye.
- Görülen tutorial id'leri save'e yazılır. Bir tutorial oyuncuya bir kez gösterilir.
- Her tutorial için başlangıç, sayfa ve bitiş (atlandı mı, kaç sayfa görüldü, süre) analytics'e gider.

---

## UI katmanı

### Panel yöneticisi

`UIRoot`, Init sahnesinde duran ve `DontDestroyOnLoad` olan tek UI kökü. Paneller
`UIPanelCatalogSO`'da `EUIPanel` → Addressables prefab referansı olarak tanımlıdır.

- Panel açma bir **istek/cevap** akışıdır: `OpenUIPanelEvent` yayınlanır, `UIRoot` prefab'ı async
  yükleyip instantiate eder ve `UIPanelOpened` (ya da `UIPanelOpenFailed`) yayınlar. Kod tarafında
  bu, `UIPanels.OpenAsync(panel, layer)` ile tek bir await'e indirgenir ve açılan instance döner.
- **Katmanlar:** 0 ekranlar (menü, HUD), 1 popup'lar (ayarlar, mağaza, sonuç, tutorial), 2 yükleme.
  Katmanlar sıralamayı belirler.
- **Additive olmayan** bir panel açıldığında, aynı katmandaki kilitsiz paneller kapanır. Yükleme
  paneli **kilitli** açılır ve ancak açıkça kapatılınca gider.
- Kapanan paneller yok edilmez, kapatılıp saklanır (en fazla üç tane). Tekrar açılınca yeniden
  yüklenmez. Sahne değişirken composition root'lar ilgili katmanı toptan temizler.

### View'lar pasiftir

Canvas sınıfları (`MainMenuCanvasUI`, `GameplayCanvasUI`, `WinCanvasUI`…) oyun kurallarını bilmez.
Composition root panel açıldığında `Bind(...)` ile ihtiyaç duyduğu veriyi ve callback'leri verir.
View yalnızca gösterir ve butona basılınca callback'i çağırır. Örneğin "retry butonu aktif mi"
kararı kalp sayısına bakarak `BootGameplay`'de verilir, view'a yalnızca `null` ya da bir callback gelir.
HUD sayıları EB.Presentation event'lerinden güncellenir.

---

## Servisler

| Servis | Arayüz | Özet |
| --- | --- | --- |
| Asset | `IAssetProvider` | Addressables sarmalayıcı, handle cache, hata durumunda null |
| Pool | `IGameObjectPool` | Adres anahtarlı havuz, warmup, idempotent iade |
| Scene | `ISceneService` | Enum ile sahne yükleme, yükleme paneli ve ilerleme |
| Input | `IInputService` | Input System, frame başına tek tap mandalı |
| Audio | `IAudioService` | Katalogdan SFX/müzik, sessize alma PlayerPrefs'te |
| Save | `ISaveService` | JSON save, dirty flag, atomik yazma, kalp ve ölümsüzlük zamanlayıcıları |
| IAP | `IIAPService` | Katalog → mağaza ürünü → ödül |
| Ads | `IAdService` | Level aralığına göre reklam, test/prod ad unit ayrımı, değiştirilebilir provider |
| Analytics | `IAnalyticsSystem` | EB.Analytics → N sağlayıcı |

### Save

Save bilerek en ağır parça.

- Değişiklikler hemen diske yazılmaz. Dirty flag ile toplanır ve saniyede en fazla bir kez yazılır.
  Böylece bir frame içindeki ya da art arda gelen değişiklikler tek yazmada birleşir. Uygulama arka plana
  alındığında, satın alma sonrasında ve kapanışta beklemeden flush edilir.
- Yazma atomiktir: dosya önce `.tmp`'ye yazılır, sonra `File.Replace` ile asıl dosyayla takas edilir
  ve eski sürüm `.bak` olarak kalır. Yazma ortasında uygulama ölse bile kayıt bozulmaz.
- Okuma başarısız olursa önce `.bak` denenir, o da yoksa varsayılan save oluşturulur.
- `SaveData.Version` şema değiştiğinde migration yazılabilsin diye tutuluyor. Şu an tek sürüm var.

**Kalpler** zaman damgasıyla tutulur: "bir sonraki kalp şu Unix zamanında dolacak". Uygulama açıkken
saniyede bir kontrol edilir. Kapalıyken geçen süre açılıştaki ilk kontrolde toplu olarak hesaplanır. Satın alınan
kalpler maksimumun üstüne çıkabilir (menüde `5(+3)` gösterilir). Maksimumun üstündeyken sayaç durur.
**Ölümsüzlük** de aynı şekilde bir bitiş zaman damgasıdır. Üst üste alınırsa süreler toplanır.

### IAP

Katalogdaki her ürün bir ödül türü (ölümsüzlük dakikası, özel güç şarjı, kalp) ve miktarıdır.
Ürünler mağazaya katalog anahtarıyla (store'a özel id'lerle eşlenerek) consumable olarak tanıtılır.
Satın alma pending'e düşünce ödül verilir, save flush edilir, sonra satın alma onaylanır. Editörde
mağaza bağlanmaz. Satın alma doğrudan ödülü verir, böylece mağaza akışı gerçek hesap olmadan test
edilebilir. Mağaza ürünleri 8 saniye içinde gelmezse satın alma kapalı kalır.

### Reklamlar

Reklam kuralları ile reklamı gösteren taraf ayrı:

- **`AdService`** (Core) kuralı bilir. `AdSettingsSO`'yu yükler, bir levelın ardından reklam gerekip
  gerekmediğine karar verir, gösterim başında `AdStarted`, sonunda `AdFinished` ve analytics'e `AdShow`
  yayınlar.
- **`IAdProvider`** reklamı gerçekten gösteren taraftır. Bir SDK geldiğinde yalnızca bu arayüz
  implemente edilip `AppInstaller`'daki kayıtta değiştirilir. Oynanış ve composition root'lar
  hiçbir şey bilmez.
- **`DummyAdProvider`** (UI) şimdilik SDK yerine çalışır. `DummyAdCanvas` prefab'ını (`ui.dummy.ad`
  adresi) spawn eder, `DummyAdView` üzerinden reklam türünü, test/prod ortamını ve ad unit id'sini
  yazar. Butona basılınca reklam tamamlanmış sayılır ve instance Addressables'a iade edilir.
  Görünüm tamamen prefab'da, yazılar da `DummyAdView`'un inspector'ından değiştirilebilir.
  SDK'lar reklamı oyunun UI sisteminin dışında kendi tam ekran katmanında çizdiği için dummy de
  bilerek `UIRoot` ve panel kataloğunu kullanmaz, kendi canvas'ıyla her şeyin üstünde açılır.

`AdSettingsSO` ayarları:

| Alan | Anlamı |
| --- | --- |
| Type | Interstitial ya da Rewarded |
| Level Interval | Kaç levelda bir reklam çıkacağı. 3 ise 3., 6., 9. level kazanıldıktan sonra |
| Test Ads | Açıksa test ad unit id'si, kapalıysa production id'si kullanılır ve provider test modunda başlatılır |
| Test / Production Ad Unit Id | İki ortamın ad unit id'leri |

Reklam, kazanılan levelın sonuç ekranında Continue'ya basıldıktan sonra, bir sonraki levela ya da
menüye geçmeden önce gösterilir. Kaybedilen levellardan sonra reklam çıkmaz. Reklam süresince
`AudioSystem` `AdStarted`/`AdFinished` event'leriyle tüm sesi duraklatır. Test sahnesi de aynı kuralı
uygular, böylece reklam akışı Init'ten oynamadan denenebilir.

**Rewarded reklamın henüz bir bağlamı yok.** Type Rewarded seçilirse reklam, interstitial ile aynı
yerde (belirli aralıklarla level kazanıldıktan sonra) zorunlu olarak çıkar ve sonucu hiçbir ödüle
bağlanmaz. Bu bilinçli bir boşluk. Rewarded'ın anlamlı olması için oyuncunun kendi seçtiği bir giriş
noktası (örneğin kaybedince "reklam izle, kalp kazan" ya da mağazada "reklam izle, özel güç şarjı al")
ve `Completed` sonucunda ödülü `ISaveService` üzerinden veren bir akış gerekir. `Skipped` ve `Failed`
sonuçları da o akışta ödülsüz kapanmalı.

### Analytics

Oynanış kodu yalnızca `EB.Analytics`'e struct yayınlar. `AnalyticsSystem` bu event'leri dinleyip
kayıtlı her `IAnalytics` sağlayıcısına dağıtır. Şu an tek sağlayıcı `AnalyticsLog` (yalnızca
development build'de konsola yazar). Firebase ya da başka bir SDK eklemek, `IAnalytics`'i uygulayıp
`AppInstaller`'da bir `Register` çağrısı eklemekten ibaret. Oynanış koduna dokunulmaz.

---

## İçerik hattı ve editör araçları

### Veri

Tüm içerik ScriptableObject ve Addressables ile gelir:

| Asset | Ne tutar |
| --- | --- |
| `LevelManifestSO` | Level sırası (definition referansları) ve varsayılan layout |
| `LevelDefinitionSO` | Can, puan, spawn/hız eğrisi, kapsül türleri ve olasılıkları, hedef kuyruğu, layout, kamera |
| Layout prefab'ı (`LevelLayout`) | Path'ler, kapsül kökü, kutu spawn noktası ve çıkışı, dokunma katmanı |
| `CapsuleColorSO` | Bir renk. Yeni renk kod istemez |
| `CapsuleDefinitionSO` | Normal/altın/zehir kapsül tanımı, prefab adresi, uçuş süresi |
| `TargetCatalogSO` | Kapasiteye göre kutu prefab'ları, görünür kutu sayısı, aralık, kayma süresi |
| `SpecialPowerCatalogSO` / `SpecialPowerDefinitionSO` | Güçler, açılma leveli, başlangıç şarjı, süre ve çarpan |
| `TutorialCatalogSO` / `TutorialDefinitionSO` | Tutorial'lar (yukarıya bakınız) |
| `IAPCatalogSO` | Ürünler, store id'leri, ödüller, gösterilen fiyat metni |
| `GlobalSettingsSO` | Kalp sayısı ve dolum süresi, frame rate, kamera/HUD kadrajı |
| `AdSettingsSO` | Reklam türü, kaç levelda bir çıkacağı, test/prod ad unit id'leri |
| `FeedbackSettingsSO` | Olay başına sarsıntı süre/şiddeti |
| `AudioCatalogSO`, `VfxCatalogSO`, `UIPanelCatalogSO` | Enum → ses / VFX prefab / panel prefab eşlemeleri |

Global asset'ler sabit Addressables adresleriyle yüklenir (`AddressableKeys`). Level tanımları
manifest'teki `AssetReference`'lar üzerinden gelir. Adres konvansiyonu `def.level.N` ve
`layout.level.N`, editör araçları bunları otomatik atar. Tasarımcı geri bildirimi `OnValidate` ve
özel inspector'lardan gelir. Sayıların hiçbiri build istemez.

### Level editörü

`PillFrenzy/Level Editor` tüm levelları, layout'larını ve renkleri tek pencerede gösterir. Buradan
level tanımı seçilir, layout prefab'ı açılır, levela özel yeni layout (varsayılandan kopyalanarak)
üretilir, yeni renk oluşturulur. Level tanımının inspector'ı hedef kuyruğunun renk başına kaç kapsül
istediğini özetler. Layout'ta o rengi spawn eden path yoksa hata gösterir. **Create Next Level**
seçili tanımı kopyalar, manifest'e ekler, adresini atar.

Layout, Unity Splines üzerine kuruludur. Her path ayrı bir `SplineContainer`. Başı ve sonu Scene
view'da etiketli, akış yönü oklarla görünür, Y ekseni serbesttir. `LevelLayout` inspector'ından path
eklenir, silinir, knot düzenleme moduna geçilir. Path'in kendi inspector'ından yönü çevrilir. Her path kendi renk ağırlıklarıyla
spawn eder. Hız ve `MaxActive` level geneli.

Bant mesh'i (`PathMesh`) editörde spline değiştikçe yeniden üretilir. Oyunda ise layout yüklenirken
bir kez üretilir, build'de spline'ı izleyen bir kod çalışmaz. Segment/Start/End mesh
slotları boşsa prosedürel bant ve kapaklar üretilir. Doluysa segment mesh'i path boyunca döşenip
bükülür, kapaklar uçlara yerleşir. Segment mesh'i +Z boyunca, üst yüzeyi y = 0'da modellenmeli.
Kapaklar +Z yönüne dışarı bakar.

---

## Performans notları

Sıcak yollar (her frame ya da her dokunuşta çalışan kod) için verilen kararlar. Hiçbiri henüz cihazda
profiler ile ölçülmedi, bilinen maliyet kalıplarına göre alındı.

**Kapsül hareketi.** Kapsüller her frame spline'ı değerlendirmez. `LevelPath` yüklenirken yolu 5 cm
aralıklarla bir kez örnekler ve pozisyon/rotasyon tablosuna yazar. Kapsülün pozu, gittiği mesafeye göre
iki komşu örnek arasında ara değer alınarak bulunur. Bant hızı da kapsül başına değil, `CapsuleSystem`'de
tek bir değer olarak tutulur. Her kapsül frame başına yalnızca `Advance(distance)` ile ilerler.

**Fizik.** Kapsül prefab'ında kinematic, yerçekimsiz bir Rigidbody var. Collider'ı olup Rigidbody'si
olmayan ve her frame taşınan objeler PhysX'te statik collider sayılır ve taşınmaları pahalıdır.
Dokunma raycast'i yalnızca kapsül katmanına ve bir kez yapılır.

**Allocation'sız UI metni.** Skor, kombo, can, kutu doluluğu, süreler, kalp sayısı, yükleme yüzdesi ve
güç şarjı gibi sık değişen yazılar yeni string üretmez. `UiText` paylaşılan bir char buffer'a yazıp
TMP'ye `SetText(char[])` ile verir, kutu doluluğu `SetText(StringBuilder)` ile yazılır. Renk adları
`Object.name` yerine `CapsuleColorSO.DisplayName` ile önbellekten okunur. Menüdeki kalp ve Play
etiketleri yalnızca değer değiştiğinde yazılır. Ekran başına bir kez yazılan metinler (sonuç ekranı,
tutorial sayfaları) bilerek sade string olarak kaldı.

**Seyrek güncelleme.** Save bakımı ve menü/HUD sayaçları saniyede bir çalışır. Özel güç aktifken güç
çubuğu her frame yalnızca süre yazısını günceller, geri kalanı değişiklik event'iyle tazelenir. Kamera
kadrajı build'de yalnızca ekran oranı değişince hesaplanır.

**Önbellekler.** `MaterialColorSetter` shader property id'sini ve materyal sayısını bir kez hesaplar
(`sharedMaterials` her okumada yeni dizi döndürür). Kapsül collider'ını ve uçuş path dizisini tekrar
kullanır. Save'deki aramalar closure üreten `List.Find` yerine düz döngüyle yapılır.

**Bilerek yapılmayanlar.** DOTween recycling kapalı, çünkü kodda tutulan tween referansları başka bir
tween'e işaret edebilir hâle gelir. Event bus'taki `Dictionary<Type>` araması event başına ihmal
edilebilir olduğu için değiştirilmedi. HUD'da sık değişen yazıları ayrı bir alt `Canvas`'a almak,
tüm HUD'un yeniden çizilmesini önlerdi. Bu bir prefab düzenlemesi ve henüz yapılmadı.

---

## Kararlar ve gerekçeleri

**Assembly definition, sadece namespace değil.** Katman ihlali bir review yorumu değil, derleme
hatası olsun istedim. Composition root'ları Bootstrap'te tutmak Core ve Gameplay'in sahneye geri
uzanmasını engelliyor.

**Service locator, DI konteyneri değil.** On servis ve sabit bir boot sırası için Zenject/VContainer
fazla. Locator yalnızca composition root'larda kullanılıyor. Oynanış sistemleri bağımlılıklarını
constructor'dan alıyor, yani DI'ın test edilebilirlik kazancı büyük ölçüde korunuyor ve DI
bilmeyen bir geliştirici için öğrenme maliyeti yok.

**Tek tick döngüsü, MonoBehaviour'suz oynanış.** Update sırası belirli, sistemler sahnesiz
kurulabilir, skor/kalp/pacing gibi mantık EditMode'da test edilebilir. Bedeli, Unity'nin
MonoBehaviour başına verdiği hata izolasyonunu elle sağlamak. Bunu loop'taki try/catch yapıyor.

**Üç kanallı event bus.** Tek kanalda "bu event'i kim dinliyor" sorusu cevapsız kalıyor. Kanal,
event'in hedef kitlesini adıyla söylüyor ve UI'ın oynanış event'lerine, oynanışın UI event'lerine
yaslanmasını zorlaştırıyor.

**Session başına sistem kurulumu, sahne başına composition root.** Kurulum tek bir metotta yukarıdan
aşağı okunuyor. Hangi sistemin neye bağlı olduğu constructor'lardan görülüyor. Her denemede sahneyi
yeniden yükleyip her şeyi baştan kurmak, sistemlere "reset" yazmaktan daha az hata üretiyor.

**Oyun sistemleri birbirini event'le duyuyor.** `LevelSystem` yalnızca faz ve sonucu yönetiyor.
Skor, spawn, tutorial ve güç sistemleri ona referans vermek yerine run event'lerini dinliyor. Yeni bir
sistem eklemek mevcutları değiştirmeyi gerektirmiyor.

**Pause bir bayrak kümesi.** Pause'u tek bir bool yapmak, birden fazla kaynağın (menü, uygulama,
tutorial) birbirinin pause'unu kaldırmasına yol açıyor. Bayraklarla her kaynak yalnızca kendi
pause'undan sorumlu.

**Yükleme hataları gürültülü, iptaller sessiz.** Eksik zorunlu asset loglanıp akış durdurulur.
Kullanıcı sahneden çıktığı için yarıda kalan yükleme ise normal akış sayılır ve loglanmaz.

**İzole test sahnesi.** Level ve feel iterasyonu için Init → Menu → Play turunu beklememek bilinçli bir
ergonomi kararı. Test sahnesi aynı `AppInstaller` ve aynı `GameplaySession`'ı kullanıyor. Ayrı bir
kod yolu olmadığı için prod davranışından sapmıyor.

**ScriptableObject her yerde.** Level, kapsül, renk, hedef, güç, tutorial, ses, IAP, panel ve feel
ayarları veri. Tasarımcı Inspector'da çalışır, build istemez.

**Addressables ve havuz arayüz arkasında.** Oynanış, asset'in nereden ve nasıl geldiğini bilmiyor.
`LoadAsset` fırlatmıyor. Eksik bir adres boot'u değil, o özelliği bozuyor.

---

## Yeni bir şey eklerken

| Eklenecek | Nereye dokunulur |
| --- | --- |
| Level | Level Editor'da bir tanım seçip **Create Next Level**. Gerekirse **New Layout**. Kod yok |
| Renk | Level Editor'da **New Color**. Path'lerin renk ağırlıklarına ve hedef kuyruğuna ekle. Kod yok |
| Tutorial | `TutorialDefinitionSO` oluştur, `TutorialCatalog`'a ekle. Kod yok |
| IAP ürünü | `IAPCatalog`'a satır ekle (mevcut ödül türlerinden biriyle). Kod yok |
| Ses / VFX | Enum'a değer ekle, kataloğa eşle, `GameplayFeedback`'ten çal |
| UI paneli | `EUIPanel`'e değer ekle, prefab'ı `UIPanelCatalog`'a eşle, composition root'ta `UIPanels.OpenAsync` ile açıp `Bind` et |
| Oynanış sistemi | Düz C# sınıfı yaz, `GameplaySession.LoadAsync`'te kur, gerekiyorsa `GameLoop`'a kaydet, `Shutdown`'a ekle. Diğer sistemlerle event üzerinden konuş |
| Servis | Core'da `IService` türevi arayüz + `Service` implementasyonu, `AppInstaller`'da kayıt, gerekiyorsa `LoadGlobalsAsync`'te async init |
| Analytics sağlayıcısı | `IAnalytics` uygula, `AppInstaller`'da `Register` et |
| Reklam SDK'sı | `IAdProvider`'ı SDK ile implemente et, `AppInstaller`'da `DummyAdProvider` yerine ver |
| Run öncesi ekran | `IPreRunStep` uygula, session'da `PreRunSequence`'e ekle |
| Özel güç | Şu an yalnızca hız çarpanı var. Farklı bir etki `SpecialPowerSystem`'de kod ister |

---

## Bilerek eksik bırakılanlar

**Gerçek IAP.** Ödül pending anında, sunucu doğrulaması olmadan veriliyor. Yayın için makbuz doğrulaması,
sunucu tarafı kontrol, idempotency defteri ve Restore Purchases gerekir.

**Analytics sağlayıcısı.** Arayüz ve bus hazır. `AnalyticsLog` yalnızca konsola yazıyor.

**Mağaza fiyatları.** Katalogda elle yazılmış fiyat metni var. Gerçek build mağazadan yerelleştirilmiş
fiyatı okumalı, hem bölge hem mağaza politikası için.

**Yerelleştirme.** Oyuncu metinleri view sınıflarında sabit İngilizce. Çözüm Unity Localization ya da
custom bir tablo. UI oturmadan yapmak boşa emek olurdu.

**Test ve CI.** Assembly ayrımı ve MonoBehaviour'suz sistemler EditMode testlerini kolaylaştırıyor.
Henüz ne test assembly'si ne CI workflow'u var.

**Reklam SDK'sı.** Reklam servisi, ayarları ve event'leri hazır, ama gösterimi dummy bir overlay yapıyor.
Gerçek SDK (LevelPlay, AdMob vb.) için `IAdProvider` implementasyonu, reklam yüklenmemişse gösterimi
atlama, consent/ATT akışı ve "reklamları kaldır" IAP ürünü gerekir. Rewarded reklamın sonucu dönüyor
ama henüz bir ödüle ve oyuncunun seçtiği bir giriş noktasına bağlı değil (Reklamlar bölümüne bakınız).

**Sunucu otoriteli zaman.** Kalp dolumu ve ölümsüzlük cihaz saatini okuyor. Saati ileri almak ikisini
de bedavaya verir. Gerçek çözüm sunucu saati.

**Asset bellek yönetiminin ikinci yarısı.** Referans sayımı, session başına bırakma, panel prefab'larını
bırakma ve havuz küçültme yapıldı. Kalan iki adım içerik büyüyünce anlamlı: müzik gelince klipleri
Streaming olarak import edip katalogda doğrudan referans yerine `AssetReference` ile tutmak (aynısı
tutorial görselleri için de geçerli), ve tek Addressables grubunu global, UI, ses ve level başına
layout gruplarına bölmek. Şu an tek paket (Pack Together) olduğu için asset'ler bırakılsa da paketin
kendisi global asset'ler yüzünden hep yüklü kalıyor.

---

## Vaktim olsa sırada ne var

**MMFeedbacks.** `GameplayFeedback`, Feel'in elle yazılmış küçük bir alt kümesi. `MMF_Player`'a geçmek
ses, sarsıntı, VFX ve haptiği tek bir inspector asset'inde toplar ve sınıfı siler.

**Cihazda profil.** Performans notlarındaki kararlar bilinen maliyet kalıplarına ve akıl yürütmeye
dayanıyor. Bu repodaki hiçbir performans kararı henüz gerçek donanımda profiler'dan çıkmadı.

---

## Proje yapısı

```text
Assets/_Main/
├── Source/
│   ├── Core/        GameContext, GameLoop, EB, ServiceProvider ve servisler (asset, pool, scene,
│   │                input, audio, save, IAP, ads, analytics), global ayarlar, ILevelCatalog
│   ├── Gameplay/    Level, skor, kapsül, renk, hedef kuyruğu, spawn ve tempo, özel güç, tutorial,
│   │                path ve bant mesh'i, kamera kadrajı, feedback ve VFX, PreRun
│   ├── UI/          UIRoot ve panel API'si, canvas'lar, tutorial presenter'ları, dummy reklam,
│   │                allocation'sız metin yardımcısı (UiText)
│   ├── Bootstrap/   AppInstaller, BootInit, BootMenu, BootGameplay, BootLevelTest,
│   │                GameplaySession, GameplayPauseMenu, GameplayScreens
│   └── Editor/      Level Editor, level/layout/path inspector'ları, gizmo'lar, Scene Loader, Reset Save
├── SO/              ScriptableObject'ler (level, kapsül, renk, güç, tutorial, kataloglar, ayarlar)
├── Prefab/          Kapsül, level layout'ları, hedef kutular, UI panelleri, dummy reklam canvas'ı
└── Scene/           Init, Menu, Gameplay, Test (build dışı)
```

---

## Kapsam

Bu repo bir portföy işi. Oynanacak kadar içerik var ama asıl odak oyun değil, runtime: servislerin
nasıl ayağa kalktığı, oynanışın UI'dan nasıl ayrıldığı, bir level denemesinin nasıl kurulup
toplandığı ve yeni bir şey eklerken nereye dokunmak gerektiği. IAP'yi bilerek yayına hazır
bırakmadım. Neyi eksik bıraktığımı da yazdım, çünkü o da işin parçası.
