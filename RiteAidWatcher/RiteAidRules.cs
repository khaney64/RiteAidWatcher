using System.Collections.Generic;
using Newtonsoft.Json;

namespace RiteAidWatcher
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<RulesRoot>(myJsonResponse); 
    public class All
    {
        [JsonProperty("fact")]
        public string Fact { get; set; }

        [JsonProperty("operator")]
        public string Operator { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }
    }

    public class Any
    {
        [JsonProperty("all")]
        public List<All> All { get; set; }
    }

    public class Conditions
    {
        [JsonProperty("any")]
        public List<Any> Any { get; set; }
    }

    public class Params
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }

    public class Event
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("params")]
        public Params Params { get; set; }
    }

    public class RulesRoot
    {
        [JsonProperty("conditions")]
        public Conditions Conditions { get; set; }

        [JsonProperty("event")]
        public Event Event { get; set; }
    }


}