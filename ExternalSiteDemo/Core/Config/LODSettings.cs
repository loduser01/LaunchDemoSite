using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExternalSiteDemo.Core.Config
{

    /// <summary>
    /// This class mirrors the configuration json blob. This helps with loading in configuration settings!
    /// </summary>
    public class LODSettings
    {
        public string APIURL { get; set; }
        public string APIKey { get; set; }
        public List<string> IPWhitelist { get; set; }
    }
}
