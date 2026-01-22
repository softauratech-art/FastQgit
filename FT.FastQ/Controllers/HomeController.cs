using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FT.FastQ.Models;
using FT.FastQ.Services;
using FT.FastQ.Helpers;
using NLog;

namespace FT.FastQ.Controllers
{
    public class HomeController : Controller
    {
        private static Logger _logger = LogManager.GetLogger("HomeController");
        private readonly AuthenticationService _authService;

        public HomeController()
        {
            _authService = new AuthenticationService();
        }

        // GET: Home
        public ActionResult Index()
        {
            var model = new LoginViewModel
            {
                ShowCodeVerification = false
            };
            return View(model);
        }

        // POST: Home/Continue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Continue(LoginViewModel model)
        {
            try
            {
                _logger.Info($"Continue - Email: {model.Email}");
                
                if (!ModelState.IsValid)
                {
                    _logger.Warn("Continue - ModelState invalid");
                    model.ShowCodeVerification = false;
                    model.EmailError = "Enter valid Email address";
                    return View("Index", model);
                }

                // Check if email exists in database
                if (_authService.EmailExistsInDatabase(model.Email))
                {
                    // Generate 6-digit verification code
                    ///TO DO --- GET THE VERIFICATION CODE FROM THE PROC
                    string verificationCode = _authService.GenerateVerificationCode();

                    // Store code in database with expiration
                    ////_authService.StoreVerificationCode(model.Email, verificationCode);

                    // Send email with verification code
                    _authService.SendVerificationEmail(model.Email, verificationCode);

                    // Show code verification section
                    model.ShowCodeVerification = true;
                    model.CodeMessage = $"{ResourceHelper.GetResourceString("WeSentCodeTo")} {model.Email}";
                    
                    // Store email and code in session for verification
                    Session["VerificationEmail"] = model.Email;
                    //Session["VerificationCode"] = verificationCode;

                    _logger.Info($"Continue - Verification code sent to {model.Email}");
                    return View("Index", model);
                }
                else
                {
                    _logger.Warn($"Continue - Email not found in database: {model.Email}");
                    model.ShowCodeVerification = false;
                    model.EmailError = "Email address not found in our system";
                    return View("Index", model);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Continue - Exception: {ex.Message} - {ex.StackTrace}");
                model.ShowCodeVerification = false;
                model.EmailError = "An error occurred. Please try again.";
                return View("Index", model);
            }
        }

        // POST: Home/VerifyCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyCode(string Email, string VerificationCode)
        {
            try
            {
                string email = Session["VerificationEmail"]?.ToString() ?? Email;
                _logger.Info($"VerifyCode - Email: {email}, Code length: {VerificationCode?.Length ?? 0}");

                if (string.IsNullOrEmpty(VerificationCode) || VerificationCode.Length != 6)
                {
                    _logger.Warn("VerifyCode - Invalid code length");
                    var errorModel = new LoginViewModel
                    {
                        Email = email,
                        ShowCodeVerification = true,
                        CodeMessage = $"{ResourceHelper.GetResourceString("WeSentCodeTo")} {email}",
                        CodeError = "Please enter the complete 6-digit code"
                    };
                    return View("Index", errorModel);
                }

                // Verify against database
                if (!_authService.VerifyCode(email, VerificationCode))
                {
                    _logger.Warn($"VerifyCode - Code verification failed for email: {email}");
                    var errorModel = new LoginViewModel
                    {
                        Email = email,
                        ShowCodeVerification = true,
                        CodeMessage = $"{ResourceHelper.GetResourceString("WeSentCodeTo")} {email}",
                        CodeError = "Verification code not matched. Please enter the valid code"
                    };
                    return View("Index", errorModel);
                }

                // Code is valid, store user email in session and clear verification data
                Session["UserEmail"] = email;
                Session.Remove("VerificationEmail");
                _logger.Info($"VerifyCode - Successfully verified and logged in user: {email}");
                return RedirectToAction("ApptsDashboard", "Appointment", new { email = email });
            }
            catch (Exception ex)
            {
                _logger.Error($"VerifyCode - Exception: {ex.Message} - {ex.StackTrace}");
                var errorModel = new LoginViewModel
                {
                    Email = Email,
                    ShowCodeVerification = true,
                    CodeMessage = $"{ResourceHelper.GetResourceString("WeSentCodeTo")} {Email}",
                    CodeError = "An error occurred. Please try again."
                };
                return View("Index", errorModel);
            }
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        // GET: Home/Logout
        public ActionResult Logout()
        {
            try
            {
                string email = Session["UserEmail"]?.ToString();
                _logger.Info($"Logout - User: {email}");
                
                // Clear all specific session values
                Session.Remove("UserEmail");
                Session.Remove("VerificationEmail");
                
                // Clear all remaining session values
                Session.Clear();
                
                // Abandon the session
                Session.Abandon();
                
                _logger.Info("Logout - Session cleared successfully");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.Error($"Logout - Exception: {ex.Message} - {ex.StackTrace}");
                return RedirectToAction("Index", "Home");
            }
        }
    }
}