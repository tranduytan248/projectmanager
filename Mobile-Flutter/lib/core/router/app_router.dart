import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/auth_controller.dart';
import '../../features/auth/login_screen.dart';
import '../../features/checklist/checklist_board_screen.dart';
import '../../features/dashboard/dashboard_screen.dart';
import '../../features/kpi/kpi_screen.dart';
import '../../features/leaves/leave_list_screen.dart';
import '../../features/leaves/leave_request_form_screen.dart';
import '../../features/mywork/my_work_screen.dart';
import '../../features/mywork/task_detail_screen.dart';
import '../../features/notifications/notifications_screen.dart';
import '../../features/profile/profile_screen.dart';
import '../../features/projects/my_projects_screen.dart';
import '../../features/projects/project_detail_screen.dart';
import '../../features/team/leave_approval_screen.dart';
import '../../features/team/private_tasks_screen.dart';
import '../../features/team/team_dashboard_screen.dart';
import '../../shared/widgets/app_scaffold.dart';

/// Ghi chu: GoRouter duoc tao lai moi khi authState doi vi ta doc no truc tiep
/// trong Provider<GoRouter>. Du dung duoc cho template, ban dau; khi len production
/// nen chuyen sang GoRouterRefreshStream de tranh mat navigation stack khi refresh.
final appRouterProvider = Provider<GoRouter>((ref) {
  final authState = ref.watch(authControllerProvider);

  return GoRouter(
    initialLocation: '/dashboard',
    redirect: (context, state) {
      final loggingIn = state.matchedLocation == '/login';
      if (!authState.isAuthenticated) {
        return loggingIn ? null : '/login';
      }
      if (loggingIn) return '/dashboard';
      return null;
    },
    routes: [
      GoRoute(path: '/login', builder: (context, state) => const LoginScreen()),
      ShellRoute(
        builder: (context, state, child) => AppScaffold(child: child),
        routes: [
          GoRoute(
            path: '/dashboard',
            builder: (context, state) => const DashboardScreen(),
          ),
          GoRoute(
            path: '/my-work',
            builder: (context, state) => const MyWorkScreen(),
            routes: [
              GoRoute(
                path: 'task/:taskId',
                builder: (context, state) => TaskDetailScreen(
                  taskId: state.pathParameters['taskId']!,
                ),
              ),
            ],
          ),
          GoRoute(
            path: '/projects',
            builder: (context, state) => const MyProjectsScreen(),
            routes: [
              GoRoute(
                path: ':projectId',
                builder: (context, state) => ProjectDetailScreen(
                  projectId: state.pathParameters['projectId']!,
                ),
              ),
              GoRoute(
                path: ':projectId/checklist',
                builder: (context, state) => ChecklistBoardScreen(
                  projectId: state.pathParameters['projectId']!,
                ),
              ),
            ],
          ),
          GoRoute(
            path: '/leaves',
            builder: (context, state) => const LeaveListScreen(),
            routes: [
              GoRoute(
                path: 'new',
                builder: (context, state) => const LeaveRequestFormScreen(),
              ),
            ],
          ),
          GoRoute(path: '/kpi', builder: (context, state) => const KpiScreen()),
          GoRoute(
            path: '/notifications',
            builder: (context, state) => const NotificationsScreen(),
          ),
          GoRoute(path: '/profile', builder: (context, state) => const ProfileScreen()),

          // Nhom "Quan ly To" — chi hien tab trong AppScaffold khi isTeamManager = true,
          // nhung route van khai bao o day de co the mo bang deep link/push notification.
          GoRoute(
            path: '/team/dashboard',
            builder: (context, state) => const TeamDashboardScreen(),
          ),
          GoRoute(
            path: '/team/private-tasks',
            builder: (context, state) => const PrivateTasksScreen(),
          ),
          GoRoute(
            path: '/team/leave-approvals',
            builder: (context, state) => const LeaveApprovalScreen(),
          ),
        ],
      ),
    ],
  );
});
