using System;
using System.Collections.Generic;
using System.Configuration;
using System.Device.Location;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using MvcApplication1.Areas.EngineeringTools.Models;

namespace MvcApplication1.Areas.EngineeringTools.Controllers
{
    public class MesoWindController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Database()
        {
            return View();
        }

        public ActionResult CurrentData()
        {
            return View();
        }
        
        public ActionResult VelocityFreq()
        {
            return View();
        }

        public ActionResult FreqPerSector()
        {
            return View();
        }

        public ActionResult MeanVelocityPerSector()
        {
            return View();
        }

        public ActionResult Results()
        {
            return View();
        }

        private static readonly List<DatabaseItem> _items = new List<DatabaseItem>();

        public JsonResult GetDatabasePoints(int sEcho, int iDisplayLength, int iDisplayStart)
        {
            lock (_items)
            {
                if (_items.Count == 0)
                    InitDatabase();

                var filtered = _items
                    .Skip(iDisplayLength*iDisplayStart)
                    .Take(iDisplayLength)
                    .Select(x => new object[] { x.Latitude, x.Longitude, x.FileName })
                    .ToArray();
                var data = new { sEcho = sEcho, iTotalRecords = _items.Count, iTotalDisplayRecords = _items.Count, aaData = filtered };
                return Json(data, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetDatabasePointsF(double lat, double lng)
        {
            lock (_items)
            {
                if (_items.Count == 0)
                    InitDatabase();
                
                return Json(GetFiltered(lat, lng), JsonRequestBehavior.AllowGet);
            }
        }

        private DatabaseItem[] GetFiltered(double lat, double lng)
        {
            var tmp = new List<DatabaseItem>();
            foreach (var item in _items)
            {
                var sCoord = new GeoCoordinate((double)item.Latitude, (double)item.Longitude);
                var eCoord = new GeoCoordinate(lat, lng);

                item.Distance = sCoord.GetDistanceTo(eCoord); //meters
                if (item.Distance > 100000) continue;
                tmp.Add(item);
            }
            return tmp.ToArray();
        }

        private void InitDatabase()
        {
            string DbDir = WebConfigurationManager.AppSettings["MesoWindTabDir"];
            foreach (var d in Directory.EnumerateFiles(DbDir, "*.dat.tab", SearchOption.TopDirectoryOnly))
            {
                var f = System.IO.Path.GetFileName(d);
                f = f.Replace(".dat.tab", "");
                var parts = f.Split('_');

                var longitude = ParseDecimal(parts[0].TrimEnd("NESW".ToCharArray()));
                var latitude = ParseDecimal(parts[1].TrimEnd("NESW".ToCharArray()));
                if (parts[0].EndsWith("W")) longitude = -longitude;
                if (parts[1].EndsWith("S")) latitude = -latitude;

                var dbItem = new DatabaseItem();
                dbItem.Longitude = longitude;
                dbItem.Latitude = latitude;
                dbItem.FileName = System.IO.Path.GetFileName(d);
                _items.Add(dbItem);
            }
        }

        private int ParseInt(string input)
        {
            int ir;
            if (int.TryParse(input, out ir))
                return ir;
            decimal dr;
            if (decimal.TryParse(input, out dr))
                return Convert.ToInt32(dr);
            return 0;
        }

        private decimal ParseDecimal(string input)
        {
            decimal dr;
            if (decimal.TryParse(input, out dr))
                return dr;
            return 0;
        }
    }
}
