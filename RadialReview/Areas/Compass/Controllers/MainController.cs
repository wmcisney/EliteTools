using RadialReview.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Areas.Compass.Controllers
{
    public class MainController : BaseController
    {
        // GET: Compass/Main
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Index()
        {
            return View();
        }
    }
}