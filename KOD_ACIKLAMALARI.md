# Connectly kod açıklamaları

| Dosya | Görevi |
| --- | --- |
| [Program.cs](Program.cs) | Windows Forms'u hazırlar ve başlangıç ekranını açar. |
| [MainForm.cs](MainForm.cs) | Sunucu veya istemci ekranını seçtirir. |
| [ServerForm.cs](ServerForm.cs) | Sunucuyu yönetir, kullanıcıları ve olayları gösterir. |
| [ClientForm.cs](ClientForm.cs) | Bağlantıyı yönetir ve sohbeti görüntüler. |
| [ChatServer.cs](ChatServer.cs) | Bağlantıları kabul eder, kullanıcıları takip eder ve mesajları yayınlar. |
| [ChatClient.cs](ChatClient.cs) | Sunucuya bağlanır, mesaj gönderir ve ağdan gelen satırları çözümler. |
| [ChatMessage.cs](ChatMessage.cs) | Göndereni, mesaj metnini ve zamanı bir arada tutar. |
| [ChatConnection.cs](ChatConnection.cs) | Soketi ve UTF-8 okuyucu/yazıcıyı yönetir, gelen veriyi satırlara böler. |
| [ChatProtocol.cs](ChatProtocol.cs) | Komut adlarını, uzunluk sınırlarını ve doğrulama kurallarını tutar. |
| [Ui.cs](Ui.cs) | Ortak arayüz renklerini ve kontrol ayarlarını içerir. |

## 1. TCP nedir?

TCP, iki uygulama arasında sıralı bir bayt akışı sağlar. Bağlantı açıkken baytlar gönderim sırasıyla okunur. Connectly, bir mesajın nerede bittiğini anlamak için satır sonu kullanır.

## 2. İstemci ve sunucu arasındaki fark nedir?

`ChatServer` bir adreste bağlantı bekler. `ChatClient` o adrese bağlanma isteği gönderir. Bağlantı açıldıktan sonra iki taraf da veri yazabilir ve okuyabilir.

## 3. IP adresi ve port nedir?

IP adresi ağdaki adresi belirtir. Port, o adreste hangi uygulamaya ulaşılacağını belirtir. Örneğin `127.0.0.1:5000` aynı bilgisayardaki 5000 numaralı portu gösterir. `ChatProtocol.IsValidPort`, değerin 1–65535 aralığında olup olmadığını denetler.

## 4. Localhost ve 127.0.0.1 nedir?

`localhost` kendi bilgisayarını ifade eden bir ana bilgisayar adıdır. `127.0.0.1` IPv4 yerel dönüş adresidir. `ChatServer.Start`, dinleyiciyi `IPAddress.Loopback` ile bu adrese bağlar.

## 5. TcpListener nasıl çalışır?

`ChatServer.Start`, `TcpListener.Start` ile portu açar. `ListenAsync`, `AcceptTcpClientAsync` ile bağlantı gelmesini bekler. Gelen her bağlantı için ayrı bir `HandleClientAsync` görevi izlenir.

## 6. TcpClient nasıl çalışır?

`ChatClient.OpenConnectionAsync`, `TcpClient.ConnectAsync` ile IP ve porta bağlanır. Ardından `HELLO` satırıyla kullanıcı adını gönderir. Sunucudan `OK` gelirse sohbet başlar.

## 7. NetworkStream nedir?

`NetworkStream`, açık TCP bağlantısında veri okumaya ve yazmaya yarar. `ChatConnection`, bu akışı `TcpClient.GetStream()` ile alır.

## 8. StreamReader ve StreamWriter ne işe yarar?

`StreamWriter` yazıyı UTF-8 baytlarına, `StreamReader` gelen baytları karakterlere dönüştürür. `ChatConnection.ReadLineAsync`, karakterleri satır sonuna kadar toplar. Bir okumada sonraki satırın başlangıcı da gelirse okuyucu bu kısmı tamponda tutar. İzin verilen uzunluğu aşan bir satır bağlantının kapatılmasına yol açar.

## 9. async ve await ne işe yarar?

