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

                // Düğümleri oluştur (Bu metot artık sadece iskeleti oluşturuyor)
                CreateLearningNodes(session, generatedRoadmap);

                // Session'ı kaydet
                SaveSession(session);

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

        // Düğüm detayı
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

            // Eğer içerik henüz üretilmemişse, AIAgentContentService'i kullanarak üret.
            if (string.IsNullOrEmpty(node.AIExplanation) ||
                string.IsNullOrEmpty(node.VideoUrl) ||
                string.IsNullOrEmpty(node.ArticleUrl))
            {
                try
                {
                    // === DEĞİŞİKLİK BURADA: Eksik olan 'session.UserLevel' parametresi eklendi ===
                    node = await _contentService.EnrichNodeWithContentAsync(node, session.UserLevel);

                    // Session'ı güncelle
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
        // Düğümü tamamla
        [HttpPost]
        public IActionResult CompleteNode(string nodeId)
        {
            var session = GetSession();
            if (session == null)
            {
                // Session yoksa hata döndür veya Index'e yönlendir.
                // API çağrısı için JSON hatası daha uygun olabilir.
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
        public async Task<IActionResult> GetContentSuggestions(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                return BadRequest(new { error = "Konu başlığı boş olamaz." });
            }

            try
            {
                var suggestions = await _contentService.GetContentSuggestionsAsync(topic);
                return PartialView("_ContentSuggestionsPartial", suggestions); // Önerileri bir partial view ile döndür
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

        [HttpGet]
        public async Task<IActionResult> StartQuiz(string topic)
        {
            try
            {
                var session = GetSession();
                var userLevel = session?.UserLevel ?? "Başlangıç";

                var quizData = await _geminiService.GenerateQuizAsync(topic, userLevel, 10); // 10 soruluk bir sınav, istersen değişiyo

                var quizSession = new QuizSession
                {
                    Topic = topic,
                    Questions = quizData.Questions
                };

                // Sınavı HTTP Session'a kaydediyoruz
                HttpContext.Session.SetString("QuizSession", JsonSerializer.Serialize(quizSession));

                return View("Quiz", quizSession);
            }
            catch (Exception ex)
            {
                // Hata durumunda kullanıcıyı dashboard'a geri yönlendir
                TempData["ErrorMessage"] = "Sınav oluşturulurken bir hata oluştu: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }

        // Kullanıcının sınav cevaplarını alır ve sonucu gösterir
        [HttpPost]
        public IActionResult SubmitQuiz(Dictionary<int, int> userAnswers, int timeSpentSeconds)
        {
            var quizJson = HttpContext.Session.GetString("QuizSession");
            if (string.IsNullOrEmpty(quizJson))
            {
                return RedirectToAction("Index"); // Session yoksa başa dön
            }

            var quizSession = JsonSerializer.Deserialize<QuizSession>(quizJson);
            quizSession.UserAnswers = userAnswers;
            quizSession.IsSubmitted = true;

            // Puanı hesapla
            int correctAnswers = 0;
            for (int i = 0; i < quizSession.Questions.Count; i++)
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

            //
            var roadmapSession = GetSession();
            if (roadmapSession != null)
            {
                // 2. Yeni bir test sonucu nesnesi oluştur
                var newQuizResult = new QuizHistory
                {
                    Topic = quizSession.Topic,
                    Score = quizSession.Score,
                    CorrectAnswers = correctAnswers,
                    TotalQuestions = quizSession.Questions.Count,
                    TimeSpentSeconds = timeSpentSeconds
                };

                // 3. Bu sonucu yol haritası session'ındaki listeye ekle
                roadmapSession.QuizHistory.Add(newQuizResult);

                // 4. Güncellenmiş yol haritası session'ını kaydet
                SaveSession(roadmapSession);
            }
            //
            // Sonucu session'a geri kaydet
            HttpContext.Session.SetString("QuizSession", JsonSerializer.Serialize(quizSession));

            return View("QuizResult", quizSession);
        }

        public async Task<IActionResult> Progress()
        {
            var session = GetSession(); // Zaten var olan yardımcı metodun
            if (session == null)
            {
                return RedirectToAction("Index");
            }

            // Gerçek uygulamada bu veriler veritabanından gelir. Şimdilik session'dan alıyoruz.
            var completedNodes = session.Nodes.Where(n => session.CompletedNodeIds.Contains(n.Id)).ToList();
            var totalTimeSpent = completedNodes.Sum(n => n.EstimatedMinutes); // Basit bir tahmin

            // Gemini'den analiz metni al
            string analysis = "İlerleme analizi alınamadı.";
            try
            {
                // Bu metodu daha önce oluşturmuştuk, şimdi kullanıyoruz.
                // `userProgresses` için geçici bir liste oluşturalım.
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
                CompletionPercentage = CalculateCompletionPercentage(session), // Zaten var olan yardımcı metodun
                ProgressAnalysis = analysis,
                TotalTimeSpentMinutes = totalTimeSpent,
                QuizHistory = session.QuizHistory.OrderByDescending(q => q.CompletedAt).ToList() // En yeniden eskiye sırala
            };

            return View(viewModel); // Yeni oluşturacağımız Progress.cshtml'e modeli gönder
        }

    }
}