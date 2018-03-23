using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ExternalSiteDemo.Models
{
    public class LaunchModel
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        public string LabLaunchURL { get; set; }
        [Required]
        public string Email { get; set; }
    }
}
