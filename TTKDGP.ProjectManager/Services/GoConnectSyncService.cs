using System;
using System.Collections.Generic;
using System.Text;
using TTKDGP.ProjectManager.Data;
using TTKDGP.ProjectManager.Infrastructure;
using TTKDGP.ProjectManager.Models;

namespace TTKDGP.ProjectManager.Services
{
    /// <summary>Kết quả một đợt đồng bộ nhân sự, để hiển thị hoặc nhắn qua Telegram.</summary>
    public class GoConnectSyncResult
    {
        /// <summary>Số dòng GoConnect trả về.</summary>
        public int Fetched { get; set; }

        /// <summary>Số nhân sự đã ghi vào CSDL sau khi gộp trùng email.</summary>
        public int Employees { get; set; }

        /// <summary>Số đơn vị riêng biệt đã ghi.</summary>
        public int Workplaces { get; set; }

        /// <summary>Số chức danh riêng biệt đã ghi.</summary>
        public int Positions { get; set; }

        /// <summary>Số dòng bị bỏ vì thiếu email hoặc trùng email với dòng trước.</summary>
        public int Skipped { get; set; }

        public override string ToString()
        {
            var text = new StringBuilder();
            text.AppendFormat("Lấy về {0} dòng từ GoConnect.\n", Fetched);
            text.AppendFormat("• Nhân sự  : {0}\n", Employees);
            text.AppendFormat("• Đơn vị   : {0}\n", Workplaces);
            text.AppendFormat("• Chức danh: {0}", Positions);
            if (Skipped > 0) text.AppendFormat("\n• Bỏ qua   : {0} (thiếu email hoặc trùng)", Skipped);
            return text.ToString();
        }
    }

    /// <summary>
    /// Nối GoConnect với CSDL nội bộ: gọi API lấy danh sách nhân sự của một đơn vị, tách ra ba
    /// bảng (nhân sự, đơn vị, chức danh) rồi ghi xuống theo kiểu thêm-mới-hoặc-cập-nhật.
    ///
    /// Tách riêng khỏi <see cref="GoConnectService"/> (chỉ lo gọi API) và các lớp Store
    /// (chỉ lo ghi CSDL) để mỗi phần còn dùng lại được độc lập.
    /// </summary>
    public static class GoConnectSyncService
    {
        /// <summary>
        /// Đồng bộ bằng token của phiên đăng nhập vừa lưu trên đĩa (App_Data). Dùng ở máy local
        /// ngay sau khi /hrm đăng nhập xong. Ném <see cref="InvalidOperationException"/> nếu chưa
        /// có phiên dùng được.
        /// </summary>
        public static GoConnectSyncResult SyncFromSavedSession(Action<string> progress = null)
        {
            var tokens = GoConnectSession.Read();

            if (tokens == null)
                throw new InvalidOperationException(
                    "Chưa có phiên đăng nhập GoConnect nào được lưu. Gõ /hrm để đăng nhập trước.");

            return SyncFromTokens(tokens, "phiên đã lưu", progress);
        }

        /// <summary>
        /// Đồng bộ bằng token đọc từ CSDL. Đây là lối gọi trên bản chạy thật: /hrm bị khoá, token
        /// do máy local đăng nhập rồi lưu vào CSDL dùng chung. Ném
        /// <see cref="InvalidOperationException"/> nếu chưa có token hoặc token đã hết hạn.
        /// </summary>
        public static GoConnectSyncResult SyncFromDatabase(Action<string> progress = null)
        {
            var tokens = GoConnectTokenStore.Read();

            if (tokens == null)
                throw new InvalidOperationException(
                    "Chưa có token GoConnect trong cơ sở dữ liệu. Chạy /hrm dưới máy local để đăng nhập "
                    + "và lưu token trước, rồi mới /dongbo được ở đây.");

            return SyncFromTokens(tokens, "cơ sở dữ liệu", progress);
        }

