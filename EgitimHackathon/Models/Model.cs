namespace EgitimMaskotuApp.Models
{
    //Münazara için modeller
    public class MunazaraSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Konu { get; set; }
        public string KullaniciTarafi { get; set; }
        public string AiTarafi { get; set; }
        public List<MunazaraMessage> Messages { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int TurnCount { get; set; } = 0;
        public int MaxTurns { get; set; } = 6;// 3'er mesaj olacagı içni
    }

    public class MunazaraMessage
    {
        public string Speaker { get; set; }//Kullanıcı veya Gemini
        public string Content { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class MunazaraResult
    {
        public string Winner { get; set; }
        public int UserScore { get; set; }
        public string Feedback { get; set; }
        public List<string> GoodPoints { get; set; } = new();
        public List<string> BadPoints { get; set; } = new();
        public string DetailedAnalysis { get; set; }
    }

    //öğretmen için modeller
    public class SokratikSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Konu { get; set; }
        public List<SokratikQA> QAHistory { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int QuestionCount { get; set; } = 0;
        public int MaxQuestions { get; set; } = 8;
    }

    public class SokratikQA
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public string TeacherFeedback { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    //Gemini API için modeller
    public class GeminiRequest
    {
        public List<GeminiContent> contents { get; set; } = new();
    }

    public class GeminiContent
    {
        public List<GeminiPart> parts { get; set; } = new();
    }

    public class GeminiPart
    {
        public string text { get; set; }
    }

    public class GeminiResponse
    {
        public List<GeminiCandidate> candidates { get; set; } = new();
    }

    public class GeminiCandidate
    {
        public GeminiContent content { get; set; }
    }

    //view tarafı için modeller:
    public class MunazaraStartViewModel
    {
        public string Konu { get; set; }
        public string KullaniciTarafi { get; set; }
        public string AiTarafi { get; set; }
    }

    public class MunazaraChatViewModel
    {
        public MunazaraSession Session { get; set; }
        public string NewMessage { get; set; }
    }

    public class SokratikStartViewModel
    {
        public string Konu { get; set; }
    }

    public class SokratikChatViewModel
    {
        public SokratikSession Session { get; set; }
        public string Answer { get; set; }
        public string CurrentQuestion { get; set; }
    }
}