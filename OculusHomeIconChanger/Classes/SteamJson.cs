using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OculusHomeIconChangerNS
{
    public class SteamApp
    {
        [JsonProperty(PropertyName = "appid")]
        public int appid { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string name { get; set; }
    }

    public class SteamApplist
    {
        [JsonProperty(PropertyName = "apps")]
        public List<SteamApp> apps { get; set; }
    }

    public class SteamRootObject
    {
        [JsonProperty(PropertyName = "applist")]
        public SteamApplist applist { get; set; }
    }
}
