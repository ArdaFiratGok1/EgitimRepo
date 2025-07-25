using Newtonsoft.Json;
using System.Linq;

namespace EgitimMaskotuApp.Services
{
    public class CoreApiSearchResult
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Snippet { get; set; }
        public string Source { get; set; }
    }

    // --- DTO'lar GERÇEK CORE API YANITINA GÖRE GÜNCELLENDİ ---

    public class CoreApiResponse
    {
        [JsonProperty("totalHits")]
        public int TotalHits { get; set; }

        [JsonProperty("results")]
        public List<CoreApiWork> Results { get; set; } = new();
    }

    public class CoreApiWork
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        // DEĞİŞİKLİK: 'List<string>' yerine 'List<CoreApiAuthor>' yapıldı
        [JsonProperty("authors")]
        public List<CoreApiAuthor> Authors { get; set; } = new();

        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonProperty("abstract")]
        public string Abstract { get; set; }

        [JsonProperty("doi")]
        public string Doi { get; set; }
    }

    // DEĞİŞİKLİK: Gelen JSON'daki {"name": "..."} yapısını karşılamak için yeni bir sınıf eklendi
    public class CoreApiAuthor
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class CoreApiService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://api.core.ac.uk/v3";
        private readonly string _apiKey;

        public CoreApiService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _apiKey = _configuration["ApiKeys:CoreApi"];
        }

        public async Task<List<CoreApiSearchResult>> SearchArticlesAsync(string query, int count = 10, string userLevel = "Başlangıç")
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new List<CoreApiSearchResult>();
            }

            try
            {
                // === DEĞİŞİKLİK: TÜM FİLTRELER KALDIRILDI ===
                // API'ye sadece ve sadece konunun kendisi gönderiliyor.
                // Bu, sonuç almayı garanti altına almanın en basit yoludur.
                var encodedQuery = Uri.EscapeDataString(query);
                var requestUrl = $"{_baseUrl}/search/works?q={encodedQuery}&limit={count}&api_key={_apiKey}";
                Console.WriteLine($"[DEBUG] API İsteği URL: {requestUrl}"); // Bu satır önemli!
                // ===========================================

                var response = await _httpClient.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<CoreApiResponse>(jsonContent);

                    if (apiResponse?.Results != null && apiResponse.Results.Any())
                    {
                        var searchResults = apiResponse.Results.Select(work => new CoreApiSearchResult
                        {
                            Title = work.Title ?? "Başlık Yok",
                            Url = GetWorkUrl(work),
                            Snippet = work.Abstract ?? "Açıklama mevcut değil.",
                            Source = work.Authors.Any() ? string.Join(", ", work.Authors.Select(a => a.Name)) : "CORE Academic"
                        }).Where(r => !string.IsNullOrEmpty(r.Url)).ToList();

                        // Kaliteyi artırmak için sıralama mantığı hala devrede.
                        var rankedResults = searchResults
                            .Select(r => new { Result = r, Score = CalculateRelevanceScore(r, query) })
                            .OrderByDescending(x => x.Score)
                            .Select(x => x.Result)
                            .ToList();

                        return rankedResults;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"CORE API Hatası: {response.StatusCode} - {errorContent}");
                }

                return new List<CoreApiSearchResult>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CORE API'de Beklenmedik Hata: {ex.Message}");
                return new List<CoreApiSearchResult>();
            }
        }

        private int CalculateRelevanceScore(CoreApiSearchResult result, string originalQuery)
        {
            int score = 0;
            var queryLower = originalQuery.ToLower();

            if (result.Title.ToLower().Contains(queryLower)) score += 50;
            if (result.Snippet.ToLower().Contains(queryLower)) score += 20;
            if (result.Url.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) score += 30;

            return score;
        }

        public async Task<string> GetBestArticleForTopicAsync(string topic)
        {
            var articles = await SearchArticlesAsync(topic, 5); // 1 yerine 5 sonuç isteyip en iyisini seçmek daha iyi
            return articles.FirstOrDefault()?.Url ?? $"https://scholar.google.com/scholar?q={Uri.EscapeDataString(topic)}";
        }

        private string GetWorkUrl(CoreApiWork work)
        {
            if (!string.IsNullOrEmpty(work.DownloadUrl)) return work.DownloadUrl;
            if (!string.IsNullOrEmpty(work.Doi)) return $"https://doi.org/{work.Doi}";
            return $"https://core.ac.uk/work/{work.Id}";
        }
    }
}