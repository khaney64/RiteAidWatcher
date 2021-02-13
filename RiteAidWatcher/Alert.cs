using System;
using System.Collections.Generic;
using System.Text;

namespace RiteAidWatcher
{
    class Alert
    {
        public AlertStatusType AlertStatus { get; set; }

        public Dictionary<int,AlertData> ActiveStores { get; set; }
    }
}
