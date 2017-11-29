using System;

namespace SimpleMvc.Common
{
    [Serializable]
    public class AlertMessage
    {
        public enum AlertType
        {
            Info,
            Success,
            Error,
            Warning
        }

        public const string TempDataKey = "TempDataAlerts";

        public AlertType Type { get; set; }
        public string Message { get; set; }
        public bool Dismissable { get; set; }
    }
}
