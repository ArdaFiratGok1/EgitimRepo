using EgitimMaskotuApp.Models;
using System.Text;
using System.Text.Json;

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

        // Münazara konusu analizi
        public async Task<(string konu, string kullaniciTarafi, string aiTarafi)> AnalyzeMunazaraTopicAsync(string rawTopic)
        {
            var prompt = $@"
Kullanıcı şu münazara konusunu verdi: '{rawTopic}'

Lütfen bu konuyu analiz et ve şu formatla yanıt ver:
KONU: [net ve anlaşılır münazara konusu]
KULLANICI_TARAFI: [kullanıcının savunacağı taraf]
AI_TARAFI: [AI'ın savunacağı karşıt taraf]

Konuyu Türkçe eğitim seviyesine uygun hale getir ve her iki taraf için de mantıklı argümanlar bulunabilecek şekilde düzenle.
Eğer kullanıcı tarafları belirtmişse onları kullan, belirtmemişse sen belirle.
";

            var response = await SendRequestAsync(prompt);

            // Yanıtı parse et
            var lines = response.Split('\n');
            var konu = ExtractValue(lines, "KONU:");
            var kullaniciTarafi = ExtractValue(lines, "KULLANICI_TARAFI:");
            var aiTarafi = ExtractValue(lines, "AI_TARAFI:");

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

        // Münazara sonucu değerlendirmesi
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

Lütfen şu formatla değerlendirme yap:
KAZANAN: [Kullanıcı veya AI]
PUAN: [0-100 arası kullanıcı puanı]
İYİ_YANLAR: [kullanıcının güçlü argümanları, virgülle ayırarak]
KÖTÜ_YANLAR: [kullanıcının zayıf noktaları, virgülle ayırarak]
DETAYLI_ANALIZ: [kapsamlı değerlendirme]

Değerlendirme kriterleri:
- Argüman gücü
- Mantıksal tutarlılık  
- Karşı argümanlara yanıt verme
- İkna edicilik
- Bilgi doğruluğu
";

            var response = await SendRequestAsync(prompt);

            return new MunazaraResult
            {
                Winner = ExtractValue(response.Split('\n'), "KAZANAN:"),
                UserScore = int.TryParse(ExtractValue(response.Split('\n'), "PUAN:"), out int score) ? score : 50,
                GoodPoints = ExtractValue(response.Split('\n'), "İYİ_YANLAR:").Split(',').Select(s => s.Trim()).ToList(),
                BadPoints = ExtractValue(response.Split('\n'), "KÖTÜ_YANLAR:").Split(',').Select(s => s.Trim()).ToList(),
                DetailedAnalysis = ExtractValue(response.Split('\n'), "DETAYLI_ANALIZ:")
            };
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

Lütfen şu formatla yanıt ver:
GERİ_BİLDİRİM: [öğrencinin cevabına yapıcı geri bildirim. Bilmiyorsa açıklama yap, yanlışsa düzelt, doğruysa tebrik et]
SONRAKİ_SORU: [bir sonraki Sokratik soru. Açıklama yaptıysan o konuyu pekiştirecek soru sor]

Eğer konu yeterince işlendiyse SONRAKİ_SORU kısmına 'BİTTİ' yaz.
";

            var response = await SendRequestAsync(prompt);
            var lines = response.Split('\n');

            var feedback = ExtractValue(lines, "GERİ_BİLDİRİM:");
            var nextQuestion = ExtractValue(lines, "SONRAKİ_SORU:");

            return (nextQuestion, feedback);
        }

        private string ExtractValue(string[] lines, string prefix)
        {
            var line = lines.FirstOrDefault(l => l.StartsWith(prefix));
            return line?.Substring(prefix.Length).Trim() ?? "";
        }
    }
}