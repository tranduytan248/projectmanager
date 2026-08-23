
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
  static String get baseUrl => 'http://pmncpt.cenit.vn';

  static const login = '/AuthApi/Login';
  static const forgotPasswordRequestOtp = '/AuthApi/RequestPasswordResetOtp';
  static const forgotPasswordReset = '/AuthApi/ResetPasswordWithOtp';
  static const profileMe = '/AccountApi/Me';
  static const profileUpdate = '/AccountApi/UpdateProfile';
  static const profileChangePassword = '/AccountApi/ChangePassword';
  static const dashboard = '/DashboardApi/Index';
  static const myWork = '/MyWorkApi/Index';
  static const myProjects = '/MyProjectsApi/Index';
  static const myProjectsCreateForm = '/MyProjectsApi/CreateForm';
  static const myProjectsCreate = '/MyProjectsApi/Create';
  static const checklist = '/ChecklistApi/Index';
  static const checklistCreate = '/ChecklistApi/Create';
  static const notifications = '/NotificationsApi/Index';
  static const notificationMarkRead = '/NotificationsApi/MarkRead';
  static const notificationMarkAllRead = '/NotificationsApi/MarkAllRead';
  static const notificationRegisterDevice = '/NotificationsApi/RegisterDevice';
  static const discussionsUpload = '/DiscussionsApi/Upload';
  static const discussionsTasks = '/DiscussionsApi/Tasks';

  static const leaves = '/LeavesApi/Index';
  static const leaveCreate = '/LeavesApi/Create';
  static const leaveUpdate = '/LeavesApi/Update';
  static const leaveCancel = '/LeavesApi/Cancel';

  // Cac endpoint duoi day CHUA co controller tuong ung ben backend — giu lam danh sach du kien,
  // dung toi dau them controller toi do (xem Checklist-Thiet-ke-Giao-dien-Mobile.md, muc A).
  static const refreshToken = '/auth/refresh';
  static const myReports = '/reports/mine';
  static const kpiMine = '/kpi/mine';

  static String projectDetail(String id) => '/MyProjectsApi/Detail/$id';
  static String taskDetail(String id) => '/ChecklistApi/Detail/$id';
  static const taskLogTime = '/ChecklistApi/LogTime';
  static const taskDeleteTimeLog = '/ChecklistApi/DeleteTimeLog';
  static const taskAddTodo = '/ChecklistApi/AddTodo';
  static const taskToggleTodo = '/ChecklistApi/ToggleTodo';
  static const taskEditTodo = '/ChecklistApi/EditTodo';
  static const taskDeleteTodo = '/ChecklistApi/DeleteTodo';
  static const taskComment = '/ChecklistApi/Comment';
  static const taskRecallComment = '/ChecklistApi/RecallComment';
  static String taskAttachment(int commentId) =>
      '/ChecklistApi/Attachment?commentId=$commentId';
  static const taskUpdateStatus = '/ChecklistApi/UpdateStatus';
  static String taskActivityLog(String id) => '/ChecklistApi/ActivityLog/$id';
  static String taskAssigneeOptions(String id) =>
      '/ChecklistApi/AssigneeOptions/$id';
  static const taskUpdateAssignee = '/ChecklistApi/UpdateAssignee';

  static const projectMembers = '/ProjectMembersApi/Index';
  static const projectAddMemberForm = '/ProjectMembersApi/AddMemberForm';
  static const projectAddMember = '/ProjectMembersApi/AddMember';
  static const projectSetPm = '/ProjectMembersApi/SetPm';
  static const projectEndMember = '/ProjectMembersApi/EndMember';
  static const projectRemoveMember = '/ProjectMembersApi/RemoveMember';

  // Nhom "Quan ly To" (can quyen wteam.manage hoac wtasks.create)
  static const teamDashboard = '/TeamDashboardApi/Index';
  static const teamDashboardMemberTasks = '/TeamDashboardApi/MemberTasks';
  static const teamProjects = '/team/projects';
  static const privateTasks = '/PrivateTasksApi/Index';
  static const privateTasksFormOptions = '/PrivateTasksApi/FormOptions';
  static const privateTasksCreate = '/PrivateTasksApi/Create';
  static const privateTasksUpdate = '/PrivateTasksApi/Update';
  static const privateTasksDelete = '/PrivateTasksApi/Delete';
  static const leaveApprovals = '/LeavesApi/Approvals';
  static const leaveSetState = '/LeavesApi/SetState';
  static const teamReports = '/team/reports';
  static const kpiApprovals = '/team/kpi';

  // KPI theo tháng
  static const kpiIndex = '/KpiApi/Index';
  static const kpiDetail = '/KpiApi/Detail';
  static const kpiCalculate = '/KpiApi/Calculate';
  static const kpiRecalculate = '/KpiApi/Recalculate';
}

