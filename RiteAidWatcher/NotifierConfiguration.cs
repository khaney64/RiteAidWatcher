using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiteAidWatcher
{
    internal class NotifierConfiguration
    {
        public string MailTo { get; set; }
        public string MailFrom { get; set; }
        public string MailServer { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }

        public NotifierConfiguration(IConfiguration configuration)
        {
            var section = configuration.GetSection(SectionName);
            MailTo = section[ConfigMailTo];
            MailFrom = section[ConfigMailFrom];
            MailServer = section[ConfigMailServer];
            SmtpUsername = section[ConfigSmtpUsername];
            SmtpPassword = section[ConfigSmtpPassword];
        }

        private const string SectionName = "Notify";

        private const string ConfigMailTo = "MailTo";
        private const string ConfigMailFrom = "MailFrom";
        private const string ConfigMailServer = "MailServer";
        private const string ConfigSmtpUsername = "SmtpUsername";
        private const string ConfigSmtpPassword = "SmtpPassword";

    }
}
