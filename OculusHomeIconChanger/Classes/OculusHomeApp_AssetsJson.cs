using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OculusHomeIconChangerNS
{
    public class OculusHomeApp_AssetsJson
    {
        [JsonProperty(PropertyName = "thirdParty")]
        public bool thirdParty { get; set; }

        [JsonProperty(PropertyName = "canonicalName")]
        public string canonicalName { get; set; }

        // Default Constructor
        public OculusHomeApp_AssetsJson()
        {

        }
    }
}
