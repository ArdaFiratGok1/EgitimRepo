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
        private readonly AIAgentContentService _contentService;

        public AIAgentController(GeminiApiService geminiService, AIAgentContentService contentService)
        {
            _geminiService = geminiService;
            _contentService = contentService;
        }

        
        public IActionResult Index()
        {
            return View(new AIAgentStartViewModel());
        }

        
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
                var request = new RoadmapGenerationRequest
                {
                    Topic = model.Topic,
                    UserLevel = model.UserLevel,
                    LearningGoal = model.LearningGoal,
                    FocusAreas = model.FocusAreas ?? new List<string>(),
                    MaxNodes = 12
                };

                var generatedRoadmap = await _geminiService.GenerateRoadmapAsync(request);
                var session = new AIAgentSession
                {
                    MainTopic = generatedRoadmap.MainTopic,
                    UserLevel = model.UserLevel
                };

                
                CreateLearningNodes(session, generatedRoadmap);

                
                SaveSession(session);//DB değil sessionlar üzerinden bir memory tutulacak.

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Yol haritası oluşturulurken hata oluştu: " + ex.Message);
                return View("Index", model);
            }
        }

        
        public async Task<IActionResult> Dashboard()
        {
            var session = GetSession();
            if (session == null)
            {
                return RedirectToAction("Index");
            }

            var viewModel = new AIAgentDashboardViewModel
            {
                Session = session,
                CompletedNodes = session.Nodes.Where(n => session.CompletedNodeIds.Contains(n.Id)).ToList(),
                AvailableNodes = GetAvailableNodes(session),
                CompletionPercentage = CalculateCompletionPercentage(session)
            };

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

        
        public async Task<IActionResult> NodeDetail(string nodeId)
        {
            var session = GetSession();
            if (session == null)
            {
                return RedirectToAction("Index");
            }

            var node = session.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null)
            {
                return NotFound();
            }

            //İçerik yoksa AIAgent'te yazdıgım fonksiyondan içerik üreitp onu çekiyorm
            if (string.IsNullOrEmpty(node.AIExplanation) ||
                string.IsNullOrEmpty(node.VideoUrl) ||
                string.IsNullOrEmpty(node.ArticleUrl))
            {
                try
                {
                    
                    node = await _contentService.EnrichNodeWithContentAsync(node, session.UserLevel);

                    
                    SaveSession(session);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Node enrichment error: {ex.Message}");
                }
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
        
        [HttpPost]
        public IActionResult CompleteNode(string nodeId)
        {
            var session = GetSession();
            if (session == null)
            {
                return Json(new { success = false, message = "Oturum bulunamadı." });
            }

            if (!session.CompletedNodeIds.Contains(nodeId))
            {
                session.CompletedNodeIds.Add(nodeId);
                session.LastAccessedAt = DateTime.Now;
                SaveSession(session);
            }

            return Json(new { success = true, message = "Konu tamamlandı." });
        }

        [HttpPost]
        public async Task<IActionResult> EnrichFullRoadmap()
        {
            var session = GetSession();
            if (session == null)
            {
                return BadRequest(new { error = "Oturum bulunamadı." });
            }

            var enrichedSession = await _contentService.EnrichSessionWithContentAsync(session);
            SaveSession(enrichedSession);

            return Ok(new { message = "Yol haritası içeriği başarıyla zenginleştirildi." });
        }

        [HttpGet]
        public async Task<IActionResult> GetContentSuggestions(string topic)//Eğitim maskotu önerilerini partial view
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                return BadRequest(new { error = "Konu başlığı boş olamaz." });
            }

            try
            {
                var suggestions = await _contentService.GetContentSuggestionsAsync(topic);
                return PartialView("_ContentSuggestionsPartial", suggestions); 
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        private AIAgentSession GetSession()
        {
            var sessionJson = HttpContext.Session.GetString("AIAgentSession");
            return string.IsNullOrEmpty(sessionJson) ? null : JsonSerializer.Deserialize<AIAgentSession>(sessionJson);
        }

        private void SaveSession(AIAgentSession session)
        {
            HttpContext.Session.SetString("AIAgentSession", JsonSerializer.Serialize(session));
        }

        private void CreateLearningNodes(AIAgentSession session, GeneratedRoadmap roadmap)
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
                        Level = 2, //Alt konu seviyesi
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

     
        private List<LearningNode> GetAvailableNodes(AIAgentSession session)
        {
            
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
            
            return session.Nodes.Where(n =>
                n.PrerequisiteIds.Contains(currentNode.Id)
            ).Take(3).ToList();
        }

        private bool CanAccessNode(AIAgentSession session, LearningNode node)
        {
            
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
                {"Temel Kavramlar", "#28a745"},      //yeşik
                {"Makine Öğrenmesi", "#007bff"},     //mavimsi
                {"Derin Öğrenme", "#dc3545"},        //kırmızı
                {"Uygulamalar", "#ffc107"},          //sarı-turuncu gibi
                {"İleri Konular", "#6f42c1"},        //mor
                {"Praktik", "#fd7e14"}               //Turuncu
            };

            return colors.ContainsKey(categoryName) ? colors[categoryName] : "#6c757d";
        }

        [HttpGet]
        public async Task<IActionResult> StartQuiz(string topic)
        {
            try
            {
                var session = GetSession();
                var userLevel = session?.UserLevel ?? "Başlangıç";

                var quizData = await _geminiService.GenerateQuizAsync(topic, userLevel, 10); // 10 soru olacak şekilde ayarladım.

                var quizSession = new QuizSession
                {
                    Topic = topic,
                    Questions = quizData.Questions
                };

                
                HttpContext.Session.SetString("QuizSession", JsonSerializer.Serialize(quizSession));//tüm sınavı session atıyorum

                return View("Quiz", quizSession);
            }
            catch (Exception ex)
            {
                
                TempData["ErrorMessage"] = "Sınav oluşturulurken bir hata oluştu: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }

        [HttpPost]
        public IActionResult SubmitQuiz(Dictionary<int, int> userAnswers, int timeSpentSeconds)
        {
            var quizJson = HttpContext.Session.GetString("QuizSession");
            if (string.IsNullOrEmpty(quizJson))
            {
                return RedirectToAction("Index");
            }

            var quizSession = JsonSerializer.Deserialize<QuizSession>(quizJson);
            quizSession.UserAnswers = userAnswers;
            quizSession.IsSubmitted = true;

            
            int correctAnswers = 0;
            for (int i = 0; i < quizSession.Questions.Count; i++)//çoktan seçmeli testt puanı
            {
                if (userAnswers.ContainsKey(i))
                {
                    int userAnswerIndex = userAnswers[i];
                    if (quizSession.Questions[i].AnswerOptions[userAnswerIndex].IsCorrect)
                    {
                        correctAnswers++;
                    }
                }
            }
            quizSession.Score = (correctAnswers * 100) / quizSession.Questions.Count;

            
            var roadmapSession = GetSession();
            if (roadmapSession != null)
            {
                var newQuizResult = new QuizHistory
                {
                    Topic = quizSession.Topic,
                    Score = quizSession.Score,
                    CorrectAnswers = correctAnswers,
                    TotalQuestions = quizSession.Questions.Count,
                    TimeSpentSeconds = timeSpentSeconds
                };

                
                roadmapSession.QuizHistory.Add(newQuizResult);

                SaveSession(roadmapSession);
            }
            HttpContext.Session.SetString("QuizSession", JsonSerializer.Serialize(quizSession));

            return View("QuizResult", quizSession);
        }

        public async Task<IActionResult> Progress()
        {
            var session = GetSession(); 
            if (session == null)
            {
                return RedirectToAction("Index");
            }

            var completedNodes = session.Nodes.Where(n => session.CompletedNodeIds.Contains(n.Id)).ToList();
            var totalTimeSpent = completedNodes.Sum(n => n.EstimatedMinutes); 

            //Gemini'den analiz metnini alıyorum
            string analysis = "İlerleme analizi alınamadı.";
            try
            {
                //İlerleme kısmı için geçici bir liste ayarladı m
                var userProgresses = completedNodes.Select(n => new UserProgress { TimeSpentMinutes = n.EstimatedMinutes }).ToList();
                analysis = await _geminiService.GenerateProgressAnalysisAsync(session, userProgresses);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Progress analysis error: {ex.Message}");
                analysis = "Harika bir ilerleme kaydettin, öğrenmeye devam et!";
            }

            var viewModel = new ProgressModel
            {
                Session = session,
                CompletedNodes = completedNodes,
                CompletionPercentage = CalculateCompletionPercentage(session), 
                ProgressAnalysis = analysis,
                TotalTimeSpentMinutes = totalTimeSpent,
                QuizHistory = session.QuizHistory.OrderByDescending(q => q.CompletedAt).ToList() 
            };

            return View(viewModel);
        }

    }
}