import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/auth_controller.dart';

class _NavTab {
  const _NavTab({required this.path, required this.icon, required this.label});

  final String path;
  final IconData icon;
  final String label;
}

const _baseTabs = [
  _NavTab(path: '/dashboard', icon: Icons.dashboard_outlined, label: 'Tong quan'),
  _NavTab(path: '/my-work', icon: Icons.checklist_outlined, label: 'Viec cua toi'),
  _NavTab(path: '/projects', icon: Icons.folder_outlined, label: 'Du an'),
  _NavTab(path: '/notifications', icon: Icons.notifications_outlined, label: 'Thong bao'),
  _NavTab(path: '/profile', icon: Icons.person_outline, label: 'Ca nhan'),
];

/// Khung bottom navigation dung chung, an/hien tab "To" theo quyen wteam.manage
/// giong logic phan quyen RoleGroup o web.
class AppScaffold extends ConsumerWidget {
  const AppScaffold({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isTeamManager = ref.watch(authControllerProvider).isTeamManager;
    final tabs = [
      ..._baseTabs,
      if (isTeamManager) const _NavTab(path: '/team/dashboard', icon: Icons.groups_outlined, label: 'To'),
    ];

    final location = GoRouterState.of(context).matchedLocation;
    final matchedIndex = tabs.indexWhere((t) => location.startsWith(t.path));
    final currentIndex = matchedIndex < 0 ? 0 : matchedIndex;

    return Scaffold(
      body: child,
      bottomNavigationBar: NavigationBar(
        selectedIndex: currentIndex,
        onDestinationSelected: (index) => context.go(tabs[index].path),
        destinations: [
          for (final tab in tabs) NavigationDestination(icon: Icon(tab.icon), label: tab.label),
        ],
      ),
    );
  }
}
