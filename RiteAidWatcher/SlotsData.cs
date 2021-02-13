using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiteAidWatcher
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Slots
    {
        public bool _1 { get; set; }
        public bool _2 { get; set; }
    }

    public class SlotsData
    {
        public Slots slots { get; set; }
    }

    public class SlotsRoot
    {
        [JsonProperty("Data")]
        public Slots Slots { get; set; }
        public string Status { get; set; }
        public object ErrCde { get; set; }
        public object ErrMsg { get; set; }
        public object ErrMsgDtl { get; set; }
    }


}
