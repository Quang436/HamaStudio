using System;
using System.Linq;
using System.Web.Mvc;
using HamaStudio.Models;
using System.Data.Entity;

namespace HamaStudio.Controllers
{
    public class BookingController : Controller
    {
        private HamaStudioDbContext db = new HamaStudioDbContext();

        [HttpGet]
        public ActionResult Index(int? serviceId)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Services = db.DichVus.Include(d => d.DichVuBoSungs).ToList();
            ViewBag.SelectedServiceId = serviceId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(int MaDichVu, DateTime NgayChup, string KhungGio, string DiaDiem, string GhiChu, string HinhThucThanhToan, string PhuongThucThanhToan, decimal SoTienThanhToan, decimal TongTienThucTe, string SoDienThoai)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                int userId = (int)Session["UserId"];
                var dichVu = db.DichVus.Find(MaDichVu);
                if (dichVu == null)
                {
                    ViewBag.Error = "Dịch vụ không tồn tại.";
                    ViewBag.Services = db.DichVus.Include(d => d.DichVuBoSungs).ToList();
                    return View();
                }

                // Cập nhật SĐT cho khách hàng nếu có
                if (!string.IsNullOrEmpty(SoDienThoai))
                {
                    var khachHang = db.KhachHangs.Find(userId);
                    if (khachHang != null)
                    {
                        khachHang.SoDienThoai = SoDienThoai;
                        db.SaveChanges();
                    }
                }

                // Thực hiện lưu trong 1 Transaction để đảm bảo tính toàn vẹn
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        DatLich datLich = new DatLich
                        {
                            MaKhachHang = userId,
                            MaDichVu = MaDichVu,
                            NgayChup = NgayChup,
                            KhungGio = KhungGio,
                            DiaDiem = DiaDiem,
                            GhiChu = GhiChu,
                            TrangThai = "Chờ xác nhận",
                            TongTien = TongTienThucTe,
                            SoTienDaCoc = Math.Round(TongTienThucTe * 0.3m),
                            NgayHeThongGhiNhan = DateTime.Now
                        };

                        db.DatLiches.Add(datLich);
                        db.SaveChanges(); // Tạo MaDatLich và kích hoạt trigger nếu có

                        // Lưu thông tin giao dịch thanh toán
                        ThanhToan thanhToan = new ThanhToan
                        {
                            MaDatLich = datLich.MaDatLich,
                            SoTienThanhToan = SoTienThanhToan,
                            PhuongThuc = PhuongThucThanhToan + " (" + (HinhThucThanhToan == "DatCoc" ? "Cọc trước 30%" : "Thanh toán 100%") + ")",
                            NoiDungThanhToan = "HAMA LD" + datLich.MaDatLich.ToString("D4") + " " + (HinhThucThanhToan == "DatCoc" ? "COC 30%" : "FULL 100%"),
                            NgayGiaoDich = DateTime.Now
                        };

                        db.ThanhToans.Add(thanhToan);
                        db.SaveChanges();

