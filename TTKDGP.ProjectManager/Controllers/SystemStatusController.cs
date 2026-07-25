using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Controllers
{
    /// <summary>
    /// Dashboard "Tình trạng hệ thống" cho Quản trị: việc cần xử lý ở tầng quản trị và trạng thái
    /// các luồng tự động. Mọi số liệu lấy từ cấu hình và dữ liệu thật.
    /// </summary>
    [AppAuthorize]
    public class SystemStatusController : BaseController
    {
        [AppAuthorize(Permission = "users.view")]
        public ActionResult Index()
        {
            var users = Repository.Users.All();
            var groups = Repository.RoleGroups.All()
                .OrderBy(g => g.SortOrder)
                .ThenBy(g => g.Name, StringComparer.CurrentCulture)
                .ToList();

            // Tài khoản cần báo cáo (nhóm có quyền myreports.report) nhưng chưa gắn nhân sự —
            // sẽ không thấy phân công để báo cáo. Đây mới là vấn đề thực sự cần xử lý.
            var unlinked = users
                .Where(u => u.IsActive && u.MemberId <= 0
                            && Permissions.UserHas(u.Role, Permissions.MyReports.Perm("report")))
                .OrderBy(u => u.UserName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var model = new AdminSystemViewModel
            {
                UnlinkedUsers = unlinked,
                TotalUsers = users.Count,
                ActiveIntegrations = Repository.IntegrationSystems.All().Count(s => s.IsActive),
                Flows = BuildFlows(),
                GroupCounts = groups.Select(g => new GroupUserCount
                {
                    Name = g.Name,
                    Count = users.Count(u => Models.Roles.Has(u.Role, g.Code))
                }).ToList()
            };

            return View(model);
        }

        private static List<AutomationStatus> BuildFlows()
        {
            var flows = new List<AutomationStatus>();

            // Nhắc báo cáo qua Telegram
            if (!AppSettings.Telegram.IsConfigured)
            {
                flows.Add(new AutomationStatus { Name = "Nhắc báo cáo qua Telegram", State = "off", Detail = "Chưa cấu hình bot/nhóm nhận." });
            }
            else if (AppSettings.Reminder.AutoSend)
            {
                flows.Add(new AutomationStatus { Name = "Nhắc báo cáo qua Telegram", State = "ok", Detail = "Đang bật, tự gửi theo lịch." });
            }
            else
            {
                flows.Add(new AutomationStatus { Name = "Nhắc báo cáo qua Telegram", State = "warn", Detail = "Đã cấu hình nhưng tự gửi theo lịch đang TẮT." });
            }

            // Mail nhắc báo cáo cho từng thành viên
            flows.Add(AppSettings.Email.IsConfigured
                ? new AutomationStatus { Name = "Mail nhắc báo cáo", State = "ok", Detail = "Đã cấu hình hộp thư gửi." }
                : new AutomationStatus { Name = "Mail nhắc báo cáo", State = "off", Detail = "Chưa bật hoặc chưa cấu hình SMTP." });

            // Tự đăng nhập GoConnect
            if (!AppSettings.GoConnect.Enabled)
            {
                flows.Add(new AutomationStatus { Name = "Tự đăng nhập GoConnect", State = "off", Detail = "Đang tắt trên bản chạy này." });
            }
            else
            {
                var changed = GoConnectAutoLogin.LastChangedAt == default(DateTime)
                    ? "" : " · cập nhật " + GoConnectAutoLogin.LastChangedAt.ToString("HH:mm dd/MM");
                flows.Add(new AutomationStatus
                {
                    Name = "Tự đăng nhập GoConnect",
                    State = "ok",
                    Detail = "Trạng thái: " + GoConnectAutoLogin.State + changed
                });
            }

            // Hệ thống tích hợp API
            var active = Repository.IntegrationSystems.All().Count(s => s.IsActive);
            flows.Add(new AutomationStatus
            {
                Name = "API tích hợp",
                State = active > 0 ? "ok" : "warn",
                Detail = active > 0 ? active + " hệ thống đang hoạt động." : "Chưa có hệ thống nào đang hoạt động."
            });

            return flows;
        }
    }
}
