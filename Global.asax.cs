using System;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using HamaStudio.Services;

namespace HamaStudio
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Khởi chạy tiến trình nhắc lịch tự động chạy ngầm
            StartReminderTask();
        }

        private void StartReminderTask()
        {
            new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        // Kiểm tra vào 8h sáng mỗi ngày
                        var now = DateTime.Now;
                        if (now.Hour == 8)
                        {
                            ReminderService.SendRemindersAsync().Wait();
                            // Nghỉ 1h để không chạy lặp lại trong cùng khung giờ 8h
                            Thread.Sleep(3600000);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Reminder Task Error: " + ex.Message);
                    }
                    // Kiểm tra lại sau mỗi 30 phút
                    Thread.Sleep(1800000);
                }
            }).Start();
        }
    }
}
