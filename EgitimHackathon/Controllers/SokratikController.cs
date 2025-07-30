using Microsoft.AspNetCore.Mvc;
using EgitimMaskotuApp.Models;
using EgitimMaskotuApp.Services;
using System.Text.Json;

namespace EgitimMaskotuApp.Controllers
{
    public class SokratikController : Controller
    {
        private readonly GeminiApiService _geminiService;

        public SokratikController(GeminiApiService geminiService)
        {
            _geminiService = geminiService;
        }

        public IActionResult Index()
        {
            return View(new SokratikStartViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Start(SokratikStartViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Konu))
            {
                ModelState.AddModelError("Konu", "Lütfen bir konu girin.");
                return View("Index", model);
            }

            try
            {
                var firstQuestion = await _geminiService.GenerateFirstSokratikQuestionAsync(model.Konu);

                var session = new SokratikSession
                {
                    Konu = model.Konu
                };

                HttpContext.Session.SetString("SokratikSession", JsonSerializer.Serialize(session));

                var viewModel = new SokratikChatViewModel
                {
                    Session = session,
                    CurrentQuestion = firstQuestion
                };

                return View("Chat", viewModel);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Oturum başlatılırken bir hata oluştu: " + ex.Message);
                return View("Index", model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitAnswer(string answer)
        {
            var sessionJson = HttpContext.Session.GetString("SokratikSession");
            if (string.IsNullOrEmpty(sessionJson))
            {
                return RedirectToAction("Index");
            }

            var session = JsonSerializer.Deserialize<SokratikSession>(sessionJson);

            if (!session.IsActive || session.QuestionCount >= session.MaxQuestions)
            {
                return RedirectToAction("Summary");
            }

            try
            {
                string currentQuestion = Request.Form["currentQuestion"];

                if (string.IsNullOrEmpty(currentQuestion))
                {
                    currentQuestion = session.QAHistory.LastOrDefault()?.Question ?? "Soru bulunamadı";
                }

                var (nextQuestion, feedback) = await _geminiService.GenerateNextSokratikAsync(session, answer);

                session.QAHistory.Add(new SokratikQA
                {
                    Question = currentQuestion,
                    Answer = answer,
                    TeacherFeedback = feedback
                });

                session.QuestionCount++;

                if (nextQuestion == "BİTTİ" || session.QuestionCount >= session.MaxQuestions)
                {
                    session.IsActive = false;
                    HttpContext.Session.SetString("SokratikSession", JsonSerializer.Serialize(session));
                    return RedirectToAction("Summary");
                }

                HttpContext.Session.SetString("SokratikSession", JsonSerializer.Serialize(session));

                var viewModel = new SokratikChatViewModel
                {
                    Session = session,
                    CurrentQuestion = nextQuestion
                };

                return View("Chat", viewModel);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Cevap işlenirken hata oluştu: " + ex.Message);

                var viewModel = new SokratikChatViewModel
                {
                    Session = session,
                    CurrentQuestion = "Teknik bir sorun oluştu. Lütfen tekrar deneyin."
                };

                return View("Chat", viewModel);
            }
        }

        public IActionResult Summary()
        {
            var sessionJson = HttpContext.Session.GetString("SokratikSession");
            if (string.IsNullOrEmpty(sessionJson))
            {
                return RedirectToAction("Index");
            }

            var session = JsonSerializer.Deserialize<SokratikSession>(sessionJson);
            return View(session);
        }

        public IActionResult NewSession()
        {
            HttpContext.Session.Remove("SokratikSession");
            return RedirectToAction("Index");
        }
    }
}