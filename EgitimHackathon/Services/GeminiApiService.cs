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
    }
}