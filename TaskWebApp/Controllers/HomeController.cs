using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using TaskWebApp.Business;

namespace TaskWebApp.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<ActionResult> Claims()
        {
            var userObjectId = ClaimsPrincipal.Current.Claims
                .Where(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")
                .Select(x => x.Value)
                .FirstOrDefault();

            var model = ClaimsPrincipal.Current.Claims
                .Select(x => new Claim(x.Type, x.Value))
                .ToList();

            if (userObjectId != null)
            {
                var b2cGraph = new B2CGraphClient();
                var userGroups = await b2cGraph.GetUserGroups(userObjectId);

                var aggregatedGroups = string.Empty;

                foreach (var userGroup in userGroups)
                {
                    if (aggregatedGroups != string.Empty)
                    {
                        aggregatedGroups += " ";
                    }

                    aggregatedGroups += userGroup.DisplayName;
                }

                model.Add(new Claim("USER_GROUPS", aggregatedGroups));
            }
            ViewBag.Message = "Your application description page.";
            return View(model);
        }

        public ActionResult Error(string message)
        {
            ViewBag.Message = message;

            return View("Error");
        }
    }
}