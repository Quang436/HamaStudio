using System;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity;
using HamaStudio.Models;

namespace HamaStudio.Services
{
    public class ReminderService
    {
        public static async Task SendRemindersAsync()
        {
            using (var db = new HamaStudioDbContext())
            {
                // Lấy ngày mai
                DateTime tomorrow = DateTime.Now.AddDays(1).Date;

                // Tìm các lịch đặt có ngày chụp là ngày mai và trạng thái đã xác nhận/cọc
                var upcomingBookings = await db.DatLiches
                    .Include(d => d.KhachHang)
                    .Include(d => d.DichVu)
                    .Where(d => DbFunctions.TruncateTime(d.NgayChup) == tomorrow
                           && (d.TrangThai == "Đã xác nhận" || d.TrangThai == "Đã đặt cọc"))
                    .ToListAsync();

                foreach (var booking in upcomingBookings)
                {
                    if (booking.KhachHang != null && !string.IsNullOrEmpty(booking.KhachHang.Email))
                    {
                        string subject = "[HAMA STUDIO] THÔNG BÁO NHẮC LỊCH CHỤP ẢNH";
                        string body = $@"
<div style='background-color: #f4f4f4; padding: 40px 0; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.1); border-top: 5px solid #8c8574;'>
        <div style='padding: 30px; text-align: center; background-color: #faf9f7;'>
            <h1 style='color: #8c8574; margin: 0; font-size: 24px; letter-spacing: 2px; text-transform: uppercase;'>Hama Studio</h1>
            <p style='color: #a09a8e; font-size: 14px; margin-top: 5px;'>Capturing your beautiful moments</p>
        </div>
        <div style='padding: 40px;'>
            <p style='font-size: 18px; color: #333;'>Thân chào <strong>{booking.KhachHang.HoTen}</strong>,</p>
            <p style='color: #666; line-height: 1.6;'>Hama Studio rất mong chờ buổi gặp gỡ vào ngày mai. Để buổi chụp diễn ra thuận tiện nhất, chúng tôi xin gửi lại thông tin chi tiết lịch hẹn của bạn:</p>
            
            <div style='background-color: #faf9f7; border-radius: 8px; padding: 25px; margin: 30px 0; border-left: 3px solid #8c8574;'>
                <table style='width: 100%; border-collapse: collapse;'>
                    <tr>
                        <td style='padding: 10px 0; color: #8c8574; font-weight: bold; width: 120px;'>Gói dịch vụ:</td>
                        <td style='padding: 10px 0; color: #333;'>{booking.DichVu.TenDichVu}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; color: #8c8574; font-weight: bold;'>Ngày chụp:</td>
                        <td style='padding: 10px 0; color: #333; font-size: 16px;'><strong>{booking.NgayChup:dd/MM/yyyy}</strong></td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; color: #8c8574; font-weight: bold;'>Khung giờ:</td>
                        <td style='padding: 10px 0; color: #e67e22; font-weight: bold;'>{booking.KhungGio}</td>
                    </tr>
                    <tr>
                        <td style='padding: 10px 0; color: #8c8574; font-weight: bold;'>Địa điểm:</td>
                        <td style='padding: 10px 0; color: #333;'>{booking.DiaDiem}</td>
                    </tr>
                </table>
            </div>

            <p style='color: #666; line-height: 1.6; font-style: italic;'>* Quý khách vui lòng có mặt đúng giờ hoặc sớm hơn 15 phút để chuẩn bị tốt nhất. Trân trọng!</p>
            
            <div style='margin-top: 40px; padding-top: 20px; border-top: 1px solid #eee; text-align: center;'>
                <p style='margin: 0; color: #8c8574; font-weight: bold;'>HAMA STUDIO</p>
                <p style='margin: 5px 0 0 0; font-size: 12px; color: #aaa;'>Hotline: 0901 112 223 | www.hamastudio.com</p>
            </div>
        </div>
    </div>
</div>";
                        await EmailService.SendEmailAsync(booking.KhachHang.Email, subject, body);
                    }
                }
            }
        }
    }
}
