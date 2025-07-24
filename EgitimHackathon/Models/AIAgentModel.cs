namespace EgitimMaskotuApp.Models
{
    public class AIAgentSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string MainTopic { get; set; } // Ana konu (örn: "Yapay Zeka")
        public string UserLevel { get; set; } = "Başlangıç"; // Başlangıç, Orta, İleri
        public List<LearningNode> Nodes { get; set; } = new();
        public List<string> CompletedNodeIds { get; set; } = new(); // Tamamlanan düğümler
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastAccessedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }

    // Öğrenme Düğümü (Her bir konuyu temsil eder)
    public class LearningNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } // Düğüm başlığı
        public string Category { get; set; } // Kategori (Temel, İleri vs.)
        public int Level { get; set; } = 1; // Seviye (1: Ana kategori, 2: Alt konu)
        public string ParentId { get; set; } // Üst düğüm ID'si
        public List<string> PrerequisiteIds { get; set; } = new(); // Önkoşul düğümler

        // İçerik
        public string AIExplanation { get; set; } // AI tarafından üretilen açıklama
        public string VideoUrl { get; set; } // YouTube video linki
        public string VideoTitle { get; set; } // Video başlığı
        public string ArticleUrl { get; set; } // Makale linki
        public string ArticleTitle { get; set; } // Makale başlığı
        public string ArticleSource { get; set; } // Makale kaynağı

        // Metadata
        public int EstimatedMinutes { get; set; } = 15; // Tahmini öğrenme süresi
        public string Difficulty { get; set; } = "Kolay"; // Kolay, Orta, Zor
        public List<string> Keywords { get; set; } = new(); // Anahtar kelimeler
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // UI için pozisyon bilgileri (opsiyonel)
        public NodePosition Position { get; set; } = new();
    }

    // Düğüm pozisyon bilgisi (UI'da gösterim için)
    public class NodePosition
    {
        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;
        public string Color { get; set; } = "#007bff"; // Bootstrap primary
    }

    // Kullanıcı Progress Takibi
    public class UserProgress
    {
        public string UserId { get; set; } // Gelecekte kullanıcı sistemi için
        public string SessionId { get; set; }
        public string NodeId { get; set; }
        public bool IsCompleted { get; set; } = false;
        public bool IsBookmarked { get; set; } = false;
        public int TimeSpentMinutes { get; set; } = 0;
        public DateTime CompletedAt { get; set; }
        public DateTime LastAccessedAt { get; set; } = DateTime.Now;
        public string Notes { get; set; } = ""; // Kullanıcı notları
        public int Rating { get; set; } = 0; // 1-5 arası değerlendirme
    }

    // Yol Haritası Üretim İsteği
    public class RoadmapGenerationRequest
    {
        public string Topic { get; set; }
        public string UserLevel { get; set; } = "Başlangıç";
        public string LearningGoal { get; set; } = "Genel"; // Genel, Sınav, Proje vs.
        public int MaxNodes { get; set; } = 12; // Maksimum düğüm sayısı
        public List<string> FocusAreas { get; set; } = new(); // Odaklanılacak alanlar
    }

    // API'den dönen yol haritası yapısı
    public class GeneratedRoadmap
    {
        public string MainTopic { get; set; }
        public string Description { get; set; }
        public List<RoadmapCategory> Categories { get; set; } = new();
        public int EstimatedTotalHours { get; set; }
        public string Difficulty { get; set; }
    }

    public class RoadmapCategory
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public List<RoadmapTopic> Topics { get; set; } = new();
    }

    public class RoadmapTopic
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public string Difficulty { get; set; }
        public int EstimatedMinutes { get; set; }
        public List<string> Keywords { get; set; } = new();
    }

    // View Models
    public class AIAgentStartViewModel
    {
        public string Topic { get; set; }
        public string UserLevel { get; set; } = "Başlangıç";
        public string LearningGoal { get; set; } = "Genel";
        public List<string> FocusAreas { get; set; } = new();
    }

    public class AIAgentDashboardViewModel
    {
        public AIAgentSession Session { get; set; }
        public List<LearningNode> AvailableNodes { get; set; } = new(); // Açık düğümler
        public List<LearningNode> CompletedNodes { get; set; } = new(); // Tamamlanan düğümler
        public int CompletionPercentage { get; set; }
        public int TotalEstimatedHours { get; set; }
        public string CurrentRecommendation { get; set; } // AI önerisi
    }

    public class NodeDetailViewModel
    {
        public AIAgentSession Session { get; set; }
        public LearningNode CurrentNode { get; set; }
        public List<LearningNode> Prerequisites { get; set; } = new(); // Önkoşullar
        public List<LearningNode> NextNodes { get; set; } = new(); // Sonraki öneriler
        public UserProgress Progress { get; set; }
        public bool CanAccess { get; set; } = true; // Önkoşullar tamamlandı mı?
    }
}