`async` bir metotta `await` kullanılmasına izin verir. `await`, tamamlanmamış bir ağ işleminin sonucunu beklerken arayüz iş parçacığını serbest bırakır. Örneğin `ClientForm.ConnectAsync` bağlanma sırasında pencerenin olay işlemeye devam etmesini sağlar.

## 10. Task nedir?

`Task`, tamamlanması beklenebilen bir işlemi temsil eder. Örneğin `ChatClient`, alıcı görevinin referansını `_receiveTask` alanında tutar. Bağlantıyı keserken bu görevin tamamlanmasını bekler.

## 11. Mesajlar istemcilere nasıl iletilir?

`ChatClient.SendAsync` metni `MSG` satırı olarak yollar. `ChatServer.HandleMessageAsync` kullanıcı adını ve sunucunun saatini ekler. `BroadcastAsync`, `CHAT` satırını gönderen dahil tüm kullanıcılara yollar. Sunucu yayınları `SemaphoreSlim` ile sıraya koyar, böylece aynı ağ akışına eşzamanlı iki yazma yapılmaz. Gönderen de kendi mesajını bu yayınla görüntüler.

Sunucu, bir alıcıya yazmayı en fazla 3 saniye bekler. İstemcinin gönderim süresi 6 saniyedir. Bu fark, yoğun trafikte sunucuya yanıt vermeyen alıcıyı çıkarıp gönderenin bekleyen verisini okumak için zaman tanır.

## 12. try-catch neden kullanılır?

Bağlantı kurulurken sunucu kapalı olabilir. Port başka bir işlem tarafından kullanılıyor olabilir. `ClientForm.ConnectAsync` ve `ServerForm.StartServerAsync`, bu durumdaki `SocketException` hatasını yakalayıp formda anlaşılır bir açıklama gösterir.

## 13. Invoke neden gerekebilir?

Windows Forms kontrolleri arayüz iş parçacığından güncellenir. Ağ olayları başka bir iş parçacığında oluşabilir. `Ui.Post`, `BeginInvoke` ile liste ve sohbet güncellemelerini formun iş parçacığına gönderir.

## 14. Bağlantılar nasıl kapatılır?

İstemci soketi kapatır. Alıcı, devam eden yazmanın bitmesini bekleyip okuyucu ve yazıcıyı bırakır. Sunucu iptal sinyali verir, dinleyiciyi durdurur ve henüz kullanıcı adı göndermemiş olanlar dahil bağlantı görevlerinin bitmesini bekler. Form kapanırken de ilgili temizleme beklenir.

## 15. Sınıfların sorumluluklarını neden ayırdık?

Form sınıfları düğmeleri ve ekrandaki bilgileri yönetir. `ChatServer` ve `ChatClient` TCP işlemlerini yürütüp formlara olay gönderir. Bu ayrım sayesinde aynı ağ kodu Windows Forms olmadan macOS'ta test edilebilir.

## 16. Projede constructor ve property nasıl kullanıldı?

Constructor, nesne oluşturulurken çalışan metottur. `MainForm()` başlangıç ekranını kurar. `ChatMessage` constructor'ı, `sender`, `text` ve `sentAt` değerlerini alıp `Sender`, `Text` ve `SentAt` property'lerine atar. Bu property'ler yalnızca `get` içerir, dolayısıyla oluşturulan mesajın bilgileri sonradan değiştirilemez.

## 17. C# kavramları kodda nerede kullanılıyor?