        /// <summary>
        /// Đồng bộ bằng một cặp token có sẵn, sau khi đã kiểm hạn dùng và xác định đơn vị.
        /// <paramref name="source"/> chỉ để nhắc trong thông báo lỗi biết token đến từ đâu.
        /// </summary>
        private static GoConnectSyncResult SyncFromTokens(
            GoConnectTokens tokens, string source, Action<string> progress)
        {
            if (!tokens.IsUsable)
                throw new InvalidOperationException(string.Format(
                    "Token trong {0} đã hết hạn{1}. Chạy /hrm dưới máy local để lấy token mới.",
                    source,
                    tokens.AccessTokenExpiry.HasValue
                        ? " lúc " + tokens.AccessTokenExpiry.Value.ToString("HH:mm dd/MM/yyyy")
                        : ""));

            // Ưu tiên đơn vị khai trong cấu hình (đơn vị cấp trên chứa toàn bộ nhân sự cần lấy);
            // chưa khai thì lấy tạm đơn vị của chính chủ tài khoản.
            var wpId = AppSettings.GoConnect.WorkplaceId;
            if (string.IsNullOrWhiteSpace(wpId)) wpId = tokens.OwnWorkplaceId;

            if (string.IsNullOrWhiteSpace(wpId))
                throw new InvalidOperationException(
                    "Chưa biết lấy nhân sự của đơn vị nào. Đặt GoConnect:WorkplaceId trong Web.config.");

            return SyncWorkplace(wpId, tokens.AccessToken, tokens.RefreshToken, progress);
        }

        /// <summary>
        /// Lưu token của phiên đăng nhập vừa xong vào CSDL, để bản chạy thật dùng lại. Gọi ngay
        /// sau khi /hrm đăng nhập thành công. Trả về true nếu lưu được.
        /// </summary>
        public static bool SaveSessionTokenToDatabase()
        {
            var tokens = GoConnectSession.Read();
            if (tokens == null || !tokens.HasAccessToken) return false;

            GoConnectTokenStore.Save(tokens);
            return true;
        }

        /// <summary>
        /// Đồng bộ toàn bộ nhân sự của một đơn vị. Ném <see cref="GoConnectAuthException"/> khi
        /// token hết hạn — người gọi bắt lấy để chạy lại phiên đăng nhập.
        /// <paramref name="progress"/> nhận thông báo tiến độ của cả hai chặng (có thể null).
        /// </summary>
        public static GoConnectSyncResult SyncWorkplace(
            string wpId, string accessToken, string refreshToken, Action<string> progress = null)
        {
            var api = new GoConnectService(accessToken, refreshToken);
            var users = api.GetAllUsers(wpId, progress);

            return SaveUsers(users, progress);
        }

        /// <summary>
        /// Ghi một danh sách trả về từ GoConnect xuống CSDL. Tách rời phần gọi API để dùng lại
        /// được khi dữ liệu đến từ nguồn khác (ví dụ đọc từ tệp đã tải sẵn lúc gỡ lỗi).
        ///
        /// Ghi đơn vị và chức danh TRƯỚC, nhân sự sau — để lúc đọc kèm tên bằng phép nối bảng
        /// thì đã có sẵn tên mà nối.
        /// </summary>
        public static GoConnectSyncResult SaveUsers(List<UserInfo> users, Action<string> progress = null)
        {
            var result = new GoConnectSyncResult();
            if (users == null || users.Count == 0) return result;

            result.Fetched = users.Count;

            var workplaces = new List<HrWorkplace>();
            var positions = new List<HrPosition>();
            var employees = new List<HrEmployee>();

            foreach (var user in users)
            {
                if (user == null) continue;

                CollectWorkplaces(user, workplaces);
                CollectPosition(user, positions);
                employees.Add(ToEmployee(user));
            }

            result.Workplaces = HrWorkplaceStore.SaveMany(workplaces, progress);
            result.Positions = HrPositionStore.SaveMany(positions, progress);

            int skipped;
            result.Employees = HrEmployeeStore.SaveMany(employees, out skipped, progress);
            result.Skipped = skipped + (users.Count - employees.Count);

            return result;
        }

