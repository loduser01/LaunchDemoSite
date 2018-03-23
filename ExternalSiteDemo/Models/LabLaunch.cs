using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace ExternalSiteDemo.Data
{
    public class LabAction
    {
        public long Id { get; set; }
        [DisplayName("First Name")]
        public string FirstName { get; set; }
        [DisplayName("Last Name")]
        public string LastName { get; set; }
        public string State { get; set; }
        public string Status { get; set; }
        public long LabInstanceId { get; set; }
        public int LabProfileId { get; set; }
        [DisplayName("Lab Profile Name")]
        public string LabProfileName { get; set; }
        [DisplayName("Lab Action Description")]
        public string LabActionDescription { get; set; }
    }
}
