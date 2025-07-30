using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace EgitimMaskotuApp.Services
{
    public class YouTubeSearchResult
    {
        public string VideoId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ChannelTitle { get; set; }
        public string ThumbnailUrl { get; set; }
        public DateTime PublishedAt { get; set; }
    }

    public class YouTubeService
    {
        private readonly string _apiKey;
        private readonly Google.Apis.YouTube.v3.YouTubeService _youtubeService;

        public YouTubeService(string apiKey)
        {
            _apiKey = apiKey;
            _youtubeService = new Google.Apis.YouTube.v3.YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _apiKey,
                ApplicationName = "EgitimMaskotuApp"
            });
        }

        public async Task<List<YouTubeSearchResult>> SearchVideosAsync(string query, int maxResults = 5)
        {
            try
            {
                var searchListRequest = _youtubeService.Search.List("snippet");
                searchListRequest.Q = query;
                searchListRequest.Type = "video";
                searchListRequest.MaxResults = maxResults;
                searchListRequest.Order = Google.Apis.YouTube.v3.SearchResource.ListRequest.OrderEnum.Relevance;

                searchListRequest.RegionCode = "TR"; //Türkçe içerik için region code

                searchListRequest.RelevanceLanguage = "tr";

                var searchListResponse = await searchListRequest.ExecuteAsync();

                var results = new List<YouTubeSearchResult>();

                foreach (var searchResult in searchListResponse.Items)
                {
                    results.Add(new YouTubeSearchResult
                    {
                        VideoId = searchResult.Id.VideoId,
                        Title = searchResult.Snippet.Title,
                        Description = searchResult.Snippet.Description,
                        ChannelTitle = searchResult.Snippet.ChannelTitle,
                        ThumbnailUrl = searchResult.Snippet.Thumbnails.Medium?.Url ??
                                     searchResult.Snippet.Thumbnails.Default__?.Url,
                        PublishedAt = searchResult.Snippet.PublishedAt ?? DateTime.MinValue
                    });
                }

                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"YouTube API Error: {ex.Message}");
                return new List<YouTubeSearchResult>();
            }
        }

        public async Task<string> GetBestVideoForTopicAsync(string topic)
        {
            var videos = await SearchVideosAsync(topic, 1);
            if (videos.Any())
            {
                return $"https://www.youtube.com/watch?v={videos.First().VideoId}";
            }
            return $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(topic)}";
        }
    }
}