# Game Over UI Kurulum Rehberi

Bu dosya, Game Over ekranını Unity sahnesine nasıl ekleyeceğinizi açıklar.

## Adım 1: Canvas Oluştur

1. Hierarchy'de **Right Click** → **UI** → **Canvas**
2. Canvas'ı seç ve Inspector'da:
   - **Render Mode**: Screen Space - Overlay
   - **Canvas Scaler** → **UI Scale Mode**: Scale With Screen Size
   - **Reference Resolution**: 1920x1080 (veya tercih edilen çözünürlük)

## Adım 2: Game Over Panel Oluştur

1. Canvas'ın altında **Right Click** → **UI** → **Panel**
2. Adını **"GameOverPanel"** olarak değiştir
3. Inspector'da:
   - **Rect Transform**:
     - **Anchors**: Stretch (sol üst köşedeki preset'ten "stretch all" seç)
     - **Left, Top, Right, Bottom**: Hepsi 0
   - **Image** component:
     - **Color**: Siyah, Alpha: 180 (yarı saydam siyah arka plan)

## Adım 3: Game Over Başlığı (Text)

1. GameOverPanel'in altında **Right Click** → **UI** → **Text - TextMeshPro**
   - İlk kez kullanıyorsan "Import TMP Essentials" butonuna tıkla
2. Adını **"GameOverText"** olarak değiştir
3. Inspector'da:
   - **Rect Transform**:
     - **Pos Y**: 100
     - **Width**: 600
     - **Height**: 100
   - **TextMeshProUGUI**:
     - **Text**: "GAME OVER"
     - **Font Size**: 80
     - **Alignment**: Center & Middle
     - **Color**: Kırmızı (veya tercih edilen renk)

## Adım 4: Restart Butonu

1. GameOverPanel'in altında **Right Click** → **UI** → **Button - TextMeshPro**
2. Adını **"RestartButton"** olarak değiştir
3. Inspector'da:
   - **Rect Transform**:
     - **Pos Y**: -50
     - **Width**: 300
     - **Height**: 80
   - **Image** component:
     - **Color**: Yeşil (veya tercih edilen renk)
4. Button'un child'ı olan **Text (TMP)** objesini seç:
   - **Text**: "RESTART"
   - **Font Size**: 36
   - **Alignment**: Center & Middle
   - **Color**: Beyaz

## Adım 5: GameOverUI Script Ekle

1. Canvas objesini seç
2. Inspector'da **Add Component** → "GameOverUI" ara ve ekle
3. Script ayarları:
   - **Game Over Panel**: GameOverPanel objesini sürükle
   - **Restart Button**: RestartButton objesini sürükle
   - **Game Over Text**: GameOverText objesini sürükle (opsiyonel)
   - **Fade In Duration**: 0.5 (animasyon süresi)
   - **Use Scale Animation**: ✅ (check)
   - **Show Debug Logs**: ✅ (test için)

## Adım 6: PlayerHealth Ayarları

1. Player GameObject'ini seç
2. Inspector'da **PlayerHealth** component'ine bak
3. Ayarlar:
   - **Use Game Over UI**: ✅ (check)
   - **Game Over UI**: Canvas'taki GameOverUI'ı sürükle (otomatik bulunabilir)
   - **Auto Restart If No UI**: İsteğe bağlı
   - **Auto Restart Delay**: 2.0

## Test Etme

1. Play butonuna bas
2. Player'ı öldür (düşmana hasar aldır)
3. Game Over ekranı görünmeli
4. "RESTART" butonuna tıkla
5. Sahne yeniden başlamalı

## Özelleştirme İpuçları

### Arka Plan Blur Efekti (Opsiyonel)
GameOverPanel'e blur efekti eklemek için:
1. Panel Image'a **Material** ekle
2. Unity'de "UI-Default-Blur" gibi bir UI blur shader'ı kullan

### Ek Bilgiler Gösterme
GameOverPanel'e şunlar eklenebilir:
- Öldüğün wave sayısı
- Toplanan altın miktarı
- Öldürülen düşman sayısı
- High score

### Buton Hover Efekti
RestartButton Inspector → Button component:
- **Transition**: Color Tint
- **Highlighted Color**: Daha açık yeşil
- **Pressed Color**: Koyu yeşil

### Animasyon Özelleştirme
GameOverUI script Inspector:
- **Fade In Duration**: Daha hızlı animasyon için azalt (örn: 0.3)
- **Use Scale Animation**: Kapat = sadece fade, Aç = bounce effect

## Sorun Giderme

**Problem**: Game Over ekranı görünmüyor
- Canvas'ın Render Mode'u "Screen Space - Overlay" olmalı
- GameOverPanel başlangıçta gizli olmalı (script otomatik gizler)
- Console'da hata mesajlarını kontrol et

**Problem**: Restart butonu çalışmıyor
- Button'a script'in listener'ını eklediğinden emin ol (otomatik)
- Console'da "OnRestartButtonClicked" debug mesajını ara

**Problem**: Oyun donuyor
- GameOverUI script `Time.timeScale = 0` yapıyor (kasıtlı)
- Restart'a basınca `Time.timeScale = 1` olacak

## İleri Seviye: Ek Butonlar

### Main Menu Butonu Eklemek:
1. RestartButton'u kopyala, adını "MainMenuButton" yap
2. Pos Y = -150 yap
3. Text = "MAIN MENU"
4. GameOverUI.cs'ye şu metodu ekle:

```csharp
public void LoadMainMenu()
{
    Time.timeScale = 1f;
    SceneManager.LoadScene("MainMenu"); // Ana menü sahnenin adı
}
```

5. Button'un OnClick event'ine bu metodu ekle (Inspector'dan)
