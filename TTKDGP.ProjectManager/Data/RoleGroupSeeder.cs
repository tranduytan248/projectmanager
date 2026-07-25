using System;
using System.Collections.Generic;
using System.Linq;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Data
{
    /// <summary>
    /// Tạo ba nhóm quyền mặc định khi bảng RoleGroups còn trống. Mã nhóm giữ đúng
    /// Admin/Manager/Reporter để dữ liệu tài khoản hiện có (User.Role) khớp ngay.
    /// </summary>
    public static class RoleGroupSeeder
    {
        public static void EnsureDefaults()
        {
            try
            {
                if (Repository.RoleGroups.All().Any()) return;

                Repository.RoleGroups.Insert(new RoleGroup
                {
                    Code = Roles.Admin,
                    Name = "Quản trị",
                    Description = "Toàn quyền hệ thống, gồm cả cấu hình nhóm quyền và người dùng.",
                    Permissions = "*",
                    IsActive = true,
                    SortOrder = 1
                });

                Repository.RoleGroups.Insert(new RoleGroup
                {
                    Code = Roles.Manager,
                    Name = "Quản lý",
                    Description = "Cập nhật dự án, thành viên, phân công, báo cáo nhân sự và danh mục.",
                    Permissions = string.Join(",", Permissions.ManagerDefaults()),
                    IsActive = true,
                    SortOrder = 2
                });

                Repository.RoleGroups.Insert(new RoleGroup
                {
                    Code = Roles.Reporter,
                    Name = "Báo cáo công việc",
                    Description = "Chỉ xem màn Tổng hợp và báo cáo phần việc của chính mình.",
                    Permissions = string.Join(",", Permissions.ReporterDefaults()),
                    IsActive = true,
                    SortOrder = 3
                });
            }
            catch (Exception)
            {
                // Không để lỗi hạ tầng (SQL chập chờn) làm sập lúc khởi động; lần chạy sau seed lại.
            }
        }
    }
}