| Kavram | Connectly'deki örnek |
| --- | --- |
| Class ve object | `ChatClient` bir sınıftır. `ClientForm` içindeki `_client = new()` satırı bu sınıftan bir nesne oluşturur. Her istemcinin kendi bağlantısı vardır. |
| Constructor ve property | `new ChatMessage(sender, text, sentAt)` constructor'ı çalıştırır. Mesajın metni `Text` property'sinden okunur. |
| `internal` ve `sealed` | `ChatClient`, `internal` olduğu için aynı proje derlemesinde kullanılabilir. `sealed` olduğu için bu sınıftan yeni bir sınıf türetilemez. |
| `static` | `ChatProtocol.IsValidPort` doğrudan sınıf adıyla çağrılır. Bunun için bir `ChatProtocol` nesnesi oluşturulmaz. |
| `readonly` | `ChatServer._users` alanının gösterdiği sözlük sonradan değiştirilemez. Aynı sözlüğe kullanıcı eklenebilir veya sözlükten kullanıcı çıkarılabilir. |
| `IDisposable` ve `using` | `ChatConnection.Dispose` ağ kaynaklarını bırakır. `ChatServer.HandleClientAsync` içindeki `using var connection`, kapsamdan hata ile çıkılsa bile `Dispose` çağırır. |
| `Dictionary` | `ChatServer._users`, kullanıcı adını anahtar olarak tutar ve karşılığında bağlantıyı saklar. `StringComparer.OrdinalIgnoreCase` aynı adın farklı büyük ve küçük harflerle yeniden kaydedilmesini engeller. |
| `TcpListener`, `TcpClient`, `NetworkStream` | Sunucu dinleyiciyle bağlantı kabul eder, istemci `TcpClient` ile bağlanır. `ChatConnection` açık soketin akışına `GetStream()` ile ulaşır. |
| `StreamReader` ve `StreamWriter` | `ChatConnection` gelen baytları UTF-8 metin olarak okur ve giden satırları UTF-8 baytlarına çevirir. |
| `async`, `await` ve `Task` | `ChatServer.Start`, dinleme ve temizliğin tamamını temsil eden bir `Task` döndürür. `ServerForm.StartServerAsync` görevi beklerken arayüz olay işlemeye devam eder. |
| `SemaphoreSlim` | `_broadcastLock` aynı anda bir yayın işleminin ilerlemesini sağlar. `WaitAsync` sırası gelene kadar bekler, `finally` içindeki `Release` sırayı sonraki işleme verir. |
| `CancellationToken` | `ChatServer.StopAsync` iptal sinyali verir. Dinleme, okuma ve yazma işlemleri bu sinyali alarak beklemeyi bırakabilir. |
| `lock` | `ChatServer` kısa süren başlatma ve iptal kaynağı işlemlerinde `_lifecycleLock` kullanır. Kilit içinde `await` yapılmaz. |
| Event ve `Action` | `UsersChanged`, kullanıcı listesi değiştiğinde çağrılan olaydır. `Action<string[]>` abonenin bir dizi alıp değer döndürmeyeceğini belirtir. |
| `BeginInvoke` | `Ui.Post`, arka plandan gelen güncellemeyi formun iş parçacığına kuyruğa alır. İstemci formu güncellemeyi uygulamadan önce bağlantı sürümünü denetler. |
| `try-catch-finally` | `ClientForm.DisconnectAsync` bağlantıyı kesmeyi dener, hata olursa durum metnini günceller ve `finally` ile düğmeleri yeniden hesaplar. |

## 18. Sözlüde çıkabilecek beş kod değiştirme sorusu

1. **Varsayılan portu iki ekranda da 6000 yapmak için hangi satırı değiştirirsin?** `ChatProtocol.DefaultPort` değerini `6000` yap. İki form da başlangıç değerini bu sabitten alır. Uygulamayı yeniden açıp iki port alanını kontrol et.
2. **Kullanıcı adı en fazla 16 karakter olsun.** `ChatProtocol.MaxUserNameLength` değerini `16` yap. `ClientForm` ve `ChatClient` içindeki sınır yazan hata metinlerini de güncelle. Sunucu, istemci ve metin kutusu aynı sabiti kullanır.
3. **Sohbet saatinde saniye göstermemek için neyi değiştirirsin?** `ClientForm.ShowMessage` içindeki `HH:mm:ss` biçimini `HH:mm` yap. Ağdaki zaman damgası tam olarak iletilmeye devam eder.
4. **Sunucu ekranında en fazla 100 olay gösterilsin.** `ServerForm.AddActivity` içindeki `500` değerini `100` yap. 101. olay eklenmeden önce en eski satır silinir.
5. **Bağlantı isteği için 8 saniye beklensin.** `ChatClient.OpenConnectionAsync` içindeki `timeout.CancelAfter(TimeSpan.FromSeconds(5))` satırında `5` yerine `8` yaz. Bu değişiklik istemcinin bağlanma ve onay bekleme süresini etkiler. Sunucunun kullanıcı adı bekleme süresi `ChatServer.HandleClientAsync` içinde ayrıca tanımlıdır.
