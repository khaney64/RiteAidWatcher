using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RiteAidChecker
{
    public class RiteAidData
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string BirthDate { get; set; }
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string StateCode { get; set; }
        public string StateName { get; set; }
        public string Zip { get; set; }
        public string MobilePhone { get; set; }
        public string EmailAddress { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public OccupationType Occupation { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ConditionType Condition { get; set; }
        public string OtherConditions { get; set; }
    }
}
