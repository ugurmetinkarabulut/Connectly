# Connectly test listesi

## macOS'ta doğrulananlar

.NET 10 SDK ile Windows hedefi macOS'ta derlendi. Kısıtlı ağ erişimiyle yapılan ilk restore, nuget.org hizmet dizinine ulaşamadığı için `NU1900` uyarısı verdi. Ağ erişimiyle ikinci restore güvenlik denetimi açıkken tamamlandı. NuGet güvenlik açığı verilerini başarıyla aldı ve uyarı üretmedi. Ardından Debug ve Release derlemeleri 0 uyarı ve 0 hatayla tamamlandı.

Geçici bir `net10.0` konsol projesi, Connectly'nin gerçek ağ kaynak dosyalarını doğrudan derledi. Testler `127.0.0.1` üzerinde, işletim sisteminin seçtiği boş portlarla ve süre sınırlarıyla çalıştı.

| Senaryo | Sonuç |
| --- | --- |
| Sunucunun yerel adreste başlaması ve aynı porttaki ikinci sunucunun reddedilmesi | Geçti |
| Üç istemcinin eşzamanlı bağlanması, kullanıcı listesi ve katılım bildirimi | Geçti |
| Boş veya yalnızca boşluklu kullanıcı adlarının, geçersiz karakterlerin ve yinelenen adların reddedilmesi | Geçti |
| Üç istemciden toplam 90 mesaj, aynı yayın sırası ve zaman damgası, kendi mesajının işaretlenmesi, tek kopya | Geçti |
| Türkçe karakter, emoji ve 500 karakterlik mesaj | Geçti |
| Parçalanmış UTF-8 karakteri, tek yazmada gelen birden fazla satır ve CRLF | Geçti |
| Bağlantısız gönderme ile boş, uzun veya çok satırlı metnin reddedilmesi | Geçti |
| Ağdan gelen boş, yalnızca boşluklu, kontrol karakterli veya fazla uzun mesajın reddedilmesi, sınırı aşan bitmemiş satırın bağlantıyı kapatması | Geçti |
| İstemcinin boş `CHAT` alanlarını ve boş `JOIN`, `LEAVE`, `ERROR` bildirimlerini yok sayması | Geçti |
| Geçersiz port ve sunucuya erişilemediğinde bağlantının kapalı kalması | Geçti |
| Bağlantıyı kesme, ayrılma bildirimi, aynı adla yeniden bağlanıp mesaj gönderme ve eski kapanış olayının önceki bağlantı sürümünü taşıması | Geçti |
| Ani TCP kapanışında ayrılma bildirimi ve diğer istemcilerde mesajlaşmanın sürmesi | Geçti |
| Bekleyen bağlantının hem iptal hem de zaman aşımında soketini kapatması | Geçti |
| Yazma beklerken istemcinin bağlantısını kesmesi ve kuyruktaki gönderimlerin sonlanması | Geçti |
| Okumayan istemcinin zaman aşımıyla çıkarılması ve kalan istemcilerin mesajlaşmaya devam etmesi | Geçti |
| Mesaj trafiği sırasında durdurma, bağlı ve bekleyen soketlerin kapanması, listenin temizlenmesi ve portun serbest kalması | Geçti |
| Aynı portta altı kez başlatma ve durdurma | Geçti |
| Bir istemci görevi hata verdiğinde sunucu görevinin hatayı bildirmesi, soket ve liste temizliği, aynı portta tekrar başlatma | Geçti |

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
- [ ] Sunucu kapanırken **Sunucuyu Başlat** düğmesinin kapalı kaldığını ve temizlik bitince yeniden etkinleştiğini doğrula.
- [ ] Hızlı gönderim sırasında bağlantıyı kes ve yeniden bağlan. Eski kapanış bildiriminin yeni bağlantı durumunu değiştirmediğini gözlemle.
- [ ] Yanıt vermeyen bir IP'ye bağlanmayı dene. Pencere beklerken yanıt vermeye devam etmeli. Denemeyi tekrar başlatıp istemci penceresini kapat ve başlangıç ekranına dönebildiğini doğrula.
- [ ] Bağlı istemci penceresini ve sonra sunucu penceresini pencerenin kapatma düğmesiyle kapat. Aktif bağlantıların temizlendiğini ve portun tekrar kullanılabildiğini doğrula.
- [ ] Pencereleri küçültüp büyüt. %100 ve %150 ekran ölçeğinde düğmeleri, alanları, listeleri ve kaydırmayı kontrol et.
- [ ] Sunucu panelinin ve istemci sohbetinin gerçek ekran görüntülerini al ve README'deki bölümlere ekle.
