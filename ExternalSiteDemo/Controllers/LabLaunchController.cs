using ExternalSiteDemo.Core.Config;
using ExternalSiteDemo.Data;
using ExternalSiteDemo.Models;
using LabOnDemand.ApiV3;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ExternalSiteDemo.Controllers
{
    public class LabLaunchController : Controller
    {
        // Modify with lab profile to launch. Typically a real world application would pull the available lab profiles
        // using the LabOnDemandApiClient.Catalog() method.
        private const int LAB_PROFILE_TO_LAUNCH = 0;

        private readonly ExternalSiteDemoContext _context;
        private readonly IPAddress lodIpAddressProd;
        private readonly LODSettings _Settings;

        public IConfiguration Configuration { get; }


        public LabLaunchController(ExternalSiteDemoContext context, IConfiguration configuration)
        {
            Configuration = configuration;
            _context = context;
            _Settings = Configuration.GetSection("LODSettings").Get<LODSettings>();
        }

        public async Task<IActionResult> Index() => View(await _context.LabLaunch.ToListAsync());
        private bool LabLaunchExists(long id) => _context.LabLaunch.Any(e => e.Id == id);
        public IActionResult StartLaunch() => View("Launch", new LaunchModel());

        /// <summary>
        /// List details for a lab launch record
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();
            var labLaunch = await _context.LabLaunch.SingleOrDefaultAsync(m => m.Id == id);
            if (labLaunch == null) return NotFound();
            return View(labLaunch);
        }

        /// <summary>
        /// Called from LOD Life Cycle Action. When invoked, this method will use the LOD client to verify
        /// that the lab instance is valid. Also prior to taking any action, the requestors IP address is checked
        /// against a list of IP addresses contained within the appsettings.json file. You can add your local IP address
        /// to the whitelist to test using other invoke methods (PowerShell, PostMan, Browser, etc.)
        /// </summary>
        /// <param name="labAction">The action passed in from the LifeCycleAction URL in LOD</param>
        /// <param name="labProfileId">ID of the lab profile which the LifeCycleAction was called for</param>
        /// <param name="labInstanceId">ID of the lab instance which the LifeCycleAction was called for</param>
        /// <param name="globalLabInstanceId">Global ID of the lab profile which the LifeCycleAction was called for</param>
        /// <param name="userId">LOD User Id who launched the lab</param>
        /// <param name="firstName">First name of the user who launched the lab</param>
        /// <param name="lastName">Last name of the user who launched the lab</param>
        /// <param name="email">Email address of the user who launched the lab</param>
        /// <returns>Returns true on completion.</returns>
        [HttpGet("LabLaunch/RegisterLabAction/{labAction}")]
        public IActionResult RegisterLabAction(string labAction, int labProfileId, long labInstanceId, string globalLabInstanceId,
            string userId, string firstName, string lastName, string email)
        {
            var apiUrl = _Settings.APIURL;
            var apiKey = _Settings.APIKey;
            var client = new LabOnDemandApiClient(apiUrl, apiKey);

            // check whitelist!
            if (!_Settings.IPWhitelist.Contains(Request.HttpContext.Connection.RemoteIpAddress.ToString()))
            {
                return NotFound();
            }

            // verify that LOD knows about this launch!
            var details = client.Details(labInstanceId);
            if (!(details.UserId == userId ||
                (details.UserFirstName == firstName && details.UserLastName == lastName)) ||
                details.LabProfileId != labProfileId)
            {
                return NotFound();
            }

            // if we get here, everything checks out, so add our record!
            var launch = new LabAction
            {
                FirstName = details.UserFirstName,
                LastName = details.UserLastName,
                State = details.State,
                Status = details.Status.ToString(),
                LabInstanceId = labInstanceId,
                LabProfileId = details.LabProfileId,
                LabProfileName = details.LabProfileName,
                LabActionDescription = labAction
            };

            _context.Add(launch);
            _context.SaveChanges();

            return Json(new { success = true });
        }

        /// <summary>
        /// Cancels a lab instance that was launched with the same API key. Once the lab is canceled, 
        /// a new record is added to the database. The LOD client is used to cancel the lab. 
        /// </summary>
        /// <param name="labRecordId">Lab record Id form the demo database</param>
        /// <returns>Returns true on completion</returns>
        [HttpGet("LabLaunch/CancelLab/{labRecordId}")]
        public IActionResult CancelLab(long labRecordId)
        {
            var launchRecord = _context.LabLaunch.FirstOrDefault(rec => rec.Id == labRecordId);
            if (launchRecord == null) return NotFound();

            var apiUrl = _Settings.APIURL;
            var apiKey = _Settings.APIKey;
            var client = new LabOnDemandApiClient(apiUrl, apiKey);

            var labInstance = client.Details(launchRecord.LabInstanceId);
            if (labInstance.State != "Running") return BadRequest();

            var response = client.Cancel(launchRecord.LabInstanceId, "Canceled from remote test site");
            if (response.Result == CancelResult.Success)
            {
                var launch = new LabAction
                {
                    FirstName = labInstance.UserFirstName,
                    LastName = labInstance.UserLastName,
                    State = labInstance.State,
                    Status = labInstance.Status.ToString(),
                    LabInstanceId = labInstance.Id,
                    LabProfileId = labInstance.LabProfileId,
                    LabProfileName = labInstance.LabProfileName,
                    LabActionDescription = "Lab canceled from Demo Site"
                };
                _context.Add(launch);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// A method for deleting a record from the demo database
        /// </summary>
        /// <param name="id">Id of the record in the demo database to delete</param>
        /// <returns>Returns delete view</returns>
        // GET: LabLaunch/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null) return NotFound();
            var labLaunch = await _context.LabLaunch.SingleOrDefaultAsync(m => m.Id == id);
            if (labLaunch == null) return NotFound();

            return View(labLaunch);
        }

        /// <summary>
        /// Used to launch a lab in LOD. The API is used here to call Launch, which returns a URL that will connect the 
        /// user to the lab instance. It is possible to automatically open the window for the user, but be weary of popup blockers
        /// because any window open call on the browser without the direct action of a user (clicking a link) will be blocked by popup
        /// blockers
        /// </summary>
        /// <param name="model">Launches a lab in LOD</param>
        /// <returns>Returns a model contianing the link to the lab instance</returns>
        public IActionResult Launch(LaunchModel model)
        {
            if (!ModelState.IsValid)
            {
                var modelToRebind = model;
                ModelState.Clear();
                return View("Launch", modelToRebind);
            }

            var apiUrl = _Settings.APIURL;
            var apiKey = _Settings.APIKey;
            var client = new LabOnDemandApiClient(apiUrl, apiKey);
            var response = client.Launch(LAB_PROFILE_TO_LAUNCH, $"{model.FirstName}{model.LastName}", model.FirstName, model.LastName, model.Email);
            model.LabLaunchURL = response.Url;
            if (string.IsNullOrEmpty(model.LabLaunchURL)) return View(nameof(StartLaunch), model);
            return View("LaunchPad", model);
        }

        /// <summary>
        /// Delete Confirmation action
        /// </summary>
        /// <param name="id">Id of the record to be deleted</param>
        /// <returns>Redirect to index</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var labLaunch = await _context.LabLaunch.SingleOrDefaultAsync(m => m.Id == id);
            _context.LabLaunch.Remove(labLaunch);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


    }
}
