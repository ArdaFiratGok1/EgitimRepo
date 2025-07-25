using EgitimMaskotuApp.Models;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EgitimMaskotuApp.Services
{
    public class GeminiApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public GeminiApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _apiKey = _configuration["GeminiApi:ApiKey"];
            _baseUrl = _configuration["GeminiApi:BaseUrl"];
        }

        private async Task<string> SendRequestAsync(string prompt)
        {
            try
            {
                var request = new GeminiRequest
                {
                    contents = new List<GeminiContent>
                    {
                        new GeminiContent
                        {
                            parts = new List<GeminiPart>
                            {
                                new GeminiPart { text = prompt }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_baseUrl}?key={_apiKey}";
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson);

                    return geminiResponse?.candidates?.FirstOrDefault()?.content?.parts?.FirstOrDefault()?.text ?? "Yanıt alınamadı.";
                }
                else
                {
                    return "API çağrısında hata oluştu.";
                }
            }
            catch (Exception ex)
            {
                return $"Hata: {ex.Message}";
            }
        }

        // Münazara konusu analizi - İyileştirilmiş parsing
        public async Task<(string konu, string kullaniciTarafi, string aiTarafi)> AnalyzeMunazaraTopicAsync(string rawTopic)
        {
            var prompt = $@"
Kullanıcı şu münazara konusunu verdi: '{rawTopic}'

Lütfen bu konuyu analiz et ve MUTLAKA şu formatla yanıt ver:
KONU: [net ve anlaşılır münazara konusu]
KULLANICI_TARAFI: [kullanıcının savunacağı taraf]
AI_TARAFI: [AI'ın savunacağı karşıt taraf]

ÖNEMLI: Her satır yukarıdaki etiketlerle başlamalı. Ek açıklama yapma, sadece bu formatı kullan.

Konuyu Türkçe eğitim seviyesine uygun hale getir ve her iki taraf için de mantıklı argümanlar bulunabilecek şekilde düzenle.
Eğer kullanıcı tarafları belirtmişse onları kullan, belirtmemişse sen belirle.

Örnek:
KONU: Sosyal medyanın gençler üzerindeki etkisi
KULLANICI_TARAFI: Sosyal medya gençlere faydalıdır
AI_TARAFI: Sosyal medya gençlere zararlıdır
";

            var response = await SendRequestAsync(prompt);

            // İyileştirilmiş parsing
            var konu = ExtractValueImproved(response, "KONU:");
            var kullaniciTarafi = ExtractValueImproved(response, "KULLANICI_TARAFI:");
            var aiTarafi = ExtractValueImproved(response, "AI_TARAFI:");

            // Eğer parsing başarısız olursa varsayılan değerler
            if (string.IsNullOrWhiteSpace(konu))
                konu = rawTopic;
            if (string.IsNullOrWhiteSpace(kullaniciTarafi))
                kullaniciTarafi = "Destekleyici taraf";
            if (string.IsNullOrWhiteSpace(aiTarafi))
                aiTarafi = "Karşı taraf";

            return (konu, kullaniciTarafi, aiTarafi);
        }

        // Münazara yanıtı
        public async Task<string> GenerateMunazaraResponseAsync(MunazaraSession session, string userMessage)
        {
            var conversationHistory = string.Join("\n", session.Messages.Select(m =>
                $"{(m.Speaker == "user" ? "Kullanıcı" : "AI")}: {m.Content}"));

            var prompt = $@"
Sen bir münazara botusun ve '{session.AiTarafi}' tarafını savunuyorsun.
Münazara konusu: {session.Konu}
Sen savunduğun taraf: {session.AiTarafi}
Kullanıcının savunduğu taraf: {session.KullaniciTarafi}

Şimdiye kadarki konuşma:
{conversationHistory}

Kullanıcının son mesajı: {userMessage}

Lütfen:
1. Kendi tarafını güçlü argümanlarla savun
2. Kullanıcının argümanlarına mantıklı karşı çıkışlar yap
3. Yapıcı ve eğitici bir ton kullan
4. Maksimum 3-4 cümle ile yanıt ver
5. Türkçe ve anlaşılır ol

Yanıtın:
";

            return await SendRequestAsync(prompt);
        }

        // Münazara sonucu değerlendirmesi - İyileştirilmiş parsing
        public async Task<MunazaraResult> EvaluateMunazaraAsync(MunazaraSession session)
        {
            var conversationHistory = string.Join("\n", session.Messages.Select(m =>
                $"{(m.Speaker == "user" ? "Kullanıcı" : "AI")}: {m.Content}"));

            var prompt = $@"
Şu münazarayı değerlendir:

Konu: {session.Konu}
Kullanıcı tarafı: {session.KullaniciTarafi}  
AI tarafı: {session.AiTarafi}

Konuşma:
{conversationHistory}

Lütfen MUTLAKA şu formatla değerlendirme yap:
KAZANAN: [Kullanıcı veya AI]
PUAN: [0-100 arası kullanıcı puanı]
İYİ_YANLAR: [kullanıcının güçlü argümanları, virgülle ayırarak]
KÖTÜ_YANLAR: [kullanıcının zayıf noktaları, virgülle ayırarak]
DETAYLI_ANALIZ: [kapsamlı değerlendirme]

ÖNEMLI: Her satır yukarıdaki etiketlerle başlamalı. Ek açıklama yapma.

DEĞERLENDİRME KRİTERLERİ VE PUANLAMA:
Değerlendirmeni 100 puan üzerinden aşağıdaki rubriğe göre yap:

1.  **Argüman Yapılandırması ve Gücü (30 Puan):**
    * Kullanıcının argümanları net bir ""İddia"", onu destekleyen ""Veri/Kanıt"" ve ikisi arasındaki mantıksal ""Gerekçe"" üçlüsünü içeriyor mu? Argümanlar ne kadar güçlü ve iyi yapılandırılmış?

2.  **Çürütme ve Etkileşim (25 Puan):**
    * Kullanıcı, yapay zekanın argümanlarına doğrudan yanıt veriyor mu? Karşı argümanları doğru bir şekilde özetleyip, mantık ve kanıtla etkili bir şekilde çürütüyor mu? Fikir çatışması yaratabiliyor mu?

3.  **Kanıt Kalitesi ve Bilgi Doğruluğu (20 Puan):**
    * Kullanıcının sunduğu kanıtlar (veriler, örnekler) ne kadar güvenilir, ilgili ve doğru? Bilgiyi argümanını desteklemek için doğru kullanıyor mu?

4.  **Mantıksal Tutarlılık ve Safsata Kontrolü (15 Puan):**
    * Kullanıcının argümanları kendi içinde tutarlı mı? Konuşmasının genelinde mantık hataları (ad hominem, korkuluk safsatası vb.) var mı?

5.  **Sunum ve İkna Edicilik (10 Puan):**
    * Kullanıcı kendini ne kadar açık, net ve yapılandırılmış bir dille ifade ediyor? Dil kullanımı mantıksal ikna gücünü artırıyor mu?

Örnek:
KAZANAN: Kullanıcı
PUAN: 75
İYİ_YANLAR: Güçlü argümanlar, mantıklı yaklaşım, iyi örnekler
KÖTÜ_YANLAR: Zayıf karşı çıkışlar, yetersiz kanıt
DETAYLI_ANALIZ: Kullanıcı konuyu iyi savundu...
";

            var response = await SendRequestAsync(prompt);

            var result = new MunazaraResult
            {
                Winner = ExtractValueImproved(response, "KAZANAN:"),
                UserScore = ExtractScoreImproved(response, "PUAN:"),
                GoodPoints = ExtractListImproved(response, "İYİ_YANLAR:"),
                BadPoints = ExtractListImproved(response, "KÖTÜ_YANLAR:"),
                DetailedAnalysis = ExtractValueImproved(response, "DETAYLI_ANALIZ:")
            };

            // Varsayılan değerler
            if (string.IsNullOrWhiteSpace(result.Winner))
                result.Winner = "Berabere";
            if (result.UserScore == 0)
                result.UserScore = 50;
            if (!result.GoodPoints.Any())
                result.GoodPoints.Add("Münazaraya katılım gösterdiniz");
            if (!result.BadPoints.Any())
                result.BadPoints.Add("Daha güçlü argümanlar geliştirebilirsiniz");
            if (string.IsNullOrWhiteSpace(result.DetailedAnalysis))
                result.DetailedAnalysis = "İyi bir münazara performansı sergiledינiz.";

            return result;
        }

        // Sokratik öğretim için ilk soru
        public async Task<string> GenerateFirstSokratikQuestionAsync(string topic)
        {
            var prompt = $@"
Sen bir öğretmensin ve '{topic}' konusunu Sokratik yöntemle öğreteceksin.
Bu konuyla ilgili öğrenciye soracağın ilk soruyu hazırla.

Önemli noktalar:
1. İlk soru öğrencinin mevcut bilgi seviyesini ölçmeli
2. Çok kolay olmamalı ama çok zor da olmamalı
3. Açık uçlu olmalı ve düşünmeye sevk etmeli
4. Öğrenci bilmiyorsa açıklama yapabileceğin bir soru olmalı
5. Türkçe ve samimi bir öğretmen tonu kullan

Örnek başlangıç cümlesi: Bu konuya başlamadan önce... veya Öncelikle... gibi

Sadece soruyu yaz, ek açıklama yapma:
            ";

            return await SendRequestAsync(prompt);
        }

        // Sokratik öğretim devam sorusu
        public async Task<(string nextQuestion, string feedback)> GenerateNextSokratikAsync(SokratikSession session, string answer)
        {
            var history = string.Join("\n", session.QAHistory.Select(qa =>
                $"S: {qa.Question}\nC: {qa.Answer}"));

            var prompt = $@"
Sen bir öğretmensin ve '{session.Konu}' konusunu Sokratik yöntemle öğretiyorsun.

Şimdiye kadarki soru-cevaplar:
{history}

Son soru: {session.QAHistory.LastOrDefault()?.Question}
Öğrencinin cevabı: {answer}

Önemli kurallar:
1. Eğer öğrenci 'bilmiyorum', 'hiç fikrim yok', 'emin değilim' gibi ifadeler kullandıysa:
   - Önce o konuyla ilgili kısa ve anlaşılır bir açıklama yap
   - Sonra öğrencinin anlayıp anlamadığını kontrol edecek veya konuyu pekiştirecek yeni bir soru sor
2. Eğer öğrenci yanlış cevap verdiyse:
   - Kibar bir şekilde düzelt ve doğru bilgiyi ver
   - Konuyu daha iyi anlaması için rehberlik edici soru sor
3. Eğer öğrenci doğru cevap verdiyse:
   - Tebrik et ve konuyu derinleştiren soru sor

Lütfen MUTLAKA şu formatla yanıt ver:
GERİ_BİLDİRİM: [öğrencinin cevabına yapıcı geri bildirim]
SONRAKİ_SORU: [bir sonraki Sokratik soru]

Eğer konu yeterince işlendiyse SONRAKİ_SORU kısmına 'BİTTİ' yaz.
";

            var response = await SendRequestAsync(prompt);

            var feedback = ExtractValueImproved(response, "GERİ_BİLDİRİM:");
            var nextQuestion = ExtractValueImproved(response, "SONRAKİ_SORU:");

            return (nextQuestion, feedback);
        }

        private string ExtractValueImproved(string text, string prefix)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(prefix))
                return "";

            // Düzeltilmiş Regex: Sadece etiketten sonra aynı satır sonuna kadar olan kısmı alır.
            // Bu, API'nin formatlama farklılıklarına karşı daha dayanıklıdır.
            var pattern = $@"^{Regex.Escape(prefix)}\s*(.*)";
            var match = Regex.Match(text, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            return ""; // Eşleşme bulunamazsa boş string dön.
        }

        private int ExtractScoreImproved(string text, string prefix)
        {
            var scoreText = ExtractValueImproved(text, prefix);
            if (int.TryParse(scoreText, out int score))
            {
                // Puanın 0-100 aralığında kalmasını garantile
                return Math.Max(0, Math.Min(100, score));
            }
            // Puan ayrıştırılamazsa 0 döndür. Bu, bir hata olduğunu gösterir.
            return 0;
        }

        private List<string> ExtractListImproved(string text, string prefix)
        {
            var listText = ExtractValueImproved(text, prefix);
            if (string.IsNullOrWhiteSpace(listText))
                return new List<string>();

            return listText.Split(',')
                           .Select(s => s.Trim())
                           .Where(s => !string.IsNullOrWhiteSpace(s))
                           .ToList();
        }

        // Services/GeminiApiService.cs - AI Agent metodlarını ekleyin

        // ========== AI AGENT METODLARI ==========

        // Ana yol haritası üretimi
        public async Task<GeneratedRoadmap> GenerateRoadmapAsync(RoadmapGenerationRequest request)
        {
            var prompt = $@"
Sen bir eğitim uzmanısın ve '{request.Topic}' konusunda {request.UserLevel} seviyesindeki bir öğrenci için kapsamlı bir öğrenme yol haritası hazırlayacaksın.

Öğrenci Bilgileri:
- Konu: {request.Topic}
- Seviye: {request.UserLevel}
- Hedef: {request.LearningGoal}
- Odak Alanları: {string.Join(", ", request.FocusAreas)}

Lütfen şu formatla yanıt ver:
BAŞLIK: [ana konu başlığı]
AÇIKLAMA: [konunun genel açıklaması]
TOPLAM_SÜRE: [tahmini toplam öğrenme saati]
ZORLULİK: [Kolay/Orta/Zor]

KATEGORİ_1: [kategori adı]
KATEGORİ_1_AÇIKLAMA: [kategori açıklaması]
KONU_1_1: [konu başlığı] | [açıklama] | [zorluk] | [tahmini dakika] | [anahtar kelimeler virgülle]
KONU_1_2: [konu başlığı] | [açıklama] | [zorluk] | [tahmini dakika] | [anahtar kelimeler virgülle]

KATEGORİ_2: [kategori adı]
KATEGORİ_2_AÇIKLAMA: [kategori açıklaması]
KONU_2_1: [konu başlığı] | [açıklama] | [zorluk] | [tahmini dakika] | [anahtar kelimeler virgülle]

Kurallar:
- Maksimum {request.MaxNodes} konu oluştur
- Her kategori 2-4 konu içermeli
- Konuları basit -> karmaşık sıralamasıyla düzenle
- Türkçe kullan, anlaşılır ol
- Her konu 15-45 dakika arası olsun

Örnek:
BAŞLIK: Yapay Zeka Temelleri
AÇIKLAMA: Yapay zeka alanına giriş yapacak öğrenciler için temel kavramlar
TOPLAM_SÜRE: 8
ZORLULİK: Orta

KATEGORİ_1: Temel Kavramlar
KATEGORİ_1_AÇIKLAMA: AI'ın temel tanımları ve türleri
KONU_1_1: Yapay Zeka Nedir? | AI'ın tanımı ve günlük hayattaki örnekleri | Kolay | 20 | yapay zeka, tanım, örnekler
KONU_1_2: AI Türleri | Zayıf AI, Güçlü AI ve türleri | Kolay | 25 | AI türleri, zayıf AI, güçlü AI
";

            var response = await SendRequestAsync(prompt);
            return ParseGeneratedRoadmap(response);
        }

        // Spesifik konu için detaylı açıklama üretimi
        public async Task<string> GenerateTopicExplanationAsync(string topic, string userLevel, string context = "")
        {
            var prompt = $@"
Sen bir öğretmensin ve '{topic}' konusunu {userLevel} seviyesindeki bir öğrenciye açıklayacaksın.

{(!string.IsNullOrEmpty(context) ? $"Bağlam: {context}" : "")}

Lütfen şu kurallara uyarak açıklama yap:
1. {userLevel} seviyesine uygun dil kullan
2. Somut örnekler ver
3. Karmaşık kavramları basit benzetmelerle açıkla
4. 2-3 paragraf halinde yaz
5. Türkçe kullan

Sadece açıklamayı yaz, ek etiket veya format kullanma:
";

            return await SendRequestAsync(prompt);
        }

        // Video arama için YouTube query üretimi
        public async Task<(string query, string expectedTitle)> GenerateVideoSearchAsync(string topic)
        {
            var prompt = $@"
'{topic}' konusu için YouTube'da aranacak en iyi arama sorgusu ve beklenen video başlığını üret.

Lütfen şu formatla yanıt ver:
ARAMA: [YouTube'da aranacak sorgu]
BAŞLIK: [beklenen video başlığı]

Kurallar:
- Türkçe içerik öncelikli olsun
- Eğitici/öğretici videolar hedeflesin
- Çok spesifik olmayan, genel arama terimleri kullan

Örnek:
ARAMA: yapay zeka nedir türkçe anlatım
BAŞLIK: Yapay Zeka Nedir? - Temel Kavramlar

Sadece formatı kullan:
";

            var response = await SendRequestAsync(prompt);
            var query = ExtractValueImproved(response, "ARAMA:");
            var title = ExtractValueImproved(response, "BAŞLIK:");

            return (query, title);
        }

        // Makale arama için sorgu üretimi
        public async Task<(string query, string expectedTitle)> GenerateArticleSearchAsync(string topic)
        {
            var prompt = $@"
'{topic}' konusu için web'de aranacak en iyi makale arama sorgusu ve beklenen makale başlığını üret.

Lütfen şu formatla yanıt ver:
ARAMA: [web'de aranacak sorgu]
BAŞLIK: [beklenen makale başlığı]

Kurallar:
- Türkçe içerik öncelikli
- Akademik veya güvenilir kaynaklardan
- Detaylı açıklama içeren makaleler

Örnek:
ARAMA: yapay zeka algoritmaları makale türkçe
BAŞLIK: Yapay Zeka Algoritmaları ve Uygulamaları

Sadece formatı kullan:
";

            var response = await SendRequestAsync(prompt);
            var query = ExtractValueImproved(response, "ARAMA:");
            var title = ExtractValueImproved(response, "BAŞLIK:");

            return (query, title);
        }

        // Öğrenme önerisi üretimi
        public async Task<string> GenerateLearningRecommendationAsync(AIAgentSession session)
        {
            var completedTopics = string.Join(", ", session.Nodes
                .Where(n => session.CompletedNodeIds.Contains(n.Id))
                .Select(n => n.Title));

            var availableTopics = string.Join(", ", session.Nodes
                .Where(n => !session.CompletedNodeIds.Contains(n.Id))
                .Select(n => n.Title));

            var prompt = $@"
Bir öğrenci '{session.MainTopic}' konusunu öğreniyor.

Tamamladığı konular: {completedTopics}
Henüz çalışmadığı konular: {availableTopics}

Bu öğrenciye sıradaki öğrenmesi gereken konuyu önererek motivasyonel bir mesaj yaz.

Kurallar:
- Samimi ve motive edici ol
- Spesifik bir sonraki adım öner
- Maksimum 2 cümle
- Türkçe kullan

Sadece önerinizi yazın:
";

            return await SendRequestAsync(prompt);
        }

        // Progress analizi
        public async Task<string> GenerateProgressAnalysisAsync(AIAgentSession session, List<UserProgress> userProgresses)
        {
            var totalNodes = session.Nodes.Count;
            var completedNodes = session.CompletedNodeIds.Count;
            var totalTimeSpent = userProgresses.Sum(p => p.TimeSpentMinutes);

            var completedTopics = session.Nodes
                .Where(n => session.CompletedNodeIds.Contains(n.Id))
                .Select(n => n.Title);

            var prompt = $@"
Bir öğrencinin '{session.MainTopic}' konusundaki öğrenme ilerlemesini analiz et:

İstatistikler:
- Toplam konu: {totalNodes}
- Tamamlanan: {completedNodes}
- Geçirilen süre: {totalTimeSpent} dakika
- Tamamlanan konular: {string.Join(", ", completedTopics)}

Bu veriler ışığında öğrenciye:
1. Başarılarını tebrik eden
2. Eksik alanları belirten  
3. Motivasyonel bir değerlendirme mesajı yaz

Maksimum 3 cümle, Türkçe:
";

            return await SendRequestAsync(prompt);
        }

        // Parsing metodları
        private GeneratedRoadmap ParseGeneratedRoadmap(string response)
        {
            var roadmap = new GeneratedRoadmap();
            var lines = response.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToArray();

            roadmap.MainTopic = ExtractValueImproved(response, "BAŞLIK:");
            roadmap.Description = ExtractValueImproved(response, "AÇIKLAMA:");
            roadmap.Difficulty = ExtractValueImproved(response, "ZORLULİK:");

            if (int.TryParse(ExtractValueImproved(response, "TOPLAM_SÜRE:"), out int hours))
                roadmap.EstimatedTotalHours = hours;

            var categories = new List<RoadmapCategory>();
            RoadmapCategory currentCategory = null;
            int categoryOrder = 1;
            int topicOrder = 1;

            foreach (var line in lines)
            {
                if (line.StartsWith("KATEGORİ_") && line.Contains(":") && !line.Contains("_AÇIKLAMA"))
                {
                    // Yeni kategori
                    if (currentCategory != null)
                        categories.Add(currentCategory);

                    currentCategory = new RoadmapCategory
                    {
                        Name = line.Substring(line.IndexOf(':') + 1).Trim(),
                        Order = categoryOrder++
                    };
                    topicOrder = 1;
                }
                else if (line.StartsWith("KATEGORİ_") && line.Contains("_AÇIKLAMA:"))
                {
                    // Kategori açıklaması
                    if (currentCategory != null)
                        currentCategory.Description = line.Substring(line.IndexOf(':') + 1).Trim();
                }
                else if (line.StartsWith("KONU_") && line.Contains("|"))
                {
                    // Konu parsing
                    if (currentCategory != null)
                    {
                        var parts = line.Substring(line.IndexOf(':') + 1).Split('|');
                        if (parts.Length >= 5)
                        {
                            var topic = new RoadmapTopic
                            {
                                Title = parts[0].Trim(),
                                Description = parts[1].Trim(),
                                Difficulty = parts[2].Trim(),
                                Order = topicOrder++
                            };

                            if (int.TryParse(parts[3].Trim(), out int minutes))
                                topic.EstimatedMinutes = minutes;

                            topic.Keywords = parts[4].Split(',').Select(k => k.Trim()).ToList();
                            currentCategory.Topics.Add(topic);
                        }
                    }
                }
            }

            if (currentCategory != null)
                categories.Add(currentCategory);

            roadmap.Categories = categories;
            return roadmap;
        }

        /*
        public async Task<string> SendRawPromptAsync(string prompt)
        {
            // Bu public metot, sınıfın kendi içindeki private SendRequestAsync metodunu çağırır.
            return await SendRequestAsync(prompt);
        }
        */
        
        public async Task<string> TranslateToEnglishAcademicQueryAsync(string topic)
        {
            var prompt = $@"
        Aşağıdaki Türkçe konu başlığını, uluslararası akademik bir veritabanında (CORE API gibi) arama yapmak için en uygun, kısa ve net İngilizce arama terimine çevir. 
        Sadece çevrilmiş İngilizce arama terimini yaz, ek açıklama yapma.

        Türkçe Konu: '{topic}'

        İngilizce Arama Terimi:
    ";

            var response = await SendRequestAsync(prompt);

            // API'den "Yanıt alınamadı." gibi bir hata dönerse, orijinal konuyu kullan
            if (response.Contains("Hata") || response.Contains("alınamadı"))
            {
                return topic;
            }

            return response;
        }
        
    }
}