/// Danh sach endpoint API du kien, map 1-1 voi cac controller MVC hien co
/// (xem TTKDGP.ProjectManager/FileMoTa/Checklist-Thiet-ke-Giao-dien-Mobile.md, muc A).
///
/// Backend hien tai chi tra HTML/JsonResult noi bo qua FormsAuthentication cookie,
/// CHUA co REST API dung token — day la phan can dung truoc khi noi du lieu that.
class ApiEndpoints {
  ApiEndpoints._();

  // TODO: doi sang domain that cua backend khi da co API layer
  static const baseUrl = 'https://ttkdgp.example.local/api';

  static const login = '/auth/login';
  static const refreshToken = '/auth/refresh';
  static const me = '/auth/me';

  static const dashboard = '/dashboard';
  static const myWork = '/mywork';
  static const myProjects = '/projects/mine';
  static const notifications = '/notifications';
  static const myReports = '/reports/mine';
  static const leaves = '/leaves/mine';
  static const kpiMine = '/kpi/mine';

  static String projectDetail(String id) => '/projects/$id';
  static String taskDetail(String id) => '/checklist/$id';

  // Nhom "Quan ly To" (can quyen wteam.manage)
  static const teamDashboard = '/team/dashboard';
  static const teamProjects = '/team/projects';
  static const privateTasks = '/team/private-tasks';
  static const leaveApprovals = '/team/leaves';
  static const teamReports = '/team/reports';
  static const kpiApprovals = '/team/kpi';
}
