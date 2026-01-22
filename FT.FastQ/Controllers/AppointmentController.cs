using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using FT.FastQ.Models;
using FT.FastQ.Services;
using NLog;

namespace FT.FastQ.Controllers
{
    public class AppointmentController : Controller
    {
        private static Logger _logger = LogManager.GetLogger("AppointmentController");
        private readonly AppointmentService _appointmentService;

        public AppointmentController()
        {
            _appointmentService = new AppointmentService();
        }

        // GET: Appointment/ApptsDashboard
        public ActionResult ApptsDashboard(string viewType = "Upcoming", int page = 1, string sortBy = "AppointmentDateTime", string sortDirection = "ASC")
        {
            try
            {
                _logger.Info($"ApptsDashboard - viewType: {viewType}, page: {page}, sortBy: {sortBy}, sortDirection: {sortDirection}");
                
                string email = Session["UserEmail"]?.ToString();
                if (string.IsNullOrEmpty(email))
                {
                    _logger.Warn("ApptsDashboard - User not authenticated, redirecting to Home");
                    return RedirectToAction("Index", "Home");
                }

            int pageSize = 10;
            var model = new ApptsDashboardViewModel
            {
                Email = email,
                ViewType = viewType ?? "Upcoming",
                CurrentPage = page,
                PageSize = pageSize,
                SortBy = sortBy ?? "AppointmentDateTime",
                SortDirection = sortDirection ?? "ASC"
            };

            // Fetch all appointments for the user from database
            var allAppointments = _appointmentService.GetAppointmentsByEmail(email);
            DateTime now = DateTime.Now;

            // Clear any test data from session
            Session["TestAppointments"] = null;
            Session["TestCancelReasons"] = null;

            List<Appointment> filteredAppointments;

            // Filter based on view type
            if (viewType == "History")
            {
                filteredAppointments = allAppointments
                    .Where(a => a.AppointmentDateTime < now)
                    .ToList();
            }
            else // Upcoming
            {
                filteredAppointments = allAppointments
                    .Where(a => a.AppointmentDateTime >= now)
                    .ToList();
            }

            // Apply sorting
            filteredAppointments = ApplySorting(filteredAppointments, sortBy, sortDirection);

            // Calculate pagination
            model.TotalRecords = filteredAppointments.Count;
            model.TotalPages = (int)Math.Ceiling((double)model.TotalRecords / pageSize);

            // Apply pagination
            model.Appointments = filteredAppointments
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

                _logger.Info($"ApptsDashboard - Completed successfully. Total records: {model.TotalRecords}, Displayed: {model.Appointments.Count}");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.Error($"ApptsDashboard - Exception: {ex.Message} - {ex.StackTrace}");
                throw;
            }
        }

        private List<Appointment> ApplySorting(List<Appointment> appointments, string sortBy, string sortDirection)
        {
            bool isAscending = sortDirection == "ASC";

            switch (sortBy)
            {
                case "AppointmentDateTime":
                    return isAscending
                        ? appointments.OrderBy(a => a.AppointmentDateTime).ToList()
                        : appointments.OrderByDescending(a => a.AppointmentDateTime).ToList();

                case "Queue":
                    return isAscending
                        ? appointments.OrderBy(a => a.Queue).ToList()
                        : appointments.OrderByDescending(a => a.Queue).ToList();

                case "Type":
                    return isAscending
                        ? appointments.OrderBy(a => a.Type).ToList()
                        : appointments.OrderByDescending(a => a.Type).ToList();

                case "Status":
                    return isAscending
                        ? appointments.OrderBy(a => a.Status).ToList()
                        : appointments.OrderByDescending(a => a.Status).ToList();

                default:
                    return appointments.OrderBy(a => a.AppointmentDateTime).ToList();
            }
        }


        // GET: Appointment/CancelAppt
        public ActionResult CancelAppt(int appointmentId)
        {
            try
            {
                _logger.Info($"CancelAppt - appointmentId: {appointmentId}");
                
                string email = Session["UserEmail"]?.ToString();
                if (string.IsNullOrEmpty(email))
                {
                    _logger.Warn("CancelAppt - User not authenticated, redirecting to Home");
                    return RedirectToAction("Index", "Home");
                }

                // Get appointment from database
                var appointment = _appointmentService.GetAppointmentById(appointmentId, email);
                if (appointment == null)
                {
                    _logger.Warn($"CancelAppt - Appointment not found for appointmentId: {appointmentId}, email: {email}");
                    return RedirectToAction("ApptsDashboard");
                }

            string cancelReason = "";
            if (appointment.Status == "Cancelled")
            {
                // Get cancel reason from database
                cancelReason = _appointmentService.GetCancelReason(appointmentId, email);
            }

            var model = new CancelAppointmentViewModel
            {
                AppointmentId = appointment.Id,
                AppointmentDateTime = appointment.AppointmentDateTime,
                Service = appointment.Queue,
                ContactBy = appointment.Email,
                Status = appointment.Status,
                Reason = cancelReason,
                IsCancelled = appointment.Status == "Cancelled"
                };

                _logger.Info($"CancelAppt - Completed successfully for appointmentId: {appointmentId}");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.Error($"CancelAppt - Exception: {ex.Message} - {ex.StackTrace}");
                throw;
            }
        }

        // POST: Appointment/CancelAppointment
        [HttpPost]
        public ActionResult CancelAppointment(int appointmentId, string reason)
        {
            try
            {
                _logger.Info($"CancelAppointment - appointmentId: {appointmentId}, reason provided: {!string.IsNullOrEmpty(reason)}");
                
                string email = Session["UserEmail"]?.ToString();
                if (string.IsNullOrEmpty(email))
                {
                    _logger.Warn("CancelAppointment - User not authenticated");
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Update appointment status in database
                bool success = _appointmentService.CancelAppointment(appointmentId, email, reason);
                if (success)
                {
                    // Refresh appointment data from database
                    var appointment = _appointmentService.GetAppointmentById(appointmentId, email);
                    _logger.Info($"CancelAppointment - Successfully cancelled appointmentId: {appointmentId}");
                    return Json(new { 
                        success = true, 
                        message = "Appointment cancelled successfully",
                        status = appointment?.Status ?? "Cancelled"
                    });
                }
                else
                {
                    _logger.Warn($"CancelAppointment - Failed to cancel appointmentId: {appointmentId}");
                    return Json(new { success = false, message = "Failed to cancel appointment" });
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"CancelAppointment - Exception: {ex.Message} - {ex.StackTrace}");
                return Json(new { success = false, message = "An error occurred while cancelling the appointment" });
            }
        }

        // GET: Appointment/NewAppt
        public ActionResult NewAppt()
        {
            ViewBag.Email = Session["UserEmail"]?.ToString();
            var model = new NewAppointmentViewModel();
            return View(model);
        }

        // POST: Appointment/CreateAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAppointment(NewAppointmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Save appointment logic here
                // For now, redirect back to dashboard
                return RedirectToAction("ApptsDashboard");
            }
            
            ViewBag.Email = Session["UserEmail"]?.ToString();
            return View("NewAppt", model);
        }
    }
}

