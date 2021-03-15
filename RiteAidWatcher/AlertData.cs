using OpenQA.Selenium.Chrome;
using RiteAidChecker;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiteAidWatcher
{
    class AlertData
    {
        public int StoreNumber { get; set; }

        public string ZipCode { get; set; }

        public bool Slot1 { get; set; }

        public bool Slot2 { get; set; }

        public DateTime? Start { get; set; }

        public DateTime? End { get; set; }

        public StoreStatusType Status { get; set; }

        public DateTime? LastCheck { get; set; }

        public int Tries { get; set; }

        public ChromeDriver Browser { get; set; }
    }
}
