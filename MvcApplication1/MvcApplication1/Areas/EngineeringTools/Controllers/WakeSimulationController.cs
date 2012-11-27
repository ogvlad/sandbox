using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcApplication1.Areas.EngineeringTools.Controllers
{
    public class WakeSimulationController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GeneralProperties()
        {
            return View();
        }

        public ActionResult TurbineProperties()
        {
            return View();
        }

        public ActionResult Simulation()
        {
            return View();
        }
    }
}
