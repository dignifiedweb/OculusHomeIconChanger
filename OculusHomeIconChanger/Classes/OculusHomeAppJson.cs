using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OculusHomeIconChangerNS
{
    public class OculusHomeAppJson
    {
        [JsonProperty(PropertyName = "canonicalName")]
        public string canonicalName { get; set; }

        [JsonProperty(PropertyName = "displayName")]
        public string displayName { get; set; }

        //https://www.newtonsoft.com/json/help/html/ModifyJson.htm -- Could nto get it to load otherwise (unless just leave it out)
        [JsonProperty(PropertyName = "files")]
        public JsonArrayAttribute files { get; set; }

        [JsonProperty(PropertyName = "firewallExceptionsRequired")]
        public bool firewallExceptionsRequired { get; set; }

        [JsonProperty(PropertyName = "isCore")]
        public bool isCore { get; set; }

        [JsonProperty(PropertyName = "launchFile")]
        public string launchFile { get; set; }

        [JsonProperty(PropertyName = "launchParameters")]
        public string launchParameters { get; set; }

        [JsonProperty(PropertyName = "manifestVersion")]
        public int manifestVersion { get; set; }

        [JsonProperty(PropertyName = "packageType")]
        public string packageType { get; set; }

        [JsonProperty(PropertyName = "thirdParty")]
        public bool thirdParty { get; set; }

        [JsonProperty(PropertyName = "version")]
        public string version { get; set; }

        [JsonProperty(PropertyName = "versionCode")]
        public int versionCode { get; set; }
    }
}
