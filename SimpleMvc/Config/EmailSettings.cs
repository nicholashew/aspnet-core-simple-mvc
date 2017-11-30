using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleMvc.Config
{
    public class EmailSettings
    {
        public bool UseDebugEmail { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public string SenderName { get; set; }
        public string SenderEmail { get; set; }
        public string Account { get; set; }
        public string Password { get; set; }
        public string SystemEmailSubjectPrefix { get; set; }
        public string SystemEmailTo { get; set; }
        public string SystemEmailCc { get; set; }

        public List<string> SystemEmailCcList
        {
            get
            {
                return SystemEmailCc
                   .Split(new Char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                   .Where(x => !string.IsNullOrWhiteSpace(x))
                   .Select(s => s.Trim())
                   .ToList();
            }
        }
    }
}
