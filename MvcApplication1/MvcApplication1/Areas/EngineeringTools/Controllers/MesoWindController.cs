using System;
using System.Collections.Generic;
using System.Configuration;
using System.Device.Location;
using System.Diagnostics;
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
        private static readonly List<DatabaseItem> _items = new List<DatabaseItem>();
        private const string CurrentFile = "CurrentFile";

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

        public ActionResult WindRose()
        {
            return View();
        }

        public ActionResult Results()
        {
            return View();
        }

        public JsonResult Import(string file)
        {
            Session[CurrentFile] = file;
            return Json("OK", JsonRequestBehavior.AllowGet);
        }

        public JsonResult CurrentDataJson(int sEcho)
        {
            if (Session[CurrentFile] == null)
            {
                var dataEmpty = new { sEcho = sEcho, iTotalRecords = 0, iTotalDisplayRecords = 0, aaData = new List<decimal[]>() };
                return Json(dataEmpty, JsonRequestBehavior.AllowGet);
            }

            var file = (string)Session[CurrentFile];
            string DbDir = WebConfigurationManager.AppSettings["MesoWindTabDir"];
            var model = ImportFile(DbDir, file);
            
            var final = new List<string[]>();
            var n = model.NDirs + 1;

            var freqs = new string[n];
            freqs[0] = "Frequencies";
            for (var i = 0; i < model.FreqByDirs.Count; i++)
            {
                freqs[i + 1] = model.FreqByDirs[i].ToString();
            }
            final.Add(freqs);

            for (var bIndex = 0; bIndex < model.FreqByBins.Count; bIndex++)
            {
                var bin = model.FreqByBins[bIndex];
                var binWith13 = new string[n];
                binWith13[0] = (bIndex + 1).ToString();
                for (var i = 0; i < bin.Length; i++)
                {
                    binWith13[i + 1] = bin[i].ToString();
                }
                final.Add(binWith13);
            }

            // MeanVelocityPerDir
            model.MeanVelocityPerDir.AddRange(new decimal[model.NDirs]);
            for (var binIdx = 0; binIdx < model.NBins; binIdx++)
                for (var dirIdx = 0; dirIdx < model.NDirs; dirIdx++)
                {
                    var velocity = binIdx + 1;
                    model.MeanVelocityPerDir[dirIdx] += (decimal)(velocity * (double)model.FreqByBins[binIdx][dirIdx] / 1000);
                }

            var mean = new string[n];
            mean[0] = "Mean Vel.";
            for (var i = 0; i < model.MeanVelocityPerDir.Count; i++)
            {
                mean[i + 1] = model.MeanVelocityPerDir[i].ToString();
            }
            final.Add(mean);

            var data = new { sEcho = sEcho, iTotalRecords = model.NBins, iTotalDisplayRecords = model.NBins, aaData = final };

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        private VDataImport ImportFile(string dir, string fileName)
        {
            var model = new VDataImport();
            var path = System.IO.Path.Combine(dir, fileName);
            using (var f = new StreamReader(path))
            {
                var lineN = 0;
                while (!f.EndOfStream)
                {
                    var line = f.ReadLine();
                    lineN++;
                    Trace.WriteLine(line);
                    switch (lineN)
                    {
                        case 1:
                            break;
                        case 2:
                            var line2 = line.Trim().Split("\t ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            model.NBins = ParseInt(line2[2]);
                            break;
                        case 3:
                            var line3 = line.Trim().Split("\t ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            model.NDirs = ParseInt(line3[0]);
                            break;
                        case 4:
                            var line4 = line.Trim().Split("\t ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            foreach (var s in line4)
                            {
                                model.FreqByDirs.Add(ParseDecimal(s));
                            }
                            break;
                        default:
                            var line5N = line.Trim().Split("\t ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            Debug.Assert(line5N.Length == model.NDirs + 1); // 1st cell contains bin number
                            var tmp = new decimal[model.NDirs];
                            for (var i = 0; i < model.NDirs; i++)
                            {
                                tmp[i] = ParseDecimal(line5N[i + 1]);
                            }
                            model.FreqByBins.Add(tmp);
                            break;
                    }
                }
            }
            return model;
        }

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
