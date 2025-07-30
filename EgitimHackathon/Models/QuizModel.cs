namespace EgitimMaskotuApp.Models
{
    //Sınavın tamamını ve kullanıcının cevaplarını tutan model
    public class QuizSession
    {
        public string Topic { get; set; }
        public List<QuizQuestion> Questions { get; set; } = new();
        public Dictionary<int, int> UserAnswers { get; set; } = new(); //Soru Index == Cevap Index olacak
        public int Score { get; set; } = 0;
        public bool IsSubmitted { get; set; } = false;
    }

    public class QuizGenerationResponse
    {
        public List<QuizQuestion> Questions { get; set; }
    }

    public class QuizQuestion
    {
        public string Question { get; set; }
        public List<AnswerOption> AnswerOptions { get; set; }
        public string Hint { get; set; }
    }

    public class AnswerOption
    {
        public string Text { get; set; }
        public string Rationale { get; set; } //Cevabın neden doğru/yanlış olduğunun açıklaması
        public bool IsCorrect { get; set; }
    }

    public class QuizHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid(); 
        public string Topic { get; set; }
        public int Score { get; set; } //100 üzerinden yaparım herhalde
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.Now;

        public int TimeSpentSeconds { get; set; }
    }
}