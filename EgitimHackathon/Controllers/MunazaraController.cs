using Microsoft.AspNetCore.Mvc;
using EgitimMaskotuApp.Models;
using EgitimMaskotuApp.Services;
using System.Text.Json;

namespace EgitimMaskotuApp.Controllers
{
    public class MunazaraController : Controller
    {
        private readonly GeminiApiService _geminiService;

        public MunazaraController(GeminiApiService geminiService)
        {
            _geminiService = geminiService;
        }

        public IActionResult Index()
        {
            return View(new MunazaraStartViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Start(MunazaraStartViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Konu))
            {
                ModelState.AddModelError("Konu", "Lütfen bir konu girin.");
                return View("Index", model);
            }

            try
            {
                // Gemini API ile konu analizi
                var (konu, kullaniciTarafi, aiTarafi) = await _geminiService.AnalyzeMunazaraTopicAsync(model.Konu);

                var session = new MunazaraSession
                {
                    Konu = konu,
                    KullaniciTarafi = kullaniciTarafi,
                    AiTarafi = aiTarafi
                };

                // Session'a kaydet
                HttpContext.Session.SetString("MunazaraSession", JsonSerializer.Serialize(session));

                return View("Confirm", session);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Konu analiz edilirken bir hata oluştu: " + ex.Message);
                return View("Index", model);
            }
        }

        [HttpPost]
        public IActionResult ConfirmStart()
        {
            var sessionJson = HttpContext.Session.GetString("MunazaraSession");
            if (string.IsNullOrEmpty(sessionJson))
            {
                return RedirectToAction("Index");
            }

            var session = JsonSerializer.Deserialize<MunazaraSession>(sessionJson);
            return View("Chat", new MunazaraChatViewModel { Session = session });
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(string message)
        {
            var sessionJson = HttpContext.Session.GetString("MunazaraSession");
            if (string.IsNullOrEmpty(sessionJson))
            {
                return RedirectToAction("Index");
            }

            var session = JsonSerializer.Deserialize<MunazaraSession>(sessionJson);

            if (!session.IsActive)
            {
                return View("Chat", new MunazaraChatViewModel { Session = session });
            }

            try
            {
                // Kullanıcı mesajını ekle
                session.Messages.Add(new MunazaraMessage
                {
                    Speaker = "user",
                    Content = message
                });

                session.TurnCount++;

                // AI yanıtı al (münazara bitmeden önce)
                var aiResponse = await _geminiService.GenerateMunazaraResponseAsync(session, message);

                // AI yanıtını ekle
                session.Messages.Add(new MunazaraMessage
                {
                    Speaker = "ai",
                    Content = aiResponse
                });

                session.TurnCount++;

                // Eğer maksimum turn sayısına ulaştıysak münazarayı bitir
                if (session.TurnCount >= session.MaxTurns)
                {
                    session.IsActive = false;
                }

                // Session'ı güncelle
                HttpContext.Session.SetString("MunazaraSession", JsonSerializer.Serialize(session));

                // Chat sayfasını göster (münazara bitmiş olsa bile, kullanıcı son yanıtı okuyabilsin)
                return View("Chat", new MunazaraChatViewModel { Session = session });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Mesaj gönderilirken hata oluştu: " + ex.Message);
                return View("Chat", new MunazaraChatViewModel { Session = session });
            }
        }

        // Münazarayı bitir butonuna basıldığında
        [HttpPost]
        public IActionResult FinishMunazara()
        {
            var sessionJson = HttpContext.Session.GetString("MunazaraSession");
            if (string.IsNullOrEmpty(sessionJson))
            {
                return RedirectToAction("Index");
            }

            var session = JsonSerializer.Deserialize<MunazaraSession>(sessionJson);
            session.IsActive = false;
            HttpContext.Session.SetString("MunazaraSession", JsonSerializer.Serialize(session));
            
            return View("Finished", new MunazaraChatViewModel { Session = session });
        }

        // Yeni action: Münazara bitişini göster
        public IActionResult Finished()
        {
            var sessionJson = HttpContext.Session.GetString("MunazaraSession");
            if (string.IsNullOrEmpty(sessionJson))
            {
                return RedirectToAction("Index");
            }

            var session = JsonSerializer.Deserialize<MunazaraSession>(sessionJson);
            return View(new MunazaraChatViewModel { Session = session });
        }

        // Sonuç sayfasına gitme action'ı
        [HttpPost]
        public IActionResult GoToResult()
        {
            return RedirectToAction("Result");
        }

        public async Task<IActionResult> Result()
        {
            var sessionJson = HttpContext.Session.GetString("MunazaraSession");
            if (string.IsNullOrEmpty(sessionJson))
            {
                return RedirectToAction("Index");
            }

            var session = JsonSerializer.Deserialize<MunazaraSession>(sessionJson);

            try
            {
                // Münazara sonucunu değerlendir
                var result = await _geminiService.EvaluateMunazaraAsync(session);

                ViewBag.Session = session;
                return View(result);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Sonuç değerlendirilirken hata oluştu: " + ex.Message;
                ViewBag.Session = session;
                return View(new MunazaraResult());
            }
        }

        public IActionResult NewMunazara()
        {
            HttpContext.Session.Remove("MunazaraSession");
            return RedirectToAction("Index");
        }
    }
}