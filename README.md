<img width="963" height="454" alt="image" src="https://github.com/user-attachments/assets/3689394e-784f-4051-a977-dbd2143eb905" />

# EĞİTİM MASKOTU


egitimmaskotu-bqgnbcg9h8adhxbz.westeurope-01.azurewebsites.net | [YouTube Link]

---

## Projenin Hikayesi ve Özgünlüğü

Eğitim Maskotu fikri, standart ve tekdüze “herkese aynı” eğitim anlayışının, öğrencilerde nasıl bir motivasyon kaybına yol açabileceğini fark etmemle doğdu. İnandım ki, öğrenme süreci kişisel olmalı; her birey kendi hızında, kendi sorularıyla, kendi ilgisine göre yol almalı. Yapay zeka ise bu kişisel rehberliği mümkün kılan en güçlü araçlardan biri.Bu platformu yalnızca bilgi sunan değil, bilgiyi sorgulatan bir sistem olarak tasarladım. Sokratik yöntemden ilhamla, kullanıcıyı pasif bir dinleyici olmaktan çıkarıp, aktif bir tartışmacıya dönüştürmeyi hedefledim. Çünkü gerçek öğrenme, bir bilginin doğruluğundan emin olmakla değil, o bilgiyi savunabilmekle başlar.

Amacım, öğrenmeyi daha etkileşimli, daha kişisel ve en önemlisi daha keyifli hale getirecek bir "yol arkadaşı" yaratmaktı. Umarım bu yolculukta size de faydalı olur!

---

##  Proje Özeti ve Faydaları

### Proje temel olarak 3 ana aşamadan oluşmaktadır. Bunlar Sırasıyla:
<br/>
<img width="1228" height="638" alt="Ekran görüntüsü 2025-07-31 010045" src="https://github.com/user-attachments/assets/e955bffe-2f12-43ef-b89e-0ed01a963a0e" />

### 1.  **Kişiselleştirilmiş Yol Haritası**  
Kullanıcıdan önce konu başlığı, mevcut yetkinlik düzeyi, öğrenme hedefi ve varsa özel ilgi alanları bir form aracılığıyla alınır. Eğitim Maskotu, bu veriler doğrultusunda seçilen konuya uygun modüller ve her modüle ait kaynakları (detaylı açıklamalar, videolar, makaleler, interaktif testler) içeren sıralı bir öğrenme planı sunar.

Her modülün tamamlanmasının ardından, kullanıcı o modüle özel çoktan seçmeli testlere erişim hakkı kazanır ve dilediği kadar farklı test oluşturabilir. Testlerin sonunda sistem, her bir şıkkın neden doğru ya da yanlış olduğunu ayrıntılı biçimde açıklar.

Kullanıcı, **İlerleme Raporu** sayfası üzerinden; bitirdiği modülleri, testlerdeki başarı durumunu (doğru/yanlış sayıları), test tamamlama süresini ve yol haritasında geçirdiği toplam süreyi takip edebilir.

> **Kullanılan API'ler:**  
> - Modül içerikleri ve test sorularının oluşturulmasında: **Gemini API**  
> - Video kaynakları için: **YouTube Data API v3**  
> - Makaleler için: **CORE API**

---
<img width="1112" height="630" alt="Ekran görüntüsü 2025-07-31 010057" src="https://github.com/user-attachments/assets/e870203f-1576-40e1-9839-4a97019344c5" />

### 2.  **Sokratik Soru–Cevap Oturumu**  
Bu adımda Eğitim Maskotu, kullanıcı için sanal bir öğretmen deneyimi sunar. Seçilen konudaki anlayışı derinleştirmek amacıyla yapay zekâ temelli öğretmen, kullanıcıya adım adım sorular yöneltir, kavramsal hataları belirler ve anlık geri bildirimlerle düzeltmeler sağlar.

**Sokratik yöntem** esas alınarak geliştirilen bu yapı, kullanıcının hazır cevaplar yerine konuyu gerçekten anlamasını ve kendi düşünsel süreciyle sonuca ulaşmasını hedefler. Öğrenci konuyu bilmiyorsa, öğretmen ilgili noktaları sadeleştirerek açıklar ve eksik bilgileri tamamlamaya çalışır. Böylece klasik bir sınavdan ziyade rehberli, öğretici bir etkileşim deneyimi oluşur.

> **Kullanılan API:**  
> - Sanal öğretmen: **Gemini API**

