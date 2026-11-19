# Connectly

Connectly, aynı bilgisayarda çalışan bir TCP istemci-sunucu sohbet uygulamasıdır. Başlangıç ekranından **Sunucu** veya **İstemci** seçilir. Uygulamanın birden fazla örneği aynı anda açılabilir.

Fırat Üniversitesi Yazılım Mühendisliği 2. sınıf **YMH219 Nesne Tabanlı Programlama** dersi projesi.

[GitHub deposu](https://github.com/ugurmetinkarabulut/Connectly)

| Öğrenci bilgisi | Değer |
| --- | --- |
| Adı ve soyadı | … |
| Öğrenci numarası | … |

## Teknolojiler

- C# ve .NET 10 (`net10.0-windows`)
- Kontrolleri kodla oluşturulan Windows Forms
- TCP, UTF-8, `TcpListener`, `TcpClient` ve `async/await`
- Tek proje, harici NuGet paketi gerektirmez

## Özellikler

- **Sunucu.** Port seçimi, başlatma ve durdurma, durum, bağlı kullanıcı listesi, istemci sayısı ve zaman damgalı olaylar.
- **İstemci.** Kullanıcı adı ve IP ile bağlanma, bağlantıyı kesme, tekrar bağlanma ve Enter ya da **Gönder** ile mesaj iletme.
- **Sohbet.** Gönderen ve yerel saat bilgisi gösterilir. Kendi mesajların mavi renkte ve **Siz** etiketiyle görünür.
- **Doğrulama.** Port 1–65535 arasındadır. Kullanıcı adı en fazla 24, mesaj en fazla 500 karakter olabilir. Adlar büyük ve küçük harf ayrımı olmadan benzersizdir. Başta ve sondaki normal boşluklar temizlenir. Yalnızca boşluk içeren veya kontrol karakteri taşıyan metinler reddedilir.

## İstemci ve sunucu nasıl haberleşir?

Sunucu yalnızca `127.0.0.1` adresini, varsayılan olarak `5000` portunda dinler. İstemci bağlanınca kullanıcı adını gönderir. Sunucu adı kabul ederse istemci mesaj göndermeye başlar. Her mesajı sunucu zaman damgasıyla birlikte gönderen dahil bağlı tüm istemcilere iletir. Sohbet penceresine yalnızca sunucudan alınan yayın eklendiği için kendi mesajın da bir kez görünür.

UTF-8 protokolünde her komut bir satırdır. Alanları sekme (`TAB`), satırları satır sonu ayırır.

| Yön | Komut | Alanlar |
| --- | --- | --- |
| İstemci → sunucu | `HELLO` | Kullanıcı adı |
| Sunucu → istemci | `OK` | Bağlantı kabul edildi |
| Sunucu → istemci | `ERROR` | Hata metni |
| İstemci → sunucu | `MSG` | Mesaj metni |
| Sunucu → istemci | `CHAT` | ISO 8601 saat, kullanıcı adı, mesaj metni |
| Sunucu → istemci | `JOIN` / `LEAVE` | Katılan veya ayrılan kullanıcı adı |

## Windows'ta kurulum ve çalıştırma

1. .NET 10 destekleyen Windows 11 veya Windows 10 sürümü kullan. Microsoft, Windows 10 desteğini tabloda listelenen LTSC ve Enterprise sürümleriyle sınırlar. [Microsoft .NET Windows desteği](https://learn.microsoft.com/en-us/dotnet/core/install/windows#supported-versions)
2. Visual Studio 2026'yı **.NET masaüstü geliştirme** iş yüküyle kur veya Windows için .NET 10 SDK'yı yükle. [Microsoft Visual Studio ve .NET uyumluluğu](https://learn.microsoft.com/en-us/dotnet/core/install/windows#net-versions-and-visual-studio)
3. Git kuruluysa depoyu PowerShell'de klonla ve proje klasörüne gir.

   ```powershell
   git clone https://github.com/ugurmetinkarabulut/Connectly.git
   cd Connectly
   ```

4. Visual Studio'da [Connectly.sln](Connectly.sln) dosyasını aç. **Ctrl+F5** ile çalıştır. Terminal kullanıyorsan proje klasöründe şu komutları çalıştır.

   ```powershell
   dotnet restore Connectly.sln
   dotnet build Connectly.sln
   dotnet run --project Connectly.csproj --no-build
   ```

### Aynı bilgisayarda üç örnekle deneme

1. Uygulamanın ilk örneğinde **Sunucu** ekranını aç. Portu `5000` bırak ve **Sunucuyu Başlat** düğmesine bas. Durum göstergesi çalıştığını belirtmeli.
2. İlk örnek açıkken Windows'ta derlenen `bin\Debug\net10.0-windows\Connectly.exe` dosyasını iki kez daha çalıştır. Terminali tercih ediyorsan `dotnet run --project Connectly.csproj --no-build` komutunu iki ayrı terminalde çalıştır.
3. Her iki pencerede **İstemci** ekranını seç. Birinde **Ayşe**, diğerinde **Çağrı** adını kullan. İkisini de `127.0.0.1:5000` adresine bağla. Sunucudaki sayaç ve liste iki istemci göstermeli.
4. Ayşe'den `Merhaba Çağrı! çğıöşü 😊` mesajını Enter ile gönder. Çağrı'dan **Gönder** düğmesiyle yanıt ver. Her mesaj iki pencerede de bir kez, gönderen ve saat bilgisiyle görünmeli.
5. Bir istemcinin bağlantısını kes ve aynı adla tekrar bağlan. Ardından sunucuyu durdur. Sayaç sıfıra inmeli, iki istemci de bağlantı kaybını göstermeli. Sunucuyu tekrar başlatıp istemcileri yeniden bağla.

Ağ testlerinin sonuçları ve Windows doğrulama adımları [TEST_CHECKLIST.md](TEST_CHECKLIST.md) dosyasındadır. Sınıflar ve C# kavramları [KOD_ACIKLAMALARI.md](KOD_ACIKLAMALARI.md) dosyasında açıklanır.

## Sınırlamalar

Connectly eğitim amaçlıdır. Mesajlar açık metin olarak taşınır. Şifreleme ve kimlik doğrulama yoktur. Sunucu yalnızca aynı bilgisayardan bağlantı kabul eder. Mesajlar diske kaydedilmez ve sonradan bağlananlara sohbet geçmişi iletilmez. Dosya, ses ve görüntü aktarımı yoktur.

## Ekran görüntüleri

### Sunucu paneli

Windows testi sırasında iki bağlı istemciyi ve olay listesini gösteren gerçek ekran görüntüsü buraya eklenecek.

### İstemci sohbeti

Windows testi sırasında Türkçe bir mesajı, göndereni ve saat bilgisini gösteren gerçek ekran görüntüsü buraya eklenecek.
