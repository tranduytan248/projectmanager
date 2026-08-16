import 'checklist/checklist_board_controller.dart';
import 'dashboard/dashboard_controller.dart';
import 'kpi/kpi_controller.dart';
import 'leaves/leave_list_controller.dart';
import 'leaves/leave_request_form_controller.dart';
import 'mywork/my_work_controller.dart';
import 'mywork/task_detail_controller.dart';
import 'notifications/notifications_controller.dart';
import 'profile/profile_controller.dart';
import 'projects/my_projects_controller.dart';
import 'projects/project_detail_controller.dart';
import 'projects/project_members_controller.dart';
import 'team/leave_approval_controller.dart';
import 'team/private_tasks_controller.dart';
import 'team/team_dashboard_controller.dart';
import '../core/classes/route_manager.dart';

/// Toan bo route "sau dang nhap" cua app. Ten route dat theo dang '<name>/<duong-dan>' —
/// dung hang so static de noi khac (AppBottomNav, cac nut dieu huong...) tham chieu, khong
/// bao gio ghi thang chuoi lieu.
class AppRoutes extends RouteManager {
  static const String name = 'App';

  static const String dashboard = '$name/dashboard';
  static const String myWork = '$name/my-work';
  static const String taskDetail = '$name/my-work/task';
  static const String projects = '$name/projects';
  static const String projectDetail = '$name/projects/detail';
  static const String checklist = '$name/projects/checklist';
  static const String projectMembers = '$name/projects/members';
  static const String leaves = '$name/leaves';
  static const String leaveNew = '$name/leaves/new';
  static const String kpi = '$name/kpi';
  static const String notifications = '$name/notifications';
  static const String profile = '$name/profile';
  static const String teamDashboard = '$name/team/dashboard';
  static const String teamPrivateTasks = '$name/team/private-tasks';
  static const String teamLeaveApprovals = '$name/team/leave-approvals';

  AppRoutes() {
    addRoute(dashboard, (context) => const DashboardController());
    addRoute(myWork, (context) => const MyWorkController());
    addRoute(taskDetail, (context) => const TaskDetailController());
    addRoute(projects, (context) => const MyProjectsController());
    addRoute(projectDetail, (context) => const ProjectDetailController());
    addRoute(checklist, (context) => const ChecklistBoardController());
    addRoute(projectMembers, (context) => const ProjectMembersController());
    addRoute(leaves, (context) => const LeaveListController());
    addRoute(leaveNew, (context) => const LeaveRequestFormController());
    addRoute(kpi, (context) => const KpiController());
    addRoute(notifications, (context) => const NotificationsController());
    addRoute(profile, (context) => const ProfileController());
    addRoute(teamDashboard, (context) => const TeamDashboardController());
    addRoute(teamPrivateTasks, (context) => const PrivateTasksController());
    addRoute(teamLeaveApprovals, (context) => const LeaveApprovalController());
  }
}