---
<img width="1176" height="669" alt="Ekran görüntüsü 2025-07-31 010106" src="https://github.com/user-attachments/assets/ae4e37b1-8254-408c-8af3-5eee1d093338" />

### 3.  **Münazara Simülasyonu**  
Bu aşamada kullanıcı, öğrendiği bilgiler ışığında kendi argümanlarını geliştirerek yapay zekâ destekli bir tartışma ortamına katılır. Sanal rakip ile yapılan bu münazarada kullanıcı; düşüncelerini savunur, karşıt görüşleri değerlendirir ve stratejik biçimde yanıtlar üretir.

Yapay zekâ, argümanları **mantıksal tutarlılık**, **ifade yetkinliği** ve **içerik doğruluğu** açısından değerlendirir. Tartışma sonucunda, tarafsız bir **sanal hakem** kazananı belirler ve kullanıcıya hem **100 üzerinden bir puan** hem de **gelişim odaklı bir analiz raporu** sunar.

Bu süreç, yalnızca bilgilerin tekrar edilmesini değil; aynı zamanda **eleştirel düşünme**, **argüman oluşturma** ve **iletişim becerilerinin** gelişmesini de hedefler.

> **Kullanılan API'ler:**  
> - Münazara rakibi ve hakemi: **Gemini API**


---

#### Bu üç aşamalı yapı, kullanıcıya hem bilgiyi öğrenme hem de yorumlama fırsatı sunar. Teorik içerikler, rehberli sorular ve tartışma simülasyonlarıyla desteklenir. Böylece öğrenme süreci daha dikkatli düşünmeye teşvik eder ve kullanıcı kendi gelişimini daha net şekilde takip edebilir.

## Kullanılan Teknolojiler

- **Backend:** .NET 8, ASP.NET Core MVC
- **Frontend:** HTML, CSS, JavaScript, Bootstrap
- **API’ler:** Google Gemini, YouTube Data API, Core API
- **CI/CD:** GitHub Actions
- **Barındırma:** Azure App Service

---

## Projenin Mimarisi

```plaintext
Kullanıcı (Tarayıcı)
  ↔ Azure App Service (ASP.NET Core MVC)
    ├─ Controllers
    ├─ Views
    ├─ Services (GeminiApiService, AIAgentContentService [YouTubeService ve CoreApiService])
    └─ In-Memory Session

Dış Sistemler: Google Gemini, YouTube API, Core API
```
---

## Projenin Geleceği (Geliştirme Fırsatları)

Eğitim Maskotu’nun mevcut hali güçlü bir temel sunarken, proje birçok yönden geliştirilmeye açıktır.
Kullanıcı profilleri, öğrenme geçmişi gibi özelliklerin eklenmesiyle, kullanıcı deneyimi farklı bir seviyeye çıkarılabilir. 
Bu yapıya ek olarak, sesli konuşma özelliği gibi yeni etkileşim yöntemleriyle kullanıcıların sistemle daha doğal şekilde iletişim kurması sağlanabilir.
Eğitim kurumlarıyla entegre edilerek öğretmenlerin sınıf içinde de bu sistemi kullanabilmesi hedeflenebilir. Böylece Eğitim Maskotu, sadece bireysel değil, toplu öğrenme ortamlarında da etkili bir araç hâline gelir.

---

## Dağıtım ve Sunucu Bilgisi

Proje, GitHub üzerinden Azure Web App’e otomatik olarak dağıtılmaktadır. Dağıtım süreci GitHub Actions ile yönetilir.

Sunucu tarafında veritabanı yerine bellek içi (in-memory) session kullanılır.

API anahtarları güvenlik için Azure portal üzerindeki uygulama ayarlarında tutulur, böylece kaynak kod içinde yer almaz.

Uygulama .NET 8 destekli, Linux tabanlı bir Azure App Service üzerinde çalışmaktadır.

---

##  Katılım Bilgisi

Bu proje, **BTK Akademi Hackathon'2025** yarışması kapsamında geliştirilmiştir. Tüm içerik, fikir ve kaynak kodlar yarışma amacıyla oluşturulmuş olup telif hakları proje sahibine aittir.

##  Lisans Bilgisi

Bu projedeki kaynak kodların ve içeriklerin izinsiz kullanımı, kopyalanması veya çoğaltılması yasaktır. Proje yalnızca inceleme ve değerlendirme amaçlı paylaşılmıştır. Her hakkı saklıdır.

