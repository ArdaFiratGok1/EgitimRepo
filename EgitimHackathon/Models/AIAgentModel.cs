namespace EgitimMaskotuApp.Models
{
    public class AIAgentSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string MainTopic { get; set; }// kullanıcının yazacağı konu
        public string UserLevel { get; set; } = "Başlangıç"; //Başlangıç, Orta, İleri, default başlangıç olacak
        public List<LearningNode> Nodes { get; set; } = new();
        public List<string> CompletedNodeIds { get; set; } = new(); 
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastAccessedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public List<QuizHistory> QuizHistory { get; set; } = new();
    }



   
    public class LearningNode//modül
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } 
        public string Category { get; set; } 
        public int Level { get; set; } = 1; 
        public string ParentId { get; set; } 
        public List<string> PrerequisiteIds { get; set; } = new(); 

        
        public string AIExplanation { get; set; }
        public string VideoUrl { get; set; } //Youtube API'dan çekeceğim
        public string VideoTitle { get; set; } 
        public string ArticleUrl { get; set; } //Core API'den çekeceğim
        public string ArticleTitle { get; set; } 
        public string ArticleSource { get; set; } 

        public int EstimatedMinutes { get; set; } = 15; 
        public string Difficulty { get; set; } = "Kolay"; //Kolay, Orta, Zor
        public List<string> Keywords { get; set; } = new(); 
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public NodePosition Position { get; set; } = new();
    }

    public class NodePosition
    {
        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;
        public string Color { get; set; } = "#007bff"; //Bootstrap primary
    }

  
    public class UserProgress
    {
        public string UserId { get; set; } 
        public string SessionId { get; set; }
        public string NodeId { get; set; }
        public bool IsCompleted { get; set; } = false;
        public bool IsBookmarked { get; set; } = false;
        public int TimeSpentMinutes { get; set; } = 0;
        public DateTime CompletedAt { get; set; }
        public DateTime LastAccessedAt { get; set; } = DateTime.Now;
        public string Notes { get; set; } = ""; 
        public int Rating { get; set; } = 0; 
    }


    public class RoadmapGenerationRequest
    {
        public string Topic { get; set; }
        public string UserLevel { get; set; } = "Başlangıç";
        public string LearningGoal { get; set; } = "Genel"; //Genel, Sınav, Proje vs.
        public int MaxNodes { get; set; } = 12; //!!!!Modül sayısı max 12 yaptım, duruma göre değiştirebilirim!!!!!
        public List<string> FocusAreas { get; set; } = new();
    }

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
        public List<LearningNode> AvailableNodes { get; set; } = new(); 
        public List<LearningNode> CompletedNodes { get; set; } = new(); 
        public int CompletionPercentage { get; set; }
        public int TotalEstimatedHours { get; set; }
        public string CurrentRecommendation { get; set; } 
    }

    public class NodeDetailViewModel
    {
        public AIAgentSession Session { get; set; }
        public LearningNode CurrentNode { get; set; }
        public List<LearningNode> Prerequisites { get; set; } = new(); 
        public List<LearningNode> NextNodes { get; set; } = new(); 
        public UserProgress Progress { get; set; }
        public bool CanAccess { get; set; } = true; 
    }
}
