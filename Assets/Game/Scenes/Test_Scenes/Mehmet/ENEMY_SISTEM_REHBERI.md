# 🎮 Enemy AI Sistemi - Sahneye Entegrasyon Rehberi

## 📋 İçindekiler
1. [Gerekli Scriptler](#gerekli-scriptler)
2. [Otomatik Kurulum (Önerilen)](#otomatik-kurulum)
3. [Manuel Entegrasyon](#manuel-entegrasyon)
4. [Layer ve Physics Ayarları](#layer-ve-physics-ayarları)
5. [Test Etme](#test-etme)
6. [Sık Karşılaşılan Hatalar](#sık-karşılaşılan-hatalar)
7. [Özelleştirme](#özelleştirme)

---

## 📁 Gerekli Scriptler

### Assets/Game/Scripts/Managers/
- ✅ `GameManager.cs` - Oyun durum yöneticisi
- ✅ `TimeManager.cs` - Gündüz/Gece döngüsü
- ✅ `IDamageable.cs` - Hasar alma interface
- ✅ `Health.cs` - Genel can sistemi

### Assets/Game/Scenes/Test_Scenes/Mehmet/Scripts/
- ✅ `SimpleEnemyAI.cs` - Düşman yapay zekası
- ✅ `SimpleEnemySpawner.cs` - Düşman spawn sistemi
- ✅ `EnemyHealth.cs` - Düşman can sistemi

### Assets/Game/Scenes/Test_Scenes/Mehmet/Scripts/Editor/
- ✅ `EnemyAITestSetup.cs` - Otomatik test kurulum aracı

---

## 🚀 Otomatik Kurulum (Önerilen)

### İki Seçenek Var:

#### 1️⃣ Mevcut Sahneye Ekle (Önerilen)
Sahnenizi bozmadan sadece eksik olanları ekler:

1. Unity'de üst menüden: **Tools → Enemy AI Test Setup**
2. **"🕷️ ENEMY PREFAB OLUŞTUR"** butonuna tıkla (ilk seferinde)
3. **"➕ MEVCUT SAHNEYE EKLE"** butonuna tıkla
4. **Bitti!** ✅

Bu otomatik olarak şunları kontrol eder:
- ✅ Managers var mı? → Yoksa ekler
- ✅ Player var mı? → Yoksa ekler
- ✅ EnemySpawner var mı? → Yoksa ekler
- ⏭️ Camera ayarlarına **dokunmaz** (mevcut ayarlarınız korunur)

**Varolan objelere dokunmaz!** Sadece eksik olanları ekler.

---

#### 2️⃣ Yeni Test Sahnesi Kur (Sıfırdan Başla)
**⚠️ UYARI:** Mevcut sahneyi tamamen temizler!

1. Unity'de üst menüden: **Tools → Enemy AI Test Setup**
2. **"🕷️ ENEMY PREFAB OLUŞTUR"** butonuna tıkla (ilk seferinde)
3. **"🚀 YENİ TEST SAHNESİ KUR"** butonuna tıkla
4. Uyarıyı onayla
5. **Bitti!** ✅

Bu otomatik olarak şunları yapar:
- ⚠️ Sahneyi tamamen temizler
- ✅ Managers (GameManager + TimeManager)
- ✅ Ground (Zemin)
- ✅ Player (doğru ayarlarla)
- ✅ EnemySpawner (konfigüre edilmiş)
- ✅ Camera (orthographic, size 15)

**Test için Play'e bas!**

---

## 🔧 Manuel Entegrasyon

Mevcut bir sahneye eklemek istiyorsan:

### 1️⃣ Managers Kurulumu

**Boş GameObject oluştur:** `Managers`

**Component'leri ekle:**
```
Managers
├── GameManager.cs
└── TimeManager.cs
```

**TimeManager Inspector Ayarları:**
```
Day Duration: 60 (saniye)
Night Duration: 45 (saniye)
```

> 💡 Test için kısa süreler kullan: Day 3s, Night 15s

---

### 2️⃣ Player Hazırlığı

**Mevcut Player objesine ŞUNLAR OLMALI:**

#### GameObject Ayarları
```
Tag: Player          ⚠️ ZORUNLU
Layer: Player (8)    ⚠️ ZORUNLU
```

#### Rigidbody2D Ayarları ⚠️ ÇOK ÖNEMLİ
```
Body Type: Kinematic        ⚠️ MUTLAKA Kinematic!
Gravity Scale: 0
Constraints: Freeze Rotation ✅
Interpolation: Interpolate
```

> **❗ NEDEN Kinematic?**
> - Dynamic olursa düşmanlar player'ı iter
> - Kinematic = başka objeler itemiyor
> - Hareket için kendi movement scriptini kullan

#### Collider Ayarları
```
CircleCollider2D (veya BoxCollider2D)
├── Radius: 0.5
└── Is Trigger: false
```

#### Health Component ⚠️ ZORUNLU
```
Health.cs
└── Max Health: 100 (istediğin değer)
```

> **❗ Player mutlaka IDamageable implement etmeli!**
> Health.cs zaten IDamageable implement ediyor.

---

### 3️⃣ Enemy Prefab Oluşturma

**Yeni GameObject:** `Spider_Enemy`

#### GameObject Ayarları
```
Tag: Enemy
Layer: Enemy (9)
```

#### SpriteRenderer
```
Sprite: [Düşman görseli]
Color: Turuncu/Kırmızı
Sorting Order: 5
```

#### Rigidbody2D Ayarları
```
Body Type: Dynamic               ⚠️ MUTLAKA Dynamic!
Gravity Scale: 0
Linear Damping: 0                ⚠️ Sürtünme YOK
Angular Damping: 0
Constraints: Freeze Rotation ✅
Collision Detection: Continuous
Interpolation: Interpolate
```

#### Collider Ayarları ⚠️ ÇOK ÖNEMLİ
```
CircleCollider2D
├── Radius: 0.4
└── Is Trigger: false    ⚠️ NORMAL Collider!
```

> **❗ NEDEN Trigger DEĞİL (Normal Collider)?**
> - Normal collider = duvarlarla çarpışır ✅
> - Player ile çarpışma Physics2D Matrix'te kapatılmış (aşağıda)
> - Attack range mesafe ile kontrol ediliyor (kod içinde)

#### SimpleEnemyAI Component
```
[Movement]
Move Speed: 4

[Attack]
Attack Range: 1.2      (Collider temas mesafesi)
Attack Cooldown: 0.5   (Saniyede 2 saldırı)
Attack Damage: 10
```

> **💡 Attack Range Hesabı:**
> - Player collider radius: 0.5
> - Enemy collider radius: 0.4
> - Temas mesafesi: 0.5 + 0.4 = 0.9
> - Tolerans: +0.3
> - **Toplam: 1.2**

#### EnemyHealth Component
```
Max Health: 100
Show Debug Logs: true (test için)
```

**Prefab'a Dönüştür:**
- `Assets/Resources/Enemies/` klasörüne sürükle
- Veya istediğin klasöre kaydet

---

### 4️⃣ Enemy Spawner Kurulumu

**Boş GameObject:** `EnemySpawner`

**Component:** `SimpleEnemySpawner.cs`

#### Inspector Ayarları
```
[Enemy Prefab]
Enemy Prefab: [Spider_Enemy prefab'ı buraya sürükle] ⚠️
Pool Size: 50

[Spawn Settings]
Min Enemies Per Night: 5
Max Enemies Per Night: 15
Min Spawn Interval: 0.2s
Max Spawn Interval: 0.8s
Interval Increase: 0.05s

[Spawn Distance]
Min Distance From Player: 10
Max Distance From Player: 20

[Obstacle Check]
Obstacle Layer: Wall
Spawn Safe Radius: 1
```

> **⚠️ Enemy Prefab'ı sürüklemeyi unutma!**
> Prefab atanmazsa düşmanlar spawn olmaz.

---

## 🎛️ Layer ve Physics Ayarları

### Layer Tanımları (Project Settings → Tags and Layers)

#### Gerekli Layerlar:
```
Layer 6: Ground
Layer 7: Wall
Layer 8: Player      ⚠️ ZORUNLU
Layer 9: Enemy       ⚠️ ZORUNLU
Layer 10: Projectile
```

**Nasıl Ayarlanır?**
1. Unity'de: **Edit → Project Settings**
2. **Tags and Layers** sekmesi
3. Layers kısmında yukarıdaki layer isimlerini tanımla

---

### Physics2D Collision Matrix ⚠️ ÇOK ÖNEMLİ

#### 📍 Nerede Ayarlanır?
```
Unity'de:
Edit → Project Settings → Physics 2D
↓
En alta scroll et
↓
"Layer Collision Matrix" tablosu
```

#### 🎯 Hangi Kutular İşaretli Olmalı?

**Collision Matrix Tablosu:**
```
              Ground  Wall  Player  Enemy  Projectile
Ground          ✅     ✅     ✅      ✅        ✅
Wall            ✅     ✅     ✅      ✅        ✅
Player          ✅     ✅     ❌      ❌        ❌
Enemy           ✅     ✅     ❌      ❌        ❌
Projectile      ✅     ✅     ❌      ❌        ❌
```

#### ⚙️ Yapılacak Ayarlar:

**Bu kutucukları KALDIR (unchecked):**
1. ❌ **Player - Player** kesişimi
2. ❌ **Player - Enemy** kesişimi → **ÇOK ÖNEMLİ!**
3. ❌ **Enemy - Enemy** kesişimi → **ÇOK ÖNEMLİ!**

**Bu kutucuklar İŞARETLİ kalsın:**
1. ✅ **Enemy - Ground** kesişimi
2. ✅ **Enemy - Wall** kesişimi
3. ✅ **Player - Ground** kesişimi
4. ✅ **Player - Wall** kesişimi

---

#### 🤔 Neden Bu Ayarlar?

**Enemy Collider Normal (isTrigger = false):**
```csharp
CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
col.isTrigger = false; // NORMAL collider
```

**Ama Physics Matrix ile "Player'a Trigger Gibi Davran":**

| Collision | Matrix Ayarı | Sonuç | Açıklama |
|-----------|-------------|-------|----------|
| **Enemy ↔ Wall** | ✅ Açık | Çarpışır | Duvarlardan geçemez ✅ |
| **Enemy ↔ Ground** | ✅ Açık | Çarpışır | Zeminde kalır ✅ |
| **Enemy ↔ Player** | ❌ Kapalı | Çarpışmaz | Player'a binebilir (trigger gibi) ✅ |
| **Enemy ↔ Enemy** | ❌ Kapalı | Çarpışmaz | Birbirlerini itmiyor ✅ |

**Sonuç:**
- ✅ Normal collider kullanıyoruz (duvarlarla çarpışmak için)
- ✅ Player ile collision kapalı (trigger gibi davranır)
- ✅ Attack range kod ile kontrol ediliyor (`Vector2.Distance`)

> **💡 Özet:**
> - Enemy collider **fiziksel** (trigger değil)
> - Duvarlarla çarpışıyor (Physics Matrix'te açık)
> - Ama Player layer'ı ile collision **kapalı**
> - Sonuç: Player'a trigger gibi davranır, duvarlara normal çarpışır!

---

## 🧪 Test Etme

### Play'e Basınca Beklenenler:

#### ☀️ Gündüz Başlangıcı (3s)
```
[TimeManager] ☀️ GÜNDÜZ BAŞLADI (3s)
GameManager: Day state
```
- Düşman yok
- Player hareket edebilir

#### 🌙 Gece Başlangıcı (15s)
```
[TimeManager] 🌙 GECE BAŞLADI (15s)
[SimpleEnemySpawner] 🌙 GECE BAŞLADI - Düşmanlar geliyor!
[SimpleEnemySpawner] Bu gece 4 düşman spawn olacak
```
- 2-5 düşman spawn olur
- Aralıklı spawn (0.5s → 1.5s)

#### ⚔️ Düşman Davranışı
```
[SimpleEnemyAI] ✅ Player BULUNDU: Player at (0.0, 0.0)
[SimpleEnemyAI] ⚔️ Player'a 10 hasar verildi!
```
- Player'ı bulur
- Player'a doğru yürür
- Yaklaşınca saldırır (0.5s aralıklarla)

#### ☀️ Gündüz Dönüşü
```
[SimpleEnemySpawner] ☀️ GÜNDÜZ BAŞLADI - Spawn durduruluyor
[SimpleEnemySpawner] 4 düşman deaktif edildi
```
- Tüm düşmanlar kaybolur
- Döngü tekrar başlar

---

## ❌ Sık Karşılaşılan Hatalar ve Çözümleri

| Hata Mesajı | Sebep | Çözüm |
|-------------|-------|-------|
| `[SimpleEnemyAI] ❌ PLAYER BULUNAMADI!` | Player tag'i yok | Player objesine `Player` tag'i ekle |
| `[SimpleEnemySpawner] Enemy Prefab atanmamış!` | Prefab sürüklenmemiş | Spawner Inspector'da Enemy Prefab'ı ata |
| `[SimpleEnemyAI] ❌ Player'da IDamageable component yok!` | Health.cs yok | Player'a `Health.cs` component ekle |
| Düşmanlar spawn olmuyor | Event bağlantısı kopuk | Spawner'ın OnEnable/OnDisable kontrol et |
| Player itiliyor | Rigidbody Dynamic | Player Rigidbody → **Kinematic** yap |
| **Düşmanlar duvardan geçiyor** ⚠️ | **Collider trigger** | **Enemy Collider → Is Trigger: false** |
| Düşmanlar birbirine çarpıyor | Physics collision açık | Physics 2D → Enemy-Enemy collision **KAPAT** |
| Gece başlamıyor | GameManager yok | Managers objesine GameManager.cs ekle |
| Düşmanlar hareket etmiyor | Linear Damping > 0 | Enemy Rigidbody → **Linear Damping: 0** |

### Debug Kontrol Listesi ✅

1. **Player Kontrol:**
   - [ ] Tag: Player
   - [ ] Layer: Player
   - [ ] Rigidbody2D: Kinematic
   - [ ] Health.cs component var

2. **Enemy Prefab Kontrol:**
   - [ ] Tag: Enemy
   - [ ] Layer: Enemy
   - [ ] Rigidbody2D: Dynamic
   - [ ] Collider: Is Trigger = **false** ⚠️ (Duvarlarla çarpışsın)
   - [ ] SimpleEnemyAI.cs var
   - [ ] EnemyHealth.cs var

3. **Spawner Kontrol:**
   - [ ] Enemy Prefab atanmış
   - [ ] Sahne içinde aktif
   - [ ] SimpleEnemySpawner.cs var

4. **Physics Kontrol:**
   - [ ] Player-Enemy collision kapalı
   - [ ] Enemy-Enemy collision kapalı

5. **Managers Kontrol:**
   - [ ] GameManager.cs var
   - [ ] TimeManager.cs var
   - [ ] Her ikisi de aktif

---

## 🎨 Özelleştirme

### Farklı Zorluk Seviyeleri

#### Kolay Level
```
[SimpleEnemyAI]
Move Speed: 3
Attack Damage: 5
Attack Cooldown: 1.0

[SimpleEnemySpawner]
Min Enemies: 2
Max Enemies: 5

[EnemyHealth]
Max Health: 50

[TimeManager]
Day Duration: 90
Night Duration: 30
```

#### Normal Level (Varsayılan)
```
[SimpleEnemyAI]
Move Speed: 4
Attack Damage: 10
Attack Cooldown: 0.5

[SimpleEnemySpawner]
Min Enemies: 5
Max Enemies: 15

[EnemyHealth]
Max Health: 100

[TimeManager]
Day Duration: 60
Night Duration: 45
```

#### Zor Level
```
[SimpleEnemyAI]
Move Speed: 5
Attack Damage: 20
Attack Cooldown: 0.3

[SimpleEnemySpawner]
Min Enemies: 10
Max Enemies: 25

[EnemyHealth]
Max Health: 150

[TimeManager]
Day Duration: 45
Night Duration: 60
```

#### Boss Wave
```
[SimpleEnemyAI]
Move Speed: 2
Attack Damage: 50
Attack Cooldown: 1.0

[SimpleEnemySpawner]
Min Enemies: 1
Max Enemies: 1
Min Spawn Interval: 0
Max Spawn Interval: 0

[EnemyHealth]
Max Health: 1000

[TimeManager]
Day Duration: 30
Night Duration: 120
```

---

## 📦 Hangi Sahnelere Eklenebilir?

### ✅ Eklenebilir:
- ✅ Ana oyun sahnesi (MainGame)
- ✅ Level sahneleri (Level1, Level2...)
- ✅ Test sahneleri (Test_Mehmet, Test_Baris...)
- ✅ Tutorial sahnesi
- ✅ Arena/Survival modu

### ❌ Eklenmemeli:
- ❌ Menu sahnesi
- ❌ Loading sahnesi
- ❌ Cutscene sahneleri
- ❌ Settings sahnesi

---

## 🔄 Farklı Düşman Türleri Oluşturma

### Hızlı Düşman (Runner)
```
GameObject: Runner_Enemy
Move Speed: 6
Attack Range: 1.0
Attack Damage: 5
Max Health: 50
Collider Radius: 0.3
Color: Sarı
```

### Tanklı Düşman (Tank)
```
GameObject: Tank_Enemy
Move Speed: 2
Attack Range: 1.5
Attack Damage: 25
Max Health: 300
Collider Radius: 0.6
Color: Kırmızı
```

### Normal Düşman (Spider) - Varsayılan
```
GameObject: Spider_Enemy
Move Speed: 4
Attack Range: 1.2
Attack Damage: 10
Max Health: 100
Collider Radius: 0.4
Color: Turuncu
```

> **💡 İpucu:** Birden fazla düşman tipi için:
> - Her biri için ayrı prefab oluştur
> - Ayrı spawner kullan veya Random.Range ile seç
> - Farklı renk/sprite kullan (tanınabilir olsun)

---

## 📊 Performans Optimizasyonu

### Object Pooling (Zaten Aktif ✅)
- Pool Size: 50 (varsayılan)
- Düşmanlar öldüğünde Destroy edilmez
- SetActive(false) ile deaktif olur
- Gece başında yeniden aktif edilir

### Pool Size Ayarı
```
2-5 düşman/gece  → Pool Size: 20
5-15 düşman/gece → Pool Size: 50  ✅ Varsayılan
15-30 düşman/gece → Pool Size: 100
30+ düşman/gece  → Pool Size: 150
```

> **⚠️ Uyarı:** Pool Size düşük olursa havuz otomatik genişler ama performans düşer.

### Debug Log Kapatma (Release için)
```csharp
// EnemyHealth.cs
Show Debug Logs: false

// SimpleEnemyAI.cs içinde debug logları kaldır
// Debug.Log(...) satırlarını yorum satırı yap
```

---

## 🎯 Sonraki Adımlar

### Sistem Çalışıyor mu? ✅

1. **Test Et:**
   - Tools → Enemy AI Test Setup
   - Play'e bas
   - Console'da loglara bak

2. **Sahneye Ekle:**
   - Mevcut sahneni aç
   - Manuel entegrasyon adımlarını takip et
   - Veya test sahnesinden copy-paste yap

3. **Özelleştir:**
   - Düşman sayısını ayarla
   - Gündüz/Gece sürelerini değiştir
   - Farklı düşman tipleri oluştur

---

## 📞 Sorun mu Var?

### Debug Checklist:
1. ✅ Console'da debug logları oku
2. ✅ Player tag'ini kontrol et
3. ✅ Enemy prefab atandı mı?
4. ✅ Physics collision matrix doğru mu?
5. ✅ Rigidbody ayarları doğru mu?

### Console'da Şunları Ara:
- `[SimpleEnemyAI]` - Düşman AI durumu
- `[SimpleEnemySpawner]` - Spawn durumu
- `[TimeManager]` - Gündüz/Gece geçişleri
- `[EnemyHealth]` - Can durumu

---

## 🏆 Başarıyla Kuruldu!

Artık Enemy AI sistemi herhangi bir sahneye entegre edilebilir durumda! 🎉

**Hızlı Başlangıç:**
1. Tools → Enemy AI Test Setup
2. 🚀 TEST SAHNESİNİ KUR
3. Play ▶️

**Manuel Kurulum:**
1. Managers + TimeManager
2. Player (Kinematic, Health.cs, "Player" tag)
3. Enemy Prefab (Dynamic, Trigger, SimpleEnemyAI + EnemyHealth)
4. EnemySpawner (Prefab ata)
5. Physics collision ayarla

İyi oyunlar! 🎮
