import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/classes/route_manager.dart';
import '../../features/app_routes.dart';
import '../../features/auth/auth_provider.dart';

class _NavTab {
  const _NavTab({required this.routeName, required this.icon, required this.label});

  final String routeName;
  final IconData icon;
  final String label;
}

const _baseTabs = [
  _NavTab(routeName: AppRoutes.dashboard, icon: Icons.dashboard_outlined, label: 'Tong quan'),
  _NavTab(routeName: AppRoutes.myWork, icon: Icons.checklist_outlined, label: 'Viec cua toi'),
  _NavTab(routeName: AppRoutes.projects, icon: Icons.folder_outlined, label: 'Du an'),
  _NavTab(routeName: AppRoutes.notifications, icon: Icons.notifications_outlined, label: 'Thong bao'),
  _NavTab(routeName: AppRoutes.profile, icon: Icons.person_outline, label: 'Ca nhan'),
];

const _teamTab = _NavTab(
  routeName: AppRoutes.teamDashboard,
  icon: Icons.groups_outlined,
  label: 'To',
);

/// Khung Scaffold + bottom nav dung chung cho 6 man cap-tab (Dashboard/MyWork/Projects/
/// Notifications/Profile/TeamDashboard). Moi man tu bao boc widget nay va tu bao currentIndex
/// cua minh — thay the ShellRoute cua go_router bang mot Scaffold rieng cho tung tab (dung tinh
/// than "route phang, khong nested navigator" cua CLAUDE.md). Bam tab khac se pushReplacement
/// (Nav.to) sang route do, thay vi giu chung mot Navigator con.
class AppBottomNav extends StatelessWidget {
  const AppBottomNav({
    super.key,
    required this.currentIndex,
    required this.body,
    this.appBar,
    this.floatingActionButton,
  });

  final int currentIndex;
  final Widget body;
  final PreferredSizeWidget? appBar;
  final Widget? floatingActionButton;

  @override
  Widget build(BuildContext context) {
    final isTeamManager = context.watch<AuthProvider>().isTeamManager;
    final tabs = [..._baseTabs, if (isTeamManager) _teamTab];
    // Tab "To" chi co khi isTeamManager — neu man TeamDashboard lo duoc mo qua deep link boi
    // nguoi khong co quyen, currentIndex (5) se vuot qua do dai tabs; ve index 0 thay vi crash.
    final selectedIndex = currentIndex < tabs.length ? currentIndex : 0;

    return Scaffold(
      appBar: appBar,
      body: body,
      floatingActionButton: floatingActionButton,
      bottomNavigationBar: NavigationBar(
        selectedIndex: selectedIndex,
        onDestinationSelected: (index) {
          if (index == selectedIndex) return;
          Nav.to(context, tabs[index].routeName);
        },
        destinations: [
          for (final tab in tabs) NavigationDestination(icon: Icon(tab.icon), label: tab.label),
        ],
      ),
    );
  }
}
