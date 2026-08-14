import 'package:flutter/foundation.dart';

/// Danh sach endpoint API, tro toi cac controller trong TTKDGP.ProjectManager/Controllers/Api/.
/// Backend nay dung ASP.NET MVC routing quy uoc `{controller}/{action}` (khong co tien to
/// "/api"), nen path o day khop ten controller/action, VI DU AuthApiController.Login ->
/// "/AuthApi/Login" — xem App_Start/RouteConfig.cs (route Default) va khong co [Route] rieng
/// tren cac controller nay.
class ApiEndpoint {
  ApiEndpoint._();

  // Site IIS local "pm.vn" (127.0.0.1, xem hosts) tro thang vao thu muc source
  // TTKDGP.ProjectManager — build xong (msbuild) la co ngay tren site nay, dung de dev/test tren
  // may nay. Doi sang https://pmncpt.cenit.vn (server that) khi build ban phat hanh.
  //
  // Emulator Android khong doc duoc hosts file cua may Windows host nen khong phan giai duoc
  // "pm.vn" — dung dia chi dac biet "10.0.2.2" (alias cua host tu ben trong AVD) qua binding
  // rieng tren port 8080 (khong doi Host header) thay vi ten mien.
  static String get baseUrl =>
      _isAndroid ? 'http://10.0.2.2:8080' : 'http://pm.vn';

  static bool get _isAndroid =>
      !kIsWeb && defaultTargetPlatform == TargetPlatform.android;

  static const login = '/AuthApi/Login';
  static const dashboard = '/DashboardApi/Index';
  static const myWork = '/MyWorkApi/Index';
  static const myProjects = '/MyProjectsApi/Index';
  static const checklist = '/ChecklistApi/Index';
  static const checklistCreate = '/ChecklistApi/Create';

  // Cac endpoint duoi day CHUA co controller tuong ung ben backend — giu lam danh sach du kien,
  // dung toi dau them controller toi do (xem Checklist-Thiet-ke-Giao-dien-Mobile.md, muc A).
  static const refreshToken = '/auth/refresh';
  static const me = '/auth/me';
  static const notifications = '/notifications';
  static const myReports = '/reports/mine';
  static const leaves = '/leaves/mine';
  static const kpiMine = '/kpi/mine';

  static String projectDetail(String id) => '/MyProjectsApi/Detail/$id';
  static String taskDetail(String id) => '/checklist/$id';

  // Nhom "Quan ly To" (can quyen wteam.manage)
  static const teamDashboard = '/team/dashboard';
  static const teamProjects = '/team/projects';
  static const privateTasks = '/team/private-tasks';
  static const leaveApprovals = '/team/leaves';
  static const teamReports = '/team/reports';
  static const kpiApprovals = '/team/kpi';
}
