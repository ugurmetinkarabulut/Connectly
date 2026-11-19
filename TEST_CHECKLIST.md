# Connectly test listesi

## macOS'ta doğrulananlar

.NET 10 SDK ile Windows hedefi için `dotnet restore Connectly.sln` ve `dotnet build Connectly.sln --no-restore` tamamlandı. İlk çalıştırmada NuGet güvenlik açığı verilerine erişimle ilgili `NU1900` uyarısı alındı. NuGet denetimi yalnızca tekrar çalıştırılan restore komutunda kapatıldı. Bu restore sonrasında Debug ve Release derlemeleri 0 uyarı ve 0 hatayla tamamlandı. Projenin harici NuGet paket bağımlılığı yoktur.

Geçici bir `net10.0` konsol projesi, Connectly'nin gerçek ağ kaynak dosyalarını doğrudan derledi. Aşağıdaki kontroller son kaynak koduyla üç çalıştırmada da geçti. Testler `127.0.0.1` üzerinde işletim sisteminin seçtiği boş portlarla ve zaman sınırlarıyla çalıştı.

| Senaryo | Sonuç |
| --- | --- |
| Sunucunun yerel adreste başlaması ve aynı porttaki ikinci sunucunun reddedilmesi | Geçti |
| İlk ve ikinci istemcinin bağlanması, kullanıcı listesinin ve katılım bildiriminin güncellenmesi | Geçti |
| Geçersiz ve yinelenen kullanıcı adlarının reddedilmesi | Geçti |
| İki yönlü ve eşzamanlı mesajlaşma, aynı yayın sırası, zaman damgası ve kendi mesajı bilgisi, tekrar olmaması | Geçti |
| Türkçe karakter, emoji ve 500 karakterlik mesaj | Geçti |
| Parçalanmış UTF-8 karakteri, tek yazmada gelen birden fazla satır ve CRLF | Geçti |
| Bağlantısız gönderme ile boş, uzun veya çok satırlı metnin reddedilmesi | Geçti |
| Ağdan gelen boş ve fazla uzun mesajın reddedilmesi, sınırı aşan bitmemiş satırın bağlantıyı kapatması | Geçti |
| Geçersiz port ve sunucuya erişilemediğinde bağlantının kapalı kalması | Geçti |
| Bağlantıyı kesme, listeden çıkma, ayrılma bildirimi ve aynı adla yeniden bağlanıp mesaj gönderme | Geçti |
| Durdurma, bağlı ve el sıkışması bekleyen soketlerin kapanması, portun serbest kalması ve aynı portta yeniden başlatma | Geçti |
| Bekleyen istemci bağlantısı iptal edildiğinde soketin kapanması | Geçti |

## Windows'ta yapılacaklar

Windows Forms arayüzü ve aşağıdaki senaryolar fiziksel Windows bilgisayarında doğrulanacak.

- [ ] `Connectly.sln` dosyasını Visual Studio'da aç, Debug derlemesi al ve uygulamayı çalıştır.
- [ ] Başlangıç ekranındaki iki rolü de aç ve rol pencerelerini kapatarak başlangıç ekranına dön.
- [ ] Üç örnek aç. Bir sunucuyu `127.0.0.1:5000` üzerinde başlat ve iki farklı kullanıcı adıyla bağlan.
- [ ] Sunucu durumunun, istemci sayısının, kullanıcı listesinin ve olayların doğru güncellendiğini gözlemle.
- [ ] İki yönde Türkçe karakterli mesaj gönder. Birini Enter, diğerini **Gönder** düğmesiyle yolla. Saatleri, gönderenleri ve **Siz** etiketini doğrula. Her mesaj her pencerede bir kez görünmeli.
- [ ] Boş mesajda ve bağlantı yokken gönderimin devre dışı kaldığını doğrula.
- [ ] Aynı adı farklı büyük ve küçük harflerle tekrar kullan. İkinci bağlantıda Türkçe hata mesajını gözlemle.
- [ ] Hatalı IP, geçersiz port, sunucu kapalı ve port kullanımda durumlarında hata mesajlarını kontrol et.
- [ ] Bir istemcinin bağlantısını kes. Sayaç, liste ve diğer istemcideki ayrılma bildirimini kontrol et. Aynı adla yeniden bağlan.
- [ ] İki istemci açıkken sunucuyu durdur. İstemcilerin bağlantı kaybını gösterdiğini ve formun yanıt verdiğini doğrula. Aynı portta sunucuyu tekrar başlat ve iki istemciyi yeniden bağla.
- [ ] Bağlı istemci penceresini ve sonra sunucu penceresini pencerenin kapatma düğmesiyle kapat. Aktif bağlantıların temizlendiğini ve portun tekrar kullanılabildiğini doğrula.
- [ ] Pencereleri küçültüp büyüt. %100 ve %150 ekran ölçeğinde düğmeleri, alanları, listeleri ve kaydırmayı kontrol et.
- [ ] Sunucu panelinin ve istemci sohbetinin gerçek ekran görüntülerini al ve README'deki bölümlere ekle.
