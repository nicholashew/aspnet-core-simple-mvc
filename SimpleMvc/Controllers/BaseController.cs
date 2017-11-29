using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;
using SimpleMvc.Common;
using SimpleMvc.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleMvc.Controllers
{
    public class BaseController : Controller
    {
        #region Helpers

        public Task<ApplicationUser> GetCurrentUserAsync(UserManager<ApplicationUser> userManager)
        {
            return userManager.GetUserAsync(HttpContext.User);
        }

        public void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        public void AddSuccessMessage(string message, bool dismissable = false)
        {
            AddAlertMessage(AlertMessage.AlertType.Success, message, dismissable);
        }

        public void AddInfoMessage(string message, bool dismissable = false)
        {
            AddAlertMessage(AlertMessage.AlertType.Info, message, dismissable);
        }

        public void AddWarningMessage(string message, bool dismissable = false)
        {
            AddAlertMessage(AlertMessage.AlertType.Warning, message, dismissable);
        }

        public void AddErrorMessage(string message, bool dismissable = false)
        {
            AddAlertMessage(AlertMessage.AlertType.Error, message, dismissable);
        }

        private void AddAlertMessage(AlertMessage.AlertType type, string message, bool dismissable)
        {
            // Asp.Net Core MVC does not supported complex data type for TempData currently
            var alerts = TempData.ContainsKey(AlertMessage.TempDataKey)
                ? JsonConvert.DeserializeObject<List<AlertMessage>>(TempData[AlertMessage.TempDataKey].ToString())
                : new List<AlertMessage>();

            alerts.Add(new AlertMessage
            {
                Type = type,
                Message = message,
                Dismissable = dismissable
            });

            TempData[AlertMessage.TempDataKey] = JsonConvert.SerializeObject(alerts);
        }

        #endregion
    }
}
