using System;
using System.Collections.Generic;
using System.Text;

namespace RiteAidChecker
{
    public class RiteAidData
    {
        public string BirthDate { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public OccupationType Occupation { get; set; }
        public ConditionType Condition { get; set; }
    }
}
