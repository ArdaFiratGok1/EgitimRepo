namespace EgitimMaskotuApp.Models
{
    public class ProgressModel
    {
        public AIAgentSession Session { get; set; }
        public List<LearningNode> CompletedNodes { get; set; }
        public int CompletionPercentage { get; set; }
        public string ProgressAnalysis { get; set; } //Gemini'den gelecek analiz metni
        public int TotalTimeSpentMinutes { get; set; }
        public List<QuizHistory> QuizHistory { get; set; }

        
    }
}