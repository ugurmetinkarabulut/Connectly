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

## 12. try-catch neden kullanılır?

Bağlantı kurulurken sunucu kapalı olabilir. Port başka bir işlem tarafından kullanılıyor olabilir. `ClientForm.ConnectAsync` ve `ServerForm.StartServer`, bu durumdaki `SocketException` hatasını yakalayıp formda anlaşılır bir açıklama gösterir.

## 13. Invoke neden gerekebilir?

Windows Forms kontrolleri arayüz iş parçacığından güncellenir. Ağ olayları başka bir iş parçacığında oluşabilir. `Ui.Post`, `BeginInvoke` ile liste ve sohbet güncellemelerini formun iş parçacığına gönderir.

## 14. Bağlantılar nasıl kapatılır?

İstemci soketi kapatır, alıcı görevinin bitmesini bekler ve akışları bırakır. Sunucu iptal sinyali verir, dinleyiciyi durdurur ve henüz kullanıcı adı göndermemiş olanlar dahil bağlantı görevlerinin bitmesini bekler. `OnFormClosing` olayında da ilgili temizleme işlemine sıra verilir.

## 15. Sınıfların sorumluluklarını neden ayırdık?

Form sınıfları düğmeleri ve ekrandaki bilgileri yönetir. `ChatServer` ve `ChatClient` TCP işlemlerini yürütüp formlara olay gönderir. Bu ayrım sayesinde aynı ağ kodu Windows Forms olmadan macOS'ta test edilebilir.

## 16. Projede constructor ve property nasıl kullanıldı?

Constructor, nesne oluşturulurken çalışan metottur. `MainForm()` başlangıç ekranını kurar. `ChatMessage` constructor'ı, `sender`, `text` ve `sentAt` değerlerini alıp `Sender`, `Text` ve `SentAt` property'lerine atar. Bu property'ler yalnızca `get` içerir, dolayısıyla oluşturulan mesajın bilgileri sonradan değiştirilemez.
