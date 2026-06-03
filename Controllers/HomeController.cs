using System.Linq;
using System.Web.Mvc;
using HamaStudio.Models;
using System.Data.Entity;

namespace HamaStudio.Controllers
{
    public class HomeController : Controller
    {
        private HamaStudioDbContext db = new HamaStudioDbContext();

        public ActionResult Index()
        {
            // Lấy 3 dịch vụ nổi bật (giá cao nhất)
            var featuredServices = db.DichVus.OrderByDescending(d => d.GiaTien).Take(3).ToList();
            ViewBag.FeaturedServices = featuredServices;

            // Lấy portfolio mới nhất
            var latestPortfolios = db.Portfolios.OrderByDescending(p => p.MaAnh).Take(6).ToList();
            ViewBag.LatestPortfolios = latestPortfolios;

            return View();
        }

        public ActionResult Portfolio()
        {
            var portfolios = db.Portfolios.Include(p => p.DichVu).ToList();
            return View(portfolios);
        }

        public ActionResult Gallery()
        {
            var portfolios = db.Portfolios.ToList();
            return View(portfolios);
        }

        public ActionResult Services(string search, int? categoryId)
        {
            var query = db.DichVus.Include(d => d.DanhMucDichVu);
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(d => d.TenDichVu.Contains(search) || d.MoTa.Contains(search));
            }
            if (categoryId.HasValue)
            {
                query = query.Where(d => d.MaDanhMuc == categoryId.Value);
            }

            ViewBag.Categories = db.DanhMucDichVus.ToList();
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategoryId = categoryId;

            return View(query.ToList());
        }

        public ActionResult ServiceDetail(int? id)
        {
            if (id == null) return RedirectToAction("Services");

            var service = db.DichVus.Include(d => d.DanhMucDichVu)
                                    .Include(d => d.Portfolios)
                                    .Include(d => d.DacQuyens)
                                    .Include(d => d.DichVuBoSungs)
                                    .Include(d => d.DatLiches.Select(dl => dl.DanhGias))
                                    .FirstOrDefault(d => d.MaDichVu == id);
                                    
            if (service == null) return HttpNotFound();

            ViewBag.RelatedServices = db.DichVus.Include(d => d.DanhMucDichVu)
                                        .Where(d => d.MaDanhMuc == service.MaDanhMuc && d.MaDichVu != service.MaDichVu)
                                        .OrderByDescending(d => d.GiaTien)
                                        .Take(3).ToList();

            return View(service);
        }

        public ActionResult About()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