                        transaction.Commit();
                        return RedirectToAction("MyBookings");
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi khi đặt lịch: " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                ViewBag.Services = db.DichVus.ToList();
                ViewBag.SelectedServiceId = MaDichVu;
                return View();
            }
        }

        public ActionResult MyBookings()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];
            var bookings = db.DatLiches
                             .Include("DichVu")
                             .Include("ThanhToans")
                             .Include("DanhGias")
                             .Where(b => b.MaKhachHang == userId)
                             .OrderByDescending(b => b.NgayHeThongGhiNhan)
                             .ToList();
            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelBooking(int bookingId)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];
            var booking = db.DatLiches.FirstOrDefault(b => b.MaDatLich == bookingId && b.MaKhachHang == userId);

            if (booking == null)
            {
                return HttpNotFound();
            }

            if (booking.TrangThai != null && (booking.TrangThai.Trim() == "Chờ xác nhận" || booking.TrangThai.Trim() == "Đã xác nhận" || booking.TrangThai.Trim() == "Đã đặt cọc"))
            {
                booking.TrangThai = "Đã hủy";
                db.SaveChanges();
            }

            return RedirectToAction("MyBookings");
        }

        [HttpGet]
        public ActionResult PayRemaining(int id)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];
            var booking = db.DatLiches.Include("DichVu").FirstOrDefault(b => b.MaDatLich == id && b.MaKhachHang == userId);

            if (booking == null)
            {
                return HttpNotFound();
            }

            // Tính số tiền đã thanh toán từ bảng ThanhToan
            decimal totalPaid = db.ThanhToans.Where(t => t.MaDatLich == id).Sum(t => (decimal?)t.SoTienThanhToan) ?? 0;
            decimal remainingAmount = (booking.TongTien ?? 0) - totalPaid;

            if (remainingAmount <= 0)
            {
                return RedirectToAction("MyBookings");
            }

            ViewBag.Booking = booking;
            ViewBag.RemainingAmount = remainingAmount;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PayRemaining(int id, string PhuongThucThanhToan, decimal SoTienThanhToan)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];
            var booking = db.DatLiches.FirstOrDefault(b => b.MaDatLich == id && b.MaKhachHang == userId);

            if (booking == null)
            {
                return HttpNotFound();
            }

            ThanhToan thanhToan = new ThanhToan
            {
                MaDatLich = id,
                SoTienThanhToan = SoTienThanhToan,
                PhuongThuc = PhuongThucThanhToan + " (Thanh toán nốt 70%)",
                NoiDungThanhToan = "HAMA LD" + id.ToString("D4") + " NOT 70%",
                NgayGiaoDich = DateTime.Now
            };

            db.ThanhToans.Add(thanhToan);
            db.SaveChanges();

            return RedirectToAction("MyBookings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddReview(int bookingId, int rating, string comment)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];
            var booking = db.DatLiches.FirstOrDefault(b => b.MaDatLich == bookingId && b.MaKhachHang == userId);

            if (booking == null)
            {
                return HttpNotFound();
            }

            if (booking.TrangThai != null && booking.TrangThai.Trim() == "Hoàn thành")
            {
                // Kiểm tra xem đã đánh giá chưa
                var existingReview = db.DanhGias.FirstOrDefault(d => d.MaDatLich == bookingId);
                if (existingReview == null)
                {
                    var review = new DanhGia
                    {
                        MaDatLich = bookingId,
                        MaKhachHang = userId,
                        SoSao = rating,
                        NoiDungBinhLuan = comment,
                        NgayGui = DateTime.Now
                    };
                    db.DanhGias.Add(review);
                    db.SaveChanges();
                }
            }

            return RedirectToAction("MyBookings");
        }

        [HttpGet]
        public ActionResult GetBookedSlots(string date, int? serviceId)
        {
            if (string.IsNullOrEmpty(date) || serviceId == null)
            {
                return Json(new { success = false, message = "Date and Service ID are required." }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                DateTime parsedDate = DateTime.ParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                
                var dichVu = db.DichVus.Find(serviceId);
                if (dichVu == null) 
                {
                    return Json(new { success = false, message = "Service not found." }, JsonRequestBehavior.AllowGet);
                }
                
                int maxSlots = dichVu.GioiHanTho;

                // Đếm các lịch chụp đang hoạt động của RIÊNG dịch vụ này trong ngày được chọn
                var bookedSlots = db.DatLiches
                                    .Where(b => b.NgayChup == parsedDate && b.TrangThai != "Đã hủy" && b.MaDichVu == serviceId)
                                    .GroupBy(b => b.KhungGio)
                                    .Select(g => new { KhungGio = g.Key, Count = g.Count() })
                                    .ToList();

                // Lọc các khung giờ đã hết chỗ (số lượng đặt >= giới hạn thợ của dịch vụ)
                var fullyBookedSlots = bookedSlots
                                        .Where(s => s.Count >= maxSlots)
                                        .Select(s => s.KhungGio)
                                        .ToList();

                return Json(new { success = true, maxSlots = maxSlots, bookedSlots = bookedSlots, fullyBookedSlots = fullyBookedSlots }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateBookingAdmin(int bookingId, string trangThai, string linkAnh)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = db.DatLiches.Find(bookingId);
            if (booking == null)
            {
                return HttpNotFound();
            }

            booking.TrangThai = trangThai;
            booking.LinkAnhBanGiao = linkAnh;
            db.SaveChanges();

            return RedirectToAction("MyBookings");
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
