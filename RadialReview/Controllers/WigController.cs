using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Newtonsoft.Json;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Admin;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class WigController : BaseController
    {
        public class WigData
        {
            string WigCharter { get; set; }

        }
        // GET: Wig
        public ActionResult Index()
        {
            return View();
        }
    }
}