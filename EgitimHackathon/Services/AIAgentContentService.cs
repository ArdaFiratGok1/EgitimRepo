using EgitimMaskotuApp.Models;

namespace EgitimMaskotuApp.Services
{
    public class NodeContentSuggestions
    {
        public string Topic { get; set; }
        public List<ContentSuggestion> Videos { get; set; } = new();
        public List<ContentSuggestion> Articles { get; set; } = new();
    }

    public class ContentSuggestion
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string Source { get; set; }
        public string Type { get; set; } // "Video" or "Article"
        public string ThumbnailUrl { get; set; }
    }

    /// <summary>
    /// /////////////////////////////////////////////
    /// </summary>
    public class AIAgentContentService
    {
        private readonly YouTubeService _youtubeService;
        private readonly CoreApiService _coreApiService;
        private readonly GeminiApiService _geminiApiService;

        public AIAgentContentService(YouTubeService youtubeService, CoreApiService coreApiService, GeminiApiService geminiApiService)
        {
            _youtubeService = youtubeService;
            _coreApiService = coreApiService;
            _geminiApiService = geminiApiService;
        }

        // LearningNode'u içerikle zenginleştir
        public async Task<LearningNode> EnrichNodeWithContentAsync(LearningNode node, string userLevel)
        {
            try
            {
                var originalSearchQuery = !string.IsNullOrEmpty(node.Title) ? node.Title : string.Join(" ", node.Keywords);

                // 1. Makale araması için konuyu İngilizce'ye çevir
                var englishSearchQuery = await _geminiApiService.TranslateToEnglishAcademicQueryAsync(originalSearchQuery);
                Console.WriteLine($"[DEBUG] Türkçe Sorgu: {originalSearchQuery} -> İngilizce Sorgu: {englishSearchQuery}");

                // 2. API'leri bu sorgularla çağır
                var videoTask = _youtubeService.SearchVideosAsync(originalSearchQuery, 1);
                var articleTask = _coreApiService.SearchArticlesAsync(englishSearchQuery, 5, userLevel);
                var aiExplanationTask = _geminiApiService.GenerateTopicExplanationAsync(originalSearchQuery, userLevel);

                await Task.WhenAll(videoTask, articleTask, aiExplanationTask);

                var videos = await videoTask;
                var articles = await articleTask;
                var aiExplanation = await aiExplanationTask;

                if (videos.Any())
                {
                    var video = videos.First();
                    node.VideoUrl = $"https://www.youtube.com/watch?v={video.VideoId}";
                    node.VideoTitle = video.Title;
                }

                if (articles.Any())
                {
                    var article = articles.First();
                    node.ArticleUrl = article.Url;
                    node.ArticleTitle = article.Title;
                    node.ArticleSource = article.Source;
                }

                if (!string.IsNullOrEmpty(aiExplanation) && !aiExplanation.Contains("Hata"))
                {
                    node.AIExplanation = aiExplanation;
                }

                return node;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enriching node content: {ex.Message}");
                return node;
            }
        }

        // Session'daki tüm node'ları zenginleştir
        public async Task<AIAgentSession> EnrichSessionWithContentAsync(AIAgentSession session)
        {
            var semaphore = new SemaphoreSlim(3, 3);

            var tasks = session.Nodes.Select(async node =>
            {
                await semaphore.WaitAsync();
                try
                {
                    // === DEĞİŞİKLİK BURADA: Eksik olan 'session.UserLevel' parametresi eklendi ===
                    return await EnrichNodeWithContentAsync(node, session.UserLevel);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var enrichedNodes = await Task.WhenAll(tasks);
            session.Nodes = enrichedNodes.ToList();
            session.LastAccessedAt = DateTime.Now;

            return session;
        }
        public async Task<NodeContentSuggestions> GetContentSuggestionsAsync(string topic, int videoCount = 3, int articleCount = 5)
        {
            var videoTask = _youtubeService.SearchVideosAsync(topic, videoCount);
            var articleTask = _coreApiService.SearchArticlesAsync(topic, articleCount);

            await Task.WhenAll(videoTask, articleTask);

            return new NodeContentSuggestions
            {
                Topic = topic,
                Videos = (await videoTask).Select(v => new ContentSuggestion
                {
                    Title = v.Title,
                    Url = $"https://www.youtube.com/watch?v={v.VideoId}",
                    Description = TruncateText(v.Description, 150),
                    Source = v.ChannelTitle,
                    Type = "Video",
                    ThumbnailUrl = v.ThumbnailUrl
                }).ToList(),
                Articles = (await articleTask).Select(a => new ContentSuggestion
                {
                    Title = a.Title,
                    Url = a.Url,
                    Description = TruncateText(a.Snippet, 150),
                    Source = a.Source,
                    Type = "Article"
                }).ToList()
            };
        }

        // Yardımcı metodlar
        private List<string> ExtractKeywords(string title)
        {
            if (string.IsNullOrEmpty(title)) return new List<string>();

            var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                             .Where(w => w.Length > 2)
                             .Select(w => w.ToLower().Trim(',', '.', '!', '?', ':', ';'))
                             .Where(w => !IsStopWord(w))
                             .Take(5)
                             .ToList();
            return words;
        }

        private bool IsStopWord(string word)
        {
            var stopWords = new[] { "ve", "ile", "için", "bir", "bu", "şu", "o", "da", "de", "ta", "te", "la", "le" };
            return stopWords.Contains(word);
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength) + "...";
        }

        // Mevcut mock metodlarınızı gerçek API'lerle değiştirmek için
        public async Task<string> SearchYouTubeVideo(string query)
        {
            return await _youtubeService.GetBestVideoForTopicAsync(query);
        }

        public async Task<string> SearchWebArticle(string query)
        {
            return await _coreApiService.GetBestArticleForTopicAsync(query);
        }
    }
}