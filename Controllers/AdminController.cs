using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HamaStudio.Models;
using System.Data.Entity;

namespace HamaStudio.Controllers
{
    public class AdminController : Controller
    {
        private HamaStudioDbContext db = new HamaStudioDbContext();

        // ─────────────────────────────────────────────
        // CHECK SESSION ADMIN
        // ─────────────────────────────────────────────
        private bool IsAdmin()
        {
            return Session["IsAdmin"] != null && (bool)Session["IsAdmin"] == true;
        }

        // ─────────────────────────────────────────────
        // TRANG CHÍNH ADMIN
        // ─────────────────────────────────────────────
        public ActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            // Thống kê tổng quan
            ViewBag.TongKhachHang = db.KhachHangs.Count();
            ViewBag.TongDatLich = db.DatLiches.Count();
            ViewBag.TongDoanhThu = db.ThanhToans.Sum(t => (decimal?)t.SoTienThanhToan) ?? 0;
            ViewBag.LichChoXacNhan = db.DatLiches.Count(d => d.TrangThai == "Chờ xác nhận");

            return View();
        }

        // ─────────────────────────────────────────────
        // QUẢN LÝ LỊCH ĐẶT
        // ─────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetDatLich(string loai)
        {
            if (!IsAdmin()) return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            IQueryable<DatLich> query = db.DatLiches
                .Include("KhachHang").Include("DichVu").Include("ThanhToans");

            if (loai == "chua_xac_nhan")
                query = query.Where(d => d.TrangThai == "Chờ xác nhận");
            else if (loai == "da_xac_nhan")
                query = query.Where(d => d.TrangThai == "Đã xác nhận" || d.TrangThai == "Đã đặt cọc");
            else if (loai == "hoan_thanh")
                query = query.Where(d => d.TrangThai == "Hoàn thành");

            var list = query.OrderByDescending(d => d.NgayHeThongGhiNhan).ToList().Select(d => new
            {
                d.MaDatLich,
                KhachHang = d.KhachHang != null ? d.KhachHang.HoTen : "N/A",
                SoDienThoai = d.KhachHang != null ? d.KhachHang.SoDienThoai : "",
                DichVu = d.DichVu != null ? d.DichVu.TenDichVu : "N/A",
                NgayChup = d.NgayChup.ToString("dd/MM/yyyy"),
                d.KhungGio,
                d.DiaDiem,
                TongTien = d.TongTien ?? 0,
                SoTienDaCoc = d.SoTienDaCoc ?? 0,
                d.TrangThai,
                d.GhiChu,
                d.LinkAnhBanGiao,
                NgayGhiNhan = d.NgayHeThongGhiNhan.HasValue ? d.NgayHeThongGhiNhan.Value.ToString("dd/MM/yyyy HH:mm") : ""
            });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CapNhatTrangThaiDatLich(int maDatLich, string trangThai, string linkAnh)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var datLich = db.DatLiches.Find(maDatLich);
            if (datLich == null) return Json(new { success = false, message = "Không tìm thấy lịch đặt" });

            datLich.TrangThai = trangThai;
            if (!string.IsNullOrEmpty(linkAnh))
                datLich.LinkAnhBanGiao = linkAnh;

            db.SaveChanges();
            return Json(new { success = true });
        }

        // ─────────────────────────────────────────────
        // QUẢN LÝ KHÁCH HÀNG
        // ─────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetKhachHang()
        {
            if (!IsAdmin()) return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            var list = db.KhachHangs.ToList().Select(k => new
            {
                k.MaKhachHang,
                k.HoTen,
                k.TenDangNhap,
                k.Email,
                k.SoDienThoai,
                k.GioiTinh,
                NgaySinh = k.NgaySinh.HasValue ? k.NgaySinh.Value.ToString("dd/MM/yyyy") : "",
                k.DiaChi,
                k.TrangThai,
                NgayDangKy = k.NgayDangKy.HasValue ? k.NgayDangKy.Value.ToString("dd/MM/yyyy") : "",
                SoLichDat = db.DatLiches.Count(d => d.MaKhachHang == k.MaKhachHang)
            });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CapNhatTrangThaiKhachHang(int maKhachHang, string trangThai)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var kh = db.KhachHangs.Find(maKhachHang);
            if (kh == null) return Json(new { success = false });

            kh.TrangThai = trangThai;
            db.SaveChanges();
            return Json(new { success = true });
        }

        // ─────────────────────────────────────────────
        // THỐNG KÊ DOANH THU
        // ─────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetThongKe(int? nam, int? thang, string ngayTu, string ngayDen)
        {
            if (!IsAdmin()) return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            int year = nam ?? DateTime.Now.Year;

            // Xác định khoảng ngày lọc
            DateTime? dateFrom = null, dateTo = null;
            if (!string.IsNullOrEmpty(ngayTu) && DateTime.TryParse(ngayTu, out var df2)) dateFrom = df2;
            if (!string.IsNullOrEmpty(ngayDen) && DateTime.TryParse(ngayDen, out var dt2)) dateTo = dt2.AddDays(1).AddSeconds(-1);

            var doanhThuTheoThang = new List<object>();

            if (dateFrom.HasValue && dateTo.HasValue)
            {
                // Lọc theo ngày: hiển thị từng ngày trong khoảng
                var current = dateFrom.Value.Date;
                var end = dateTo.Value.Date;
                int idx = 1;
                while (current <= end)
                {
                    var next = current.AddDays(1);
                    var tongNgay = db.ThanhToans
                        .Where(t => t.NgayGiaoDich.HasValue && t.NgayGiaoDich.Value >= current && t.NgayGiaoDich.Value < next)
                        .Sum(t => (decimal?)t.SoTienThanhToan) ?? 0;
                    doanhThuTheoThang.Add(new { thang = idx, doanhThu = tongNgay });
                    current = next; idx++;
                }
            }
            else if (thang.HasValue)
            {
                // Lọc theo tháng: hiển thị từng ngày trong tháng
                int days = DateTime.DaysInMonth(year, thang.Value);
                for (int d = 1; d <= days; d++)
                {
                    var date = new DateTime(year, thang.Value, d);
                    var next = date.AddDays(1);
                    var tongNgay = db.ThanhToans
                        .Where(t => t.NgayGiaoDich.HasValue && t.NgayGiaoDich.Value >= date && t.NgayGiaoDich.Value < next)
                        .Sum(t => (decimal?)t.SoTienThanhToan) ?? 0;
                    doanhThuTheoThang.Add(new { thang = d, doanhThu = tongNgay });
                }
            }
            else
            {
                // Mặc định: doanh thu theo tháng trong năm
                for (int i = 1; i <= 12; i++)
                {
                    var tongThang = db.ThanhToans
                        .Where(t => t.NgayGiaoDich.HasValue && t.NgayGiaoDich.Value.Year == year && t.NgayGiaoDich.Value.Month == i)
                        .Sum(t => (decimal?)t.SoTienThanhToan) ?? 0;
                    doanhThuTheoThang.Add(new { thang = i, doanhThu = tongThang });
                }
            }

            // Doanh thu theo dịch vụ
            IQueryable<DatLich> dvQuery = db.DatLiches.Where(d => d.TrangThai != "Đã hủy").Include("DichVu");
            if (dateFrom.HasValue && dateTo.HasValue)
                dvQuery = dvQuery.Where(d => d.NgayChup >= dateFrom.Value && d.NgayChup <= dateTo.Value);
            else if (thang.HasValue)
                dvQuery = dvQuery.Where(d => d.NgayChup.Year == year && d.NgayChup.Month == thang.Value);
            else
                dvQuery = dvQuery.Where(d => d.NgayChup.Year == year);

            var doanhThuTheoDV = dvQuery
                .GroupBy(d => d.DichVu.TenDichVu)
                .Select(g => new { tenDichVu = g.Key, tongTien = g.Sum(d => d.TongTien ?? 0) })
                .OrderByDescending(x => x.tongTien)
                .Take(10)
                .ToList();

            // Tổng doanh thu
            IQueryable<ThanhToan> ttQuery = db.ThanhToans.Where(t => t.NgayGiaoDich.HasValue);
            if (dateFrom.HasValue && dateTo.HasValue)
                ttQuery = ttQuery.Where(t => t.NgayGiaoDich.Value >= dateFrom.Value && t.NgayGiaoDich.Value <= dateTo.Value);
            else if (thang.HasValue)
                ttQuery = ttQuery.Where(t => t.NgayGiaoDich.Value.Year == year && t.NgayGiaoDich.Value.Month == thang.Value);
            else
                ttQuery = ttQuery.Where(t => t.NgayGiaoDich.Value.Year == year);

            var tongDoanhThu = ttQuery.Sum(t => (decimal?)t.SoTienThanhToan) ?? 0;

            IQueryable<DatLich> dlQuery = db.DatLiches;
            if (dateFrom.HasValue && dateTo.HasValue)
                dlQuery = dlQuery.Where(d => d.NgayChup >= dateFrom.Value && d.NgayChup <= dateTo.Value);
            else if (thang.HasValue)
                dlQuery = dlQuery.Where(d => d.NgayChup.Year == year && d.NgayChup.Month == thang.Value);
            else
                dlQuery = dlQuery.Where(d => d.NgayChup.Year == year);

            var tongDatLich = dlQuery.Count(d => d.TrangThai != "Đã hủy");
            var tongHuy = dlQuery.Count(d => d.TrangThai == "Đã hủy");
            var tbSao = db.DanhGias.Any() ? db.DanhGias.Average(d => (double)d.SoSao) : 0;

            return Json(new
            {
                success = true,
                doanhThuTheoThang,
                doanhThuTheoDV,
                tongDoanhThu,
                tongDatLich,
                tongHuy,
                tbSao
            }, JsonRequestBehavior.AllowGet);
        }

        // ─────────────────────────────────────────────
        // QUẢN LÝ DỊCH VỤ
        // ─────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetDichVu()
        {
            if (!IsAdmin()) return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            var list = db.DichVus.Include("DanhMucDichVu").ToList().Select(d => new
            {
                d.MaDichVu,
                d.TenDichVu,
                d.GiaTien,
                d.GioiHanTho,
                d.MoTa,
                d.LinkAnhDaiDien,
                MaDanhMuc = d.MaDanhMuc ?? 0,
                TenDanhMuc = d.DanhMucDichVu != null ? d.DanhMucDichVu.TenDanhMuc : "N/A"
            });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ThemDichVu(string tenDichVu, decimal giaTien, int gioiHanTho, string moTa, string linkAnh, int maDanhMuc)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var dv = new DichVu
            {
                TenDichVu = tenDichVu,
                GiaTien = giaTien,
                GioiHanTho = gioiHanTho,
                MoTa = moTa,
                LinkAnhDaiDien = linkAnh,
                MaDanhMuc = maDanhMuc
            };
            db.DichVus.Add(dv);
            db.SaveChanges();
            return Json(new { success = true, id = dv.MaDichVu });
        }

        [HttpPost]
        public JsonResult SuaDichVu(int maDichVu, string tenDichVu, decimal giaTien, int gioiHanTho, string moTa, string linkAnh, int maDanhMuc)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var dv = db.DichVus.Find(maDichVu);
            if (dv == null) return Json(new { success = false });

            dv.TenDichVu = tenDichVu;
            dv.GiaTien = giaTien;
            dv.GioiHanTho = gioiHanTho;
            dv.MoTa = moTa;
            dv.LinkAnhDaiDien = linkAnh;
            dv.MaDanhMuc = maDanhMuc;
            db.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult XoaDichVu(int maDichVu)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var dv = db.DichVus.Find(maDichVu);
            if (dv == null) return Json(new { success = false });

            db.DichVus.Remove(dv);
            db.SaveChanges();
            return Json(new { success = true });
        }

        // ─────────────────────────────────────────────
        // QUẢN LÝ PORTFOLIO
        // ─────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetPortfolio()
        {
            if (!IsAdmin()) return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            var list = db.Portfolios.Include("DichVu").ToList().Select(p => new
            {
                p.MaAnh,
                p.TieuDe,
                p.LinkFileAnh,
                MaDichVu = p.MaDichVu ?? 0,
                TenDichVu = p.DichVu != null ? p.DichVu.TenDichVu : "N/A"
            });
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ThemPortfolio(string tieuDe, int? maDichVu)
        {
            if (!IsAdmin()) return Json(new { success = false });

            string linkFileAnh = "";
            var fileAnh = Request.Files["fileAnh"];
            if (fileAnh != null && fileAnh.ContentLength > 0)
            {
                try
                {
                    string folder = Server.MapPath("~/Content/Uploads/Portfolio/");
                    if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);
                    string ext = System.IO.Path.GetExtension(fileAnh.FileName);
                    string fileName = DateTime.Now.Ticks + ext;
                    fileAnh.SaveAs(System.IO.Path.Combine(folder, fileName));
                    linkFileAnh = "/Content/Uploads/Portfolio/" + fileName;
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            var p = new Portfolio { TieuDe = tieuDe, LinkFileAnh = linkFileAnh, MaDichVu = maDichVu ?? 0 };
            db.Portfolios.Add(p);
            db.SaveChanges();
            return Json(new { success = true, id = p.MaAnh });
        }

        [HttpPost]
        public JsonResult SuaPortfolio(int maAnh, string tieuDe, int? maDichVu)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var p = db.Portfolios.Find(maAnh);
            if (p == null) return Json(new { success = false, message = "Khong tim thay" });

            p.TieuDe = tieuDe;
            p.MaDichVu = maDichVu;

            var fileAnh = Request.Files["fileAnh"];
            if (fileAnh != null && fileAnh.ContentLength > 0)
            {
                try
                {
                    string folder = Server.MapPath("~/Content/Uploads/Portfolio/");
                    if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);
                    string ext = System.IO.Path.GetExtension(fileAnh.FileName);
                    string fileName = DateTime.Now.Ticks + ext;
                    fileAnh.SaveAs(System.IO.Path.Combine(folder, fileName));
                    p.LinkFileAnh = "/Content/Uploads/Portfolio/" + fileName;
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }

            db.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult XoaPortfolio(int maAnh)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var p = db.Portfolios.Find(maAnh);
            if (p == null) return Json(new { success = false });

            db.Portfolios.Remove(p);
            db.SaveChanges();
            return Json(new { success = true });
        }

        // ─────────────────────────────────────────────
        // QUẢN LÝ DỊCH VỤ BỔ SUNG
        // ─────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetDichVuBoSung()
        {
            if (!IsAdmin()) return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            var list = db.DichVuBoSungs.Include("DichVu").ToList().Select(d => new
            {
                d.MaDVBS,
                d.TenDVBS,
                d.MoTa,
                d.GiaTien,
                MaDichVu = d.MaDichVu ?? 0,
                TenDichVu = d.DichVu != null ? d.DichVu.TenDichVu : "N/A"
            });
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ThemDichVuBoSung(string tenDVBS, string moTa, decimal giaTien, int maDichVu)
        {
            if (!IsAdmin()) return Json(new { success = false });
            var d = new DichVuBoSung { TenDVBS = tenDVBS, MoTa = moTa, GiaTien = giaTien, MaDichVu = maDichVu };
            db.DichVuBoSungs.Add(d);
            db.SaveChanges();
            return Json(new { success = true, id = d.MaDVBS });
        }

        [HttpPost]
        public JsonResult SuaDichVuBoSung(int maDVBS, string tenDVBS, string moTa, decimal giaTien, int maDichVu)
        {
            if (!IsAdmin()) return Json(new { success = false });
            var d = db.DichVuBoSungs.Find(maDVBS);
            if (d == null) return Json(new { success = false });
            d.TenDVBS = tenDVBS; d.MoTa = moTa; d.GiaTien = giaTien; d.MaDichVu = maDichVu;
            db.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult XoaDichVuBoSung(int maDVBS)
        {
            if (!IsAdmin()) return Json(new { success = false });
            var d = db.DichVuBoSungs.Find(maDVBS);
            if (d == null) return Json(new { success = false });
            db.DichVuBoSungs.Remove(d);
            db.SaveChanges();
            return Json(new { success = true });
        }

        // ─────────────────────────────────────────────
        // QUẢN LÝ LỊCH CHỤP (Scheduling)
        // ─────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetLichChup(int? maDichVu)
        {
            if (!IsAdmin()) return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            IQueryable<LichChup> query = db.LichChups.Include("DichVu");

            if (maDichVu.HasValue)
                query = query.Where(l => l.MaDichVu == maDichVu);

            var list = query
                .OrderBy(l => l.NgayChup)
                .ToList()
                .Select(l => new
                {
                    l.MaLichChup,
                    NgayChup = l.NgayChup.ToString("dd/MM/yyyy"),
                    DichVu = l.DichVu != null ? l.DichVu.TenDichVu : "N/A",
                    MaDichVu = l.MaDichVu ?? 0,
                    l.SoLichToiDa,
                    l.SoLichConLai,
                    l.GhiChu,
                    l.TrangThai,
                    NgayTao = l.NgayTao.HasValue ? l.NgayTao.Value.ToString("dd/MM/yyyy HH:mm") : "",
                    SoKhungGio = db.KhungGioLiches.Count(k => k.MaLichChup == l.MaLichChup)
                });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ThemLichChup(int maDichVu, string ngayChup, int soLichToiDa, string ghiChu)
        {
            if (!IsAdmin()) return Json(new { success = false });

            if (!DateTime.TryParse(ngayChup, out DateTime date))
                return Json(new { success = false, message = "Invalid date format" });

            // Check if date already exists for this service
            var existing = db.LichChups.FirstOrDefault(l =>
                l.MaDichVu == maDichVu &&
                l.NgayChup == date.Date &&
                l.TrangThai != "Đã hủy");

            if (existing != null)
                return Json(new { success = false, message = "Lịch chụp này đã tồn tại" });

            var lichChup = new LichChup
            {
                MaDichVu = maDichVu,
                NgayChup = date.Date,
                SoLichToiDa = soLichToiDa,
                SoLichConLai = soLichToiDa,
                GhiChu = ghiChu,
                TrangThai = "Hoạt động",
                NgayTao = DateTime.Now
            };

            db.LichChups.Add(lichChup);
            db.SaveChanges();

            // Auto-confirm matching pending bookings
            AutoConfirmMatchingBookings(lichChup);

            return Json(new { success = true, id = lichChup.MaLichChup });
        }

        [HttpPost]
        public JsonResult SuaLichChup(int maLichChup, int maDichVu, string ngayChup, int soLichToiDa, string ghiChu)
        {
            if (!IsAdmin()) return Json(new { success = false });

            if (!DateTime.TryParse(ngayChup, out DateTime date))
                return Json(new { success = false, message = "Invalid date format" });

            var lichChup = db.LichChups.Find(maLichChup);
            if (lichChup == null)
                return Json(new { success = false, message = "Not found" });

            // Check for duplicate date for same service (excluding current record)
            var existing = db.LichChups.FirstOrDefault(l =>
                l.MaDichVu == maDichVu &&
                l.NgayChup == date.Date &&
                l.MaLichChup != maLichChup &&
                l.TrangThai != "Đã hủy");

            if (existing != null)
                return Json(new { success = false, message = "Lịch chụp này đã tồn tại" });

            int originalCapacity = lichChup.SoLichToiDa;
            lichChup.MaDichVu = maDichVu;
            lichChup.NgayChup = date.Date;
            lichChup.SoLichToiDa = soLichToiDa;
            lichChup.GhiChu = ghiChu;
            lichChup.NgayCapNhat = DateTime.Now;

            // Recalculate remaining slots
            int bookedSlots = db.DatLiches.Count(d =>
                d.MaLichChup == maLichChup &&
                d.TrangThai == "Đã xác nhận");

            lichChup.SoLichConLai = Math.Max(0, soLichToiDa - bookedSlots);

            // Mark as full if slots are exceeded
            if (lichChup.SoLichConLai <= 0)
                lichChup.TrangThai = "Đã đầy";
            else if (lichChup.TrangThai == "Đã đầy")
                lichChup.TrangThai = "Hoạt động";

            db.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult XoaLichChup(int maLichChup)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var lichChup = db.LichChups.Find(maLichChup);
            if (lichChup == null)
                return Json(new { success = false });

            // Check if there are confirmed bookings
            int bookedCount = db.DatLiches.Count(d =>
                d.MaLichChup == maLichChup &&
                d.TrangThai == "Đã xác nhận");

            if (bookedCount > 0)
                return Json(new { success = false, message = "Không thể xóa lịch đã có đặt chụp" });

            lichChup.TrangThai = "Đã hủy";
            db.SaveChanges();
            return Json(new { success = true });
        }

        // Auto-confirm pending bookings that match the schedule
        private void AutoConfirmMatchingBookings(LichChup lichChup)
        {
            var pendingBookings = db.DatLiches
                .Where(d =>
                    d.MaDichVu == lichChup.MaDichVu &&
                    d.NgayChup == lichChup.NgayChup &&
                    d.TrangThai == "Chờ xác nhận" &&
                    d.MaLichChup == null)
                .OrderBy(d => d.NgayHeThongGhiNhan)
                .Take(lichChup.SoLichConLai)
                .ToList();

            foreach (var booking in pendingBookings)
            {
                booking.TrangThai = "Đã xác nhận";
                booking.MaLichChup = lichChup.MaLichChup;
                lichChup.SoLichConLai--;

                if (lichChup.SoLichConLai <= 0)
                {
                    lichChup.TrangThai = "Đã đầy";
                    break;
                }
            }

            db.SaveChanges();
        }

        // Manual confirm pending booking to a schedule
        [HttpPost]
        public JsonResult XacNhanDatLichVaoLichChup(int maDatLich, int maLichChup)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var booking = db.DatLiches.Find(maDatLich);
            var lichChup = db.LichChups.Find(maLichChup);

            if (booking == null || lichChup == null)
                return Json(new { success = false, message = "Not found" });

            if (booking.TrangThai != "Chờ xác nhận")
                return Json(new { success = false, message = "Booking must be pending" });

            if (lichChup.SoLichConLai <= 0)
                return Json(new { success = false, message = "No available slots" });

            if (booking.NgayChup != lichChup.NgayChup || booking.MaDichVu != lichChup.MaDichVu)
                return Json(new { success = false, message = "Date or service mismatch" });

            booking.TrangThai = "Đã xác nhận";
            booking.MaLichChup = maLichChup;
            lichChup.SoLichConLai--;

            if (lichChup.SoLichConLai <= 0)
                lichChup.TrangThai = "Đã đầy";

            db.SaveChanges();
            return Json(new { success = true });
        }

        // ─────────────────────────────────────────────
        // DANH MỤC DỊCH VỤ (for dropdowns)
        // ─────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetDanhMuc()
        {
            if (!IsAdmin()) return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            var list = db.DanhMucDichVus.Select(d => new { d.MaDanhMuc, d.TenDanhMuc }).ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        // ─────────────────────────────────────────────
        // QUẢN LÝ KHUNG GIỜ (KhungGioLich)
        // ─────────────────────────────────────────────
        [HttpGet]
        public JsonResult GetKhungGioByLich(int maLichChup)
        {
            if (!IsAdmin()) return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            var list = db.KhungGioLiches
                .Where(k => k.MaLichChup == maLichChup)
                .OrderBy(k => k.GioBatDau)
                .ToList()
                .Select(k => new
                {
                    k.MaKhungGio,
                    k.MaLichChup,
                    k.GioBatDau,
                    k.GioKetThuc,
                    k.TrangThai,
                    k.GhiChu,
                    NgayTao = k.NgayTao.HasValue ? k.NgayTao.Value.ToString("dd/MM/yyyy HH:mm") : ""
                });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ThemKhungGio(int maLichChup, string gioBatDau, string gioKetThuc, string ghiChu)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var existing = db.KhungGioLiches.FirstOrDefault(k => k.MaLichChup == maLichChup && k.GioBatDau == gioBatDau && k.GioKetThuc == gioKetThuc);
            if (existing != null)
                return Json(new { success = false, message = "Khung giờ này đã tồn tại trong lịch chụp!" });

            var khungGio = new KhungGioLich
            {
                MaLichChup = maLichChup,
                GioBatDau = gioBatDau,
                GioKetThuc = gioKetThuc,
                TrangThai = "Hoạt động",
                GhiChu = ghiChu,
                NgayTao = DateTime.Now
            };
            db.KhungGioLiches.Add(khungGio);
            db.SaveChanges();
            return Json(new { success = true, id = khungGio.MaKhungGio });
        }

        [HttpPost]
        public JsonResult KhoaKhungGio(int maKhungGio, string lyDo)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var khungGio = db.KhungGioLiches.Find(maKhungGio);
            if (khungGio == null) return Json(new { success = false, message = "Không tìm thấy khung giờ" });

            khungGio.TrangThai = (khungGio.TrangThai == "Hoạt động") ? "Đã khóa" : "Hoạt động";
            if (!string.IsNullOrEmpty(lyDo)) khungGio.GhiChu = lyDo;
            db.SaveChanges();

            return Json(new { success = true, trangThaiMoi = khungGio.TrangThai });
        }

        [HttpPost]
        public JsonResult XoaKhungGio(int maKhungGio)
        {
            if (!IsAdmin()) return Json(new { success = false });

            var khungGio = db.KhungGioLiches.Find(maKhungGio);
            if (khungGio == null) return Json(new { success = false });

            db.KhungGioLiches.Remove(khungGio);
            db.SaveChanges();
            return Json(new { success = true });
        }

        /// <summary>API cho user booking: lấy khung giờ theo ngày + dịch vụ</summary>
        [HttpGet]
        public JsonResult GetKhungGioAvailable(string date, int serviceId)
        {
            if (string.IsNullOrEmpty(date))
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            if (!DateTime.TryParse(date, out DateTime parsedDate))
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            var lichChup = db.LichChups
                .Where(lc => lc.MaDichVu == serviceId
                    && DbFunctions.TruncateTime(lc.NgayChup) == DbFunctions.TruncateTime(parsedDate)
                    && lc.TrangThai != "Đã hủy")
                .FirstOrDefault();

            if (lichChup == null)
                return Json(new { success = true, hasSchedule = false, slots = new object[0] }, JsonRequestBehavior.AllowGet);

            var slots = db.KhungGioLiches
                .Where(k => k.MaLichChup == lichChup.MaLichChup)
                .OrderBy(k => k.GioBatDau)
                .ToList()
                .Select(k => new
                {
                    k.GioBatDau,
                    k.GioKetThuc,
                    isLocked = k.TrangThai == "Đã khóa",
                    k.GhiChu
                })
                .ToList();

            return Json(new
            {
                success = true,
                hasSchedule = true,
                maLichChup = lichChup.MaLichChup,
                soLichConLai = lichChup.SoLichConLai,
                slots
            }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
