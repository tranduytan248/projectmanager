using System.Diagnostics;
using System.Web.Hosting;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Data
{
    /// <summary>
    /// Nạp dữ liệu từ các file JSON trong App_Data sang SQL Server MỘT LẦN, khi bảng còn trống.
    /// Giữ lại hành vi "chạy được ngay" của bản cũ: máy mới dựng lên, bảng SQL tự tạo rồi
    /// được nạp từ dữ liệu mẫu/dữ liệu JSON sẵn có. Bảng đã có dữ liệu thì bỏ qua, không nạp đè.
    /// </summary>
    public static class JsonToSqlMigration
    {
        private static bool _done;
        private static readonly object Gate = new object();

        public static void RunIfNeeded()
        {
            if (_done) return;

            lock (Gate)
            {
                if (_done) return;

                try
                {
                    Migrate(Repository.Members, "members.json");
                    Migrate(Repository.Projects, "projects.json");
                    Migrate(Repository.Assignments, "projectMembers.json");
                    Migrate(Repository.Users, "users.json");
                    Migrate(Repository.ProjectTypes, "projectTypes.json");
                    Migrate(Repository.ProjectStatuses, "projectStatuses.json");
                    Migrate(Repository.WorkStatuses, "workStatuses.json");
                    Migrate(Repository.MemberRoles, "memberRoles.json");
                    Migrate(Repository.WorkLogs, "workLogs.json");
                    Migrate(Repository.ReminderLogs, "reminderLogs.json");
                }
                catch (System.Exception ex)
                {
                    // Không để lỗi nạp dữ liệu làm sập lúc khởi động; lỗi thao tác thật sẽ nổi ở từng request.
                    Debug.WriteLine("JsonToSqlMigration bỏ dở: " + ex.Message);
                }

                _done = true;
            }
        }

        private static void Migrate<T>(SqlStore<T> store, string jsonFile) where T : class, IEntity
        {
            // Chỉ nạp khi bảng SQL còn trống, tránh ghi đè lên dữ liệu đang dùng.
            if (store.Count() > 0) return;

            var path = HostingEnvironment.MapPath("~/App_Data/" + jsonFile);
            if (path == null) return;

            var source = new JsonStore<T>(path);
            foreach (var item in source.All())
            {
                store.InsertPreservingId(item);
            }
        }
    }
}
