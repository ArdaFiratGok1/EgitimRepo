// Controllers/AIAgentController.cs

using Microsoft.AspNetCore.Mvc;
using EgitimMaskotuApp.Models;
using EgitimMaskotuApp.Services;
using System.Text.Json;

namespace EgitimMaskotuApp.Controllers
{
    public class AIAgentController : Controller
    {
        private readonly GeminiApiService _geminiService;
        private readonly HttpClient _httpClient;

        public AIAgentController(GeminiApiService geminiService, HttpClient httpClient)
        {
            _geminiService = geminiService;
            _httpClient = httpClient;
        }

        // Ana sayfa - Konu girişi
        public IActionResult Index()
        {
            return View(new AIAgentStartViewModel());
        }

        // Yol haritası oluşturma
        [HttpPost]
        public async Task<IActionResult> CreateRoadmap(AIAgentStartViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Topic))
            {
                ModelState.AddModelError("Topic", "Lütfen bir konu girin.");
                return View("Index", model);
            }

            try
            {
                // AI ile yol haritası üret
                var request = new RoadmapGenerationRequest
                {
                    Topic = model.Topic,
                    UserLevel = model.UserLevel,
                    LearningGoal = model.LearningGoal,
                    FocusAreas = model.FocusAreas ?? new List<string>(),
                    MaxNodes = 12
                };

                var generatedRoadmap = await _geminiService.GenerateRoadmapAsync(request);

                // Session oluştur
                var session = new AIAgentSession
                {
                    MainTopic = generatedRoadmap.MainTopic,
                    UserLevel = model.UserLevel
                };

                // Düğümleri oluştur
                await CreateLearningNodesAsync(session, generatedRoadmap);

                // Session'ı kaydet
                HttpContext.Session.SetString("AIAgentSession", JsonSerializer.Serialize(session));

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Yol haritası oluşturulurken hata oluştu: " + ex.Message);
                return View("Index", model);
            }
        }

        // Ana dashboard
        public async Task<IActionResult> Dashboard()
        {
            var sessionJson = HttpContext.Session.GetString("AIAgentSession");
            if (string.IsNullOrEmpty(sessionJson))
            {
                return RedirectToAction("Index");
            }

            var session = JsonSerializer.Deserialize<AIAgentSession>(sessionJson);

            // View model hazırla
            var viewModel = new AIAgentDashboardViewModel
            {
                Session = session,
                CompletedNodes = session.Nodes.Where(n => session.CompletedNodeIds.Contains(n.Id)).ToList(),
                AvailableNodes = GetAvailableNodes(session),
                CompletionPercentage = CalculateCompletionPercentage(session)
            };

            // AI önerisi al
            try
            {
                viewModel.CurrentRecommendation = await _geminiService.GenerateLearningRecommendationAsync(session);
            }
            catch
            {
                viewModel.CurrentRecommendation = "Öğrenmeye devam edin! Sonraki konuya geçebilirsiniz.";
            }

            return View(viewModel);
        }

        // Düğüm detayı
        public async Task<IActionResult> NodeDetail(string nodeId)
        {
            var sessionJson = HttpContext.Session.GetString("AIAgentSession");
            if (string.IsNullOrEmpty(sessionJson))
            {
                return RedirectToAction("Index");
            }

            var session = JsonSerializer.Deserialize<AIAgentSession>(sessionJson);
            var node = session.Nodes.FirstOrDefault(n => n.Id == nodeId);

            if (node == null)
            {
                return NotFound();
            }

            // Eğer içerik henüz üretilmemişse üret
            if (string.IsNullOrEmpty(node.AIExplanation))
            {
                await PopulateNodeContentAsync(node, session.UserLevel);

                // Session'ı güncelle
                HttpContext.Session.SetString("AIAgentSession", JsonSerializer.Serialize(session));
            }

            var viewModel = new NodeDetailViewModel
            {
                Session = session,
                CurrentNode = node,
                Prerequisites = GetPrerequisites(session, node),
                NextNodes = GetNextNodes(session, node),
                CanAccess = CanAccessNode(session, node)
            };

            return View(viewModel);
        }

        // Düğümü tamamla
        [HttpPost]
        public IActionResult CompleteNode(string nodeId)
        {
            var sessionJson = HttpContext.Session.GetString("AIAgentSession");
            if (string.IsNullOrEmpty(sessionJson))
            {
                return RedirectToAction("Index");
            }

            var session = JsonSerializer.Deserialize<AIAgentSession>(sessionJson);

            if (!session.CompletedNodeIds.Contains(nodeId))
            {
                session.CompletedNodeIds.Add(nodeId);
                session.LastAccessedAt = DateTime.Now;

                HttpContext.Session.SetString("AIAgentSession", JsonSerializer.Serialize(session));
            }

            return RedirectToAction("Dashboard");
        }

        // Progress sayfası
        public async Task<IActionResult> Progress()
        {
            var sessionJson = HttpContext.Session.GetString("AIAgentSession");
            if (string.IsNullOrEmpty(sessionJson))
            {
                return RedirectToAction("Index");
            }

            var session = JsonSerializer.Deserialize<AIAgentSession>(sessionJson);

            // Mock user progress data (gerçek uygulamada veritabanından gelecek)
            var userProgresses = session.CompletedNodeIds.Select(nodeId => new UserProgress
            {
                SessionId = session.Id,
                NodeId = nodeId,
                IsCompleted = true,
                CompletedAt = DateTime.Now.AddDays(-new Random().Next(0, 7)),
                TimeSpentMinutes = new Random().Next(10, 45)
            }).ToList();

            ViewBag.ProgressAnalysis = await _geminiService.GenerateProgressAnalysisAsync(session, userProgresses);
            ViewBag.Session = session;
            ViewBag.UserProgresses = userProgresses;

            return View();
        }

        // Yeni roadmap oluştur
        public IActionResult NewRoadmap()
        {
            HttpContext.Session.Remove("AIAgentSession");
            return RedirectToAction("Index");
        }

        // ========== PRIVATE HELPER METODLARI ==========

        private async Task CreateLearningNodesAsync(AIAgentSession session, GeneratedRoadmap roadmap)
        {
            var nodes = new List<LearningNode>();
            int nodeIndex = 0;

            foreach (var category in roadmap.Categories)
            {
                foreach (var topic in category.Topics)
                {
                    var node = new LearningNode
                    {
                        Title = topic.Title,
                        Category = category.Name,
                        Level = 2, // Alt konu seviyesi
                        EstimatedMinutes = topic.EstimatedMinutes,
                        Difficulty = topic.Difficulty,
                        Keywords = topic.Keywords,
                        Position = new NodePosition
                        {
                            X = (nodeIndex % 4) * 300 + 50,
                            Y = (nodeIndex / 4) * 200 + 100,
                            Color = GetCategoryColor(category.Name)
                        }
                    };

                    nodes.Add(node);
                    nodeIndex++;
                }
            }

            session.Nodes = nodes;
        }

        private async Task PopulateNodeContentAsync(LearningNode node, string userLevel)
        {
            try
            {
                // AI açıklaması üret
                node.AIExplanation = await _geminiService.GenerateTopicExplanationAsync(
                    node.Title, userLevel, $"Kategori: {node.Category}");

                // Video arama sorgusu üret
                var (videoQuery, expectedVideoTitle) = await _geminiService.GenerateVideoSearchAsync(node.Title);

                // Makale arama sorgusu üret
                var (articleQuery, expectedArticleTitle) = await _geminiService.GenerateArticleSearchAsync(node.Title);

                // Mock video ve makale linkleri (gerçek uygulamada YouTube ve Web API kullanılacak)
                node.VideoUrl = await SearchYouTubeVideo(videoQuery);
                node.VideoTitle = expectedVideoTitle;
                node.ArticleUrl = await SearchWebArticle(articleQuery);
                node.ArticleTitle = expectedArticleTitle;
                node.ArticleSource = "Web";
            }
            catch (Exception ex)
            {
                // Hata durumunda varsayılan değerler
                node.AIExplanation = $"{node.Title} konusu hakkında detaylı bilgi yükleniyor...";
                node.VideoTitle = "Video yükleniyor...";
                node.ArticleTitle = "Makale yükleniyor...";
            }
        }

        private async Task<string> SearchYouTubeVideo(string query)
        {
            // Mock implementation - gerçek uygulamada YouTube Data API kullanılacak
            var mockVideos = new Dictionary<string, string>
            {
                {"yapay zeka", "https://www.youtube.com/watch?v=dQw4w9WgXcQ"},
                {"makine öğrenmesi", "https://www.youtube.com/watch?v=dQw4w9WgXcQ"},
                {"algoritma", "https://www.youtube.com/watch?v=dQw4w9WgXcQ"}
            };

            var normalizedQuery = query.ToLower();
            var matchingVideo = mockVideos.FirstOrDefault(kv =>
                normalizedQuery.Contains(kv.Key.ToLower())).Value;

            return matchingVideo ?? "https://www.youtube.com/results?search_query=" + Uri.EscapeDataString(query);
        }

        private async Task<string> SearchWebArticle(string query)
        {
            // Mock implementation - gerçek uygulamada Web Search API kullanılacak
            var mockArticles = new Dictionary<string, string>
            {
                {"yapay zeka", "https://tr.wikipedia.org/wiki/Yapay_zeka"},
                {"makine öğrenmesi", "https://medium.com/@example/makine-ogrenmesi"},
                {"algoritma", "https://www.example.com/algoritma-rehberi"}
            };

            var normalizedQuery = query.ToLower();
            var matchingArticle = mockArticles.FirstOrDefault(kv =>
                normalizedQuery.Contains(kv.Key.ToLower())).Value;

            return matchingArticle ?? "https://www.google.com/search?q=" + Uri.EscapeDataString(query);
        }

        /* Burasını kullanacağım. Şu anda mock data kullanıyor. Youtube API ve Bing Search API kullanacağım
        // YouTube API için (appsettings.json'a API key ekleyin)
        private async Task<string> SearchYouTubeVideo(string query)
        {
            var apiKey = "YOUR_YOUTUBE_API_KEY";
            var searchUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=1&q={Uri.EscapeDataString(query)}&type=video&key={apiKey}";

            try
            {
                var response = await _httpClient.GetStringAsync(searchUrl);
                var result = JsonSerializer.Deserialize<YouTubeSearchResponse>(response);
                return $"https://www.youtube.com/watch?v={result.items[0].id.videoId}";
            }
            catch
            {
                return "https://www.youtube.com/results?search_query=" + Uri.EscapeDataString(query);
            }
        }

        // Web arama için (Bing Search API kullanabilirsiniz)
        private async Task<string> SearchWebArticle(string query)
        {
            // Bing Search API veya Google Custom Search API kullanabilirsiniz
            // Şimdilik basit bir Google arama linki döndürüyor
            return "https://www.google.com/search?q=" + Uri.EscapeDataString(query + " makale rehber");
        }

        */

        /* Burayı da Gemini hazırladı bunu da kullanabilirim.
        public async Task<string> SearchYouTubeVideoAsync(string query)
    {
        // API anahtarını güvenli bir şekilde appsettings.json'dan okuyoruz
        var apiKey = _configuration["ApiKeys:YouTube"];
        if (string.IsNullOrEmpty(apiKey))
        {
            // Anahtar yoksa, varsayılan arama linkini döndür
            return "https://www.youtube.com/results?search_query=" + Uri.EscapeDataString(query);
        }

        var searchUrl = $"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=1&q={Uri.EscapeDataString(query)}&type=video&key={apiKey}";

        try
        {
            var response = await _httpClient.GetStringAsync(searchUrl);
            var result = JsonSerializer.Deserialize<YouTubeSearchResponse>(response);

            // ÖNEMLİ: Sonuçların null veya boş olup olmadığını kontrol ediyoruz
            if (result?.Items != null && result.Items.Any())
            {
                var videoId = result.Items[0].Id?.VideoId;
                if (!string.IsNullOrEmpty(videoId))
                {
                    return $"https://www.youtube.com/watch?v={videoId}";
                }
            }

            // Sonuç bulunamazsa, varsayılan arama linkini döndür
            return "https://www.youtube.com/results?search_query=" + Uri.EscapeDataString(query);
        }
        catch (HttpRequestException ex)
        {
            // Hata durumunda loglama yapabilirsiniz. Örn: _logger.LogError(ex, "YouTube API request failed.");
            return "https://www.youtube.com/results?search_query=" + Uri.EscapeDataString(query);
        }
    }

    // --- Geliştirilmiş Web Arama Metodu (Bing API Örneği) ---
    public async Task<string> SearchWebArticleAsync(string query)
    {
        var apiKey = _configuration["ApiKeys:BingSearch"];
        if (string.IsNullOrEmpty(apiKey))
        {
            return "https://www.google.com/search?q=" + Uri.EscapeDataString(query);
        }

        var searchUrl = $"https://api.bing.microsoft.com/v7.0/search?q={Uri.EscapeDataString(query)}&count=1";
        
        var request = new HttpRequestMessage(HttpMethod.Get, searchUrl);
        // Bing API, anahtarı header'da bekler
        request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode(); // HTTP 2xx dışında bir kod varsa hata fırlatır

            var content = await response.Content.ReadAsStringAsync();
            // Not: Bing'den gelen JSON'ı deserialize etmek için de ilgili DTO sınıflarını oluşturmanız gerekir.
            // Bu örnekte, basitlik adına ilk linki manuel olarak buluyoruz.
            // Gerçek uygulamada JsonDocument veya özel DTO'lar kullanın.
            using (var jsonDoc = JsonDocument.Parse(content))
            {
                var firstUrl = jsonDoc.RootElement
                                      .GetProperty("webPages")
                                      .GetProperty("value")[0]
                                      .GetProperty("url")
                                      .GetString();
                
                return firstUrl ?? "https://www.google.com/search?q=" + Uri.EscapeDataString(query);
            }
        }
        catch (Exception ex) // HttpRequestException, JsonException vb. yakalar
        {
            // Hata durumunda loglama yapabilirsiniz.
            return "https://www.google.com/search?q=" + Uri.EscapeDataString(query);
        }
    }*/
        private List<LearningNode> GetAvailableNodes(AIAgentSession session)
        {
            // Önkoşulları tamamlanmış ve henüz başlanmamış düğümler
            return session.Nodes.Where(node =>
                !session.CompletedNodeIds.Contains(node.Id) &&
                CanAccessNode(session, node)
            ).ToList();
        }

        private List<LearningNode> GetPrerequisites(AIAgentSession session, LearningNode node)
        {
            return session.Nodes.Where(n =>
                node.PrerequisiteIds.Contains(n.Id)
            ).ToList();
        }

        private List<LearningNode> GetNextNodes(AIAgentSession session, LearningNode currentNode)
        {
            // Mevcut düğümü önkoşul olarak kullanan düğümler
            return session.Nodes.Where(n =>
                n.PrerequisiteIds.Contains(currentNode.Id)
            ).Take(3).ToList();
        }

        private bool CanAccessNode(AIAgentSession session, LearningNode node)
        {
            // Tüm önkoşullar tamamlandı mı kontrol et
            return node.PrerequisiteIds.All(prereqId =>
                session.CompletedNodeIds.Contains(prereqId));
        }

        private int CalculateCompletionPercentage(AIAgentSession session)
        {
            if (session.Nodes.Count == 0) return 0;
            return (session.CompletedNodeIds.Count * 100) / session.Nodes.Count;
        }

        private string GetCategoryColor(string categoryName)
        {
            var colors = new Dictionary<string, string>
            {
                {"Temel Kavramlar", "#28a745"},      // Yeşil
                {"Makine Öğrenmesi", "#007bff"},     // Mavi  
                {"Derin Öğrenme", "#dc3545"},        // Kırmızı
                {"Uygulamalar", "#ffc107"},          // Sarı
                {"İleri Konular", "#6f42c1"},        // Mor
                {"Praktik", "#fd7e14"}               // Turuncu
            };

            return colors.ContainsKey(categoryName) ? colors[categoryName] : "#6c757d";
        }


    }
}