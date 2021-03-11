using System;
using System.Collections.Generic;
using System.Text;

namespace RiteAidChecker
{
    public class RiteAidConfig
    {
        public RiteAidData Data { get; set; }

        public int MaxMiles { get; set; }

        public bool Filter { get; set; }

        public bool BrowserCheck { get; set; }
    }
}
