using System;
using System.Linq;
using System.Web.Mvc;
using HamaStudio.Models;

namespace HamaStudio.Controllers
{
    public class AccountController : Controller
    {
        private HamaStudioDbContext db = new HamaStudioDbContext();

        [HttpGet]
        public ActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string TenDangNhap, string MatKhau)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra Admin trước
                var admin = db.QuanTriViens.FirstOrDefault(a => a.TenDangNhap == TenDangNhap && a.MatKhau == MatKhau);
                if (admin != null)
                {
                    Session["UserId"] = admin.MaAdmin;
                    Session["Username"] = admin.TenDangNhap;
                    Session["FullName"] = admin.HoTen;
                    Session["IsAdmin"] = true;
                    return RedirectToAction("Index", "Admin");
                }

                var user = db.KhachHangs.FirstOrDefault(u => u.TenDangNhap == TenDangNhap && u.MatKhau == MatKhau);
                if (user != null)
                {
                    Session["UserId"] = user.MaKhachHang;
                    Session["Username"] = user.TenDangNhap;
                    Session["FullName"] = user.HoTen;
                    Session["IsAdmin"] = false;
                    return RedirectToAction("Index", "Home");
                }
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng.";
            }
            return View();
        }

        [HttpGet]
        public ActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(KhachHang khachHang, string firstName, string lastName, string confirmPassword)
        {
            // HoTen được tính từ firstName + lastName, không gửi qua form → xóa lỗi ModelState để không bị block
            ModelState.Remove("HoTen");
            // SoDienThoai không còn bắt buộc ở form đăng ký
            ModelState.Remove("SoDienThoai");

            if (ModelState.IsValid)
            {
                // Validate password
                if (khachHang.MatKhau != confirmPassword)
                {
                    ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                    return View(khachHang);
                }

                // Check if username exists
                if (db.KhachHangs.Any(k => k.TenDangNhap == khachHang.TenDangNhap))
                {
                    ViewBag.Error = "Tên đăng nhập đã tồn tại.";
                    return View(khachHang);
                }

                // Check if email exists
                if (!string.IsNullOrEmpty(khachHang.Email) && db.KhachHangs.Any(k => k.Email == khachHang.Email))
                {
                    ViewBag.Error = "Email đã được sử dụng.";
                    return View(khachHang);
                }

                khachHang.HoTen = lastName.Trim() + " " + firstName.Trim();
                khachHang.NgayDangKy = DateTime.Now;
                khachHang.TrangThai = "Hoạt động";

                db.KhachHangs.Add(khachHang);
                db.SaveChanges();

                return RedirectToAction("Login");
            }
            
            // Hiển thị lỗi ModelState nếu vẫn invalid
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m));
            ViewBag.Error = string.Join(" | ", errors);
            if (string.IsNullOrEmpty(ViewBag.Error))
            {
                ViewBag.Error = "Vui lòng kiểm tra lại thông tin đăng ký.";
            }
            return View(khachHang);
        }

        public ActionResult Profile()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login");
            }

            int userId = (int)Session["UserId"];
            var user = db.KhachHangs.Find(userId);
            return View(user);
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
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
