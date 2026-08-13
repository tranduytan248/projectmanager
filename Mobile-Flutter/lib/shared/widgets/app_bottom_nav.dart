import 'package:flutter/material.dart';

import '../../core/classes/route_manager.dart';
import '../../features/app_routes.dart';

class _NavTab {
  const _NavTab({
    required this.routeName,
    required this.icon,
    required this.selectedIcon,
    required this.label,
  });

  final String routeName;
  final IconData icon;
  final IconData selectedIcon;
  final String label;
}

// 4 tab duy nhat: Dashboard, Du an, Cong viec, Cai dat. Khong con tab "To" (Quan ly To) —
// TeamDashboard van con route, chi khong gan vao thanh dieu huong duoi nua. "Thong bao" cung
// khong o day, mo qua chuong thong bao tren man Home (xem dashboard_screen.dart).
//
// Moi tab co hai icon: net (chua chon) va dac (dang chon) — dung NavigationDestination.
// selectedIcon, giong cach iOS/Material lam de nhin ro tab nao dang mo thay vi chi doi mau.
const _tabs = [
  _NavTab(
    routeName: AppRoutes.dashboard,
    icon: Icons.dashboard_outlined,
    selectedIcon: Icons.dashboard_rounded,
    label: 'Dashboard',
  ),
  _NavTab(
    routeName: AppRoutes.projects,
    icon: Icons.folder_outlined,
    selectedIcon: Icons.folder_rounded,
    label: 'Du an',
  ),
  _NavTab(
    routeName: AppRoutes.myWork,
    icon: Icons.checklist_outlined,
    selectedIcon: Icons.checklist_rounded,
    label: 'Cong viec',
  ),
  _NavTab(
    routeName: AppRoutes.profile,
    icon: Icons.settings_outlined,
    selectedIcon: Icons.settings_rounded,
    label: 'Cai dat',
  ),
];

/// Khung Scaffold + bottom nav dung chung cho 4 man cap-tab (Dashboard/Projects/MyWork/Profile).
/// Moi man tu bao boc widget nay va tu bao currentIndex cua minh — thay the ShellRoute cua
/// go_router bang mot Scaffold rieng cho tung tab (dung tinh than "route phang, khong nested
/// navigator" cua CLAUDE.md). Bam tab khac se pushReplacement (Nav.to) sang route do, thay vi
/// giu chung mot Navigator con.
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
    // Man nao lo duoc mo ma khong con nam trong 4 tab (vi du TeamDashboard qua deep link) thi
    // khong to sang dong nao ca, thay vi vuot qua do dai danh sach roi crash.
    final selectedIndex = currentIndex < _tabs.length ? currentIndex : null;

    return Scaffold(
      appBar: appBar,
      body: body,
      floatingActionButton: floatingActionButton,
      bottomNavigationBar: NavigationBar(
        selectedIndex: selectedIndex ?? 0,
        onDestinationSelected: (index) {
          if (index == selectedIndex) return;
          Nav.to(context, _tabs[index].routeName);
        },
        destinations: [
          for (final tab in _tabs)
            NavigationDestination(
              icon: Icon(tab.icon),
              selectedIcon: Icon(tab.selectedIcon),
              label: tab.label,
            ),
        ],
      ),
    );
  }
}