        /// <summary>Gom mọi đơn vị trên đường đi của một người vào danh sách chung (trùng lặp
        /// được gộp lại ở tầng ghi).</summary>
        private static void CollectWorkplaces(UserInfo user, List<HrWorkplace> into)
        {
            if (user.Workplaces == null) return;

            foreach (var workplace in user.Workplaces)
            {
                if (workplace == null || string.IsNullOrWhiteSpace(workplace.WpId)) continue;

                int level;
                into.Add(new HrWorkplace
                {
                    WpId = workplace.WpId,
                    WpCode = workplace.WpCode,
                    WpName = workplace.WpName,
                    WpParent = workplace.WpParent,
                    WpOrder = workplace.WpOrder,
                    Level = int.TryParse(workplace.Level, out level) ? level : (int?)null
                });
            }
        }

        private static void CollectPosition(UserInfo user, List<HrPosition> into)
        {
            if (string.IsNullOrWhiteSpace(user.PosId)) return;

            into.Add(new HrPosition
            {
                PosId = user.PosId,
                PosCode = user.PosCode,
                PosName = user.PosName,
                PosOrder = user.PosOrder
            });
        }

        /// <summary>Chuyển một dòng của GoConnect sang bản ghi nhân sự nội bộ.</summary>
        private static HrEmployee ToEmployee(UserInfo user)
        {
            return new HrEmployee
            {
                Email = user.UserEmail,
                Code = user.UserCode,
                FullName = FullNameOf(user),
                PhoneNumber = user.UserPhonenumber,
                Gender = user.UserGender,
                WorkplaceId = DeepestWorkplaceId(user),
                PositionId = user.PosId,
                UserId = user.UserId,

                // GoConnect chưa trả trường tài khoản tương ứng ở API này (userIdentifiers luôn
                // rỗng trên dữ liệu thật); để trống thay vì đoán.
                AccountId = null
            };
        }

        /// <summary>
        /// Ưu tiên họ tên đầy đủ do GoConnect trả sẵn. Thiếu thì ghép theo thứ tự tiếng Việt
        /// (họ — đệm — tên) từ ba trường rời.
        /// </summary>
        private static string FullNameOf(UserInfo user)
        {
            if (!string.IsNullOrWhiteSpace(user.UserFullname)) return user.UserFullname.Trim();

            var name = new StringBuilder();
            foreach (var part in new[] { user.UserLastname, user.UserMiddlename, user.UserFirstname })
            {
                if (string.IsNullOrWhiteSpace(part)) continue;
                if (name.Length > 0) name.Append(' ');
                name.Append(part.Trim());
            }

            return name.Length == 0 ? null : name.ToString();
        }

        /// <summary>
        /// Đơn vị công tác của một người là nhánh sâu nhất trong cây đơn vị của họ — đó mới là
        /// phòng/tổ cụ thể, thứ cần khi có người báo sai đơn vị. GoConnect đánh Level NGƯỢC với
        /// trực giác: 0 là cấp thấp nhất, số càng lớn càng lên cao (đã kiểm trên dữ liệu thật:
        /// 0 = "239.307.603", 2 = "239" VNPT Khánh Hòa, 4 = "VNPT" tập đoàn). Vậy phải lấy Level
        /// NHỎ nhất. Số cấp không cố định — dữ liệu thật có người 4 cấp, người 5, người 6 — nên
        /// không thể lấy phần tử cuối mảng.
        ///
        /// Không đọc được cây thì lùi về đơn vị ghi ngay trên dòng dữ liệu.
        /// </summary>
        private static string DeepestWorkplaceId(UserInfo user)
        {
            if (user.Workplaces == null || user.Workplaces.Count == 0) return user.WpId;

            string deepest = null;
            var deepestLevel = int.MaxValue;

            foreach (var workplace in user.Workplaces)
            {
                if (workplace == null || string.IsNullOrWhiteSpace(workplace.WpId)) continue;

                int level;
                if (!int.TryParse(workplace.Level, out level)) continue;

                if (level > deepestLevel) continue;
                deepestLevel = level;
                deepest = workplace.WpId;
            }

            return deepest ?? user.WpId;
        }
    }
}
