using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiteAidWatcher
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Slots
    {
        public Slots() { }

        [JsonProperty("1")]
        public bool Slot1 { get; set; }
        [JsonProperty("2")]
        public bool Slot2 { get; set; }
    }

    public class SlotsData
    {
        public Slots Slots { get; set; }
    }

    public class SlotsRoot
    {
        public SlotsData Data { get; set; }
        public string Status { get; set; }
        public object ErrCde { get; set; }
        public object ErrMsg { get; set; }
        public object ErrMsgDtl { get; set; }
    }


}
