using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Web;

namespace FT.FastQ.Helpers
{
    public static class ResourceHelper
    {
        private static Dictionary<string, Dictionary<string, string>> _resources = new Dictionary<string, Dictionary<string, string>>
        {
            {
                "en", new Dictionary<string, string>
                {
                    { "AppointmentsDashboard", "My Appointments" },
                    { "NewAppointment", "New Appointment" },
                    { "ManageProfile", "Manage Profile" },
                    { "Email", "Email" },
                    { "Language", "Language" },
                    { "English", "English" },
                    { "Spanish", "Spanish" },
                    { "Creole", "Creole" },
                    { "Login", "Login" },
                    { "Continue", "Continue" },
                    { "VerifyCode", "Verify Code" },
                    { "VerificationCode", "Verification Code" },
                    { "WeSentCodeTo", "We sent a code to" },
                    { "Logout", "Logout" },
                    { "ServiceProvider", "Service Provider" },
                    { "ServiceType", "Service Type" },
                    { "AppointmentDate", "Appointment Date" },
                    { "AppointmentTime", "Appointment Time" },
                    { "AppointmentType", "Appointment Type" },
                    { "Reference", "Reference" },
                    { "Enter", "Enter" },
                    { "Validate", "Validate" },
                    { "Comments", "Comments" },
                    { "SeeMeetingGuidelines", "See Meeting Guidelines" },
                    { "CreateAppointment", "Create Appointment" },
                    { "Select", "--Select--" },
                    { "SelectAppointmentDate", "--Select Appointment Date--" },
                    { "Upcoming", "Upcoming" },
                    { "History", "History" },
                    { "DateAndTime", "Date & Time" },
                    { "Queue", "Queue" },
                    { "Type", "Type" },
                    { "Status", "Status" },
                    { "Cancel", "Cancel" },
                    { "Scheduled", "Scheduled" },
                    { "Cancelled", "Cancelled" },
                    { "CancelAppointment", "Cancel Appointment" },
                    { "Service", "Service" },
                    { "ContactBy", "Contact By" },
                    { "Reason", "Reason" },
                    { "SelectFromList", "Select from list" },
                    { "ScheduleConflict", "Schedule Conflict" },
                    { "NoLongerNeeded", "No Longer Needed" },
                    { "FoundAlternative", "Found Alternative" },
                    { "Other", "Other" },
                    { "Close", "Close" },
                    { "Confirm", "Confirm" },
                    { "ConfirmCancelMessage", "Are you sure you want to cancel this appointment?" },
                    { "No", "No" },
                    { "Yes", "Yes" },
                    { "SelectReasonValidation", "Select the reason for the cancellation" },
                    { "Back", "Back" }
                }
            },
            {
                "es", new Dictionary<string, string>
                {
                    { "AppointmentsDashboard", "Mis Citas" },
                    { "NewAppointment", "Nueva Cita" },
                    { "ManageProfile", "Gestionar Perfil" },
                    { "Email", "Correo Electrónico" },
                    { "Language", "Idioma" },
                    { "English", "Inglés" },
                    { "Spanish", "Español" },
                    { "Creole", "Creole" },
                    { "Login", "Iniciar Sesión" },
                    { "Continue", "Continuar" },
                    { "VerifyCode", "Verificar Código" },
                    { "VerificationCode", "Código de Verificación" },
                    { "WeSentCodeTo", "Enviamos un código a" },
                    { "Logout", "Cerrar Sesión" },
                    { "ServiceProvider", "Proveedor de Servicio" },
                    { "ServiceType", "Tipo de Servicio" },
                    { "AppointmentDate", "Fecha de Cita" },
                    { "AppointmentTime", "Hora de Cita" },
                    { "AppointmentType", "Tipo de Cita" },
                    { "Reference", "Referencia" },
                    { "Enter", "Ingresar" },
                    { "Validate", "Validar" },
                    { "Comments", "Comentarios" },
                    { "SeeMeetingGuidelines", "Ver Pautas de Reunión" },
                    { "CreateAppointment", "Crear Cita" },
                    { "Select", "--Seleccionar--" },
                    { "SelectAppointmentDate", "--Seleccionar Fecha de Cita--" },
                    { "Upcoming", "Próximos" },
                    { "History", "Historial" },
                    { "DateAndTime", "Fecha y Hora" },
                    { "Queue", "Cola" },
                    { "Type", "Tipo" },
                    { "Status", "Estado" },
                    { "Cancel", "Cancelar" },
                    { "Scheduled", "Programado" },
                    { "Cancelled", "Cancelado" },
                    { "CancelAppointment", "Cancelar Cita" },
                    { "Service", "Servicio" },
                    { "ContactBy", "Contacto Por" },
                    { "Reason", "Razón" },
                    { "SelectFromList", "Seleccionar de la lista" },
                    { "ScheduleConflict", "Conflicto de Horario" },
                    { "NoLongerNeeded", "Ya No Necesario" },
                    { "FoundAlternative", "Encontré Alternativa" },
                    { "Other", "Otro" },
                    { "Close", "Cerrar" },
                    { "Confirm", "Confirmar" },
                    { "ConfirmCancelMessage", "¿Está seguro de que desea cancelar esta cita?" },
                    { "No", "No" },
                    { "Yes", "Sí" },
                    { "SelectReasonValidation", "Seleccione la razón de la cancelación" },
                    { "Back", "Atrás" }
                }
            },
            {
                "ht", new Dictionary<string, string>
                {
                    { "AppointmentsDashboard", "Appointman Mwen" },
                    { "NewAppointment", "Nouvo Appointman" },
                    { "ManageProfile", "Jere Pwofil" },
                    { "Email", "Imèl" },
                    { "Language", "Lang" },
                    { "English", "Angle" },
                    { "Spanish", "Panyòl" },
                    { "Creole", "Kreyòl" },
                    { "Login", "Konekte" },
                    { "Continue", "Kontinye" },
                    { "VerifyCode", "Verifye Kòd" },
                    { "VerificationCode", "Kòd Verifikasyon" },
                    { "WeSentCodeTo", "Nou voye yon kòd nan" },
                    { "Logout", "Dekonekte" },
                    { "ServiceProvider", "Founisè Sèvis" },
                    { "ServiceType", "Kalite Sèvis" },
                    { "AppointmentDate", "Dat Rendez-vous" },
                    { "AppointmentTime", "Lè Rendez-vous" },
                    { "AppointmentType", "Kalite Rendez-vous" },
                    { "Reference", "Referans" },
                    { "Enter", "Antre" },
                    { "Validate", "Valide" },
                    { "Comments", "Kòmantè" },
                    { "SeeMeetingGuidelines", "Wè Gid Reyinyon" },
                    { "CreateAppointment", "Kreye Rendez-vous" },
                    { "Select", "--Chwazi--" },
                    { "SelectAppointmentDate", "--Chwazi Dat Rendez-vous--" },
                    { "Upcoming", "K ap vini" },
                    { "History", "Istwa" },
                    { "DateAndTime", "Dat ak Lè" },
                    { "Queue", "Kew" },
                    { "Type", "Kalite" },
                    { "Status", "Estati" },
                    { "Cancel", "Anile" },
                    { "Scheduled", "Pwograme" },
                    { "Cancelled", "Anile" },
                    { "CancelAppointment", "Anile Appointman" },
                    { "Service", "Sèvis" },
                    { "ContactBy", "Kontakte Pa" },
                    { "Reason", "Rezon" },
                    { "SelectFromList", "Chwazi nan lis la" },
                    { "ScheduleConflict", "Konfli Orè" },
                    { "NoLongerNeeded", "Pa Bezwen Ankò" },
                    { "FoundAlternative", "Jwenn Altènatif" },
                    { "Other", "Lòt" },
                    { "Close", "Fèmen" },
                    { "Confirm", "Konfime" },
                    { "ConfirmCancelMessage", "Èske w sèten ou vle anile appointman sa a?" },
                    { "No", "Non" },
                    { "Yes", "Wi" },
                    { "SelectReasonValidation", "Chwazi rezon pou anilasyon an" },
                    { "Back", "Retou" }
                }
            }
        };

        public static string GetResourceString(string key)
        {
            try
            {
                string culture = GetCurrentCulture();
                if (_resources.ContainsKey(culture) && _resources[culture].ContainsKey(key))
                {
                    return _resources[culture][key];
                }
                // Fallback to English
                if (_resources.ContainsKey("en") && _resources["en"].ContainsKey(key))
                {
                    return _resources["en"][key];
                }
                return key;
            }
            catch
            {
                return key;
            }
        }

        public static string GetCurrentCulture()
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies["Language"];
            if (cookie != null && !string.IsNullOrEmpty(cookie.Value))
            {
                return cookie.Value;
            }
            return "en"; // Default to English
        }

        public static void SetCulture(string culture)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture == "ht" ? "ht-HT" : (culture == "es" ? "es-ES" : "en-US"));
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture == "ht" ? "ht-HT" : (culture == "es" ? "es-ES" : "en-US"));
        }
    }
}
