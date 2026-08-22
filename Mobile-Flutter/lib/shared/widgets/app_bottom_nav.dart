import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../config/app_routes.dart' show Routes;
import '../../config/app_theme.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_text.dart';
import '../../features/app_routes.dart';

class _NavTab {
  const _NavTab({
    required this.routeName,
    required this.icon,
    required this.filledIcon,
    required this.label,
  });

  final String routeName;

  /// Net mong khi chua chon, dac (fill) khi dang chon — doc ro tab nao dang mo thay vi chi
  /// doi mau, giong cach iOS/cac app cao cap lam.
  final IconData icon;
  final IconData filledIcon;
  final String label;
}

// 4 tab duy nhat: Dashboard, Du an, Cong viec, Cai dat. Khong con tab "To" (Quan ly To) —
// TeamDashboard van con route, chi khong gan vao thanh dieu huong duoi nua. "Thong bao" cung
// khong o day, mo qua chuong thong bao tren man Home (xem dashboard_screen.dart).
const _tabs = [
  _NavTab(
    routeName: AppRoutes.dashboard,
    icon: PhosphorIconsRegular.squaresFour,
    filledIcon: PhosphorIconsFill.squaresFour,
    label: 'Dashboard',
  ),
  _NavTab(
    routeName: AppRoutes.projects,
    icon: PhosphorIconsRegular.folders,
    filledIcon: PhosphorIconsFill.folders,
    label: 'Dự án',
  ),
  _NavTab(
    routeName: AppRoutes.myWork,
    icon: PhosphorIconsRegular.listChecks,
    filledIcon: PhosphorIconsFill.listChecks,
    label: 'Công việc',
  ),
  _NavTab(
    routeName: AppRoutes.profile,
    icon: PhosphorIconsRegular.gearSix,
    filledIcon: PhosphorIconsFill.gearSix,
    label: 'Cài đặt',
  ),
];

/// Khung Scaffold + bottom nav dung chung cho 4 man cap-tab (Dashboard/Projects/MyWork/Profile).
/// Moi man tu bao boc widget nay va tu bao currentIndex cua minh — thay the ShellRoute cua
/// go_router bang mot Scaffold rieng cho tung tab (dung tinh than "route phang, khong nested
/// navigator" cua CLAUDE.md). Bam tab khac se pushReplacement (Nav.to) sang route do, thay vi
/// giu chung mot Navigator con.
///
/// Thanh dieu huong tu ve rieng (khong dung Material NavigationBar mac dinh) de duoc dung kieu
/// "flat": nen trang phang, vien tren mong thay vi do bong day, muc dang chon la mot khoi bo
/// tron mau nhat cua thuong hieu quanh icon+nhan thay vi cham indicator o giua.
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

    // Thanh trang thai trong suot + icon mau toi: nen trang cua man noi lien vao vung status
    // bar thay vi hien mot dai mau toi mac dinh cua he thong, cho cam giac "fullscreen" giong
    // nhu man dang nhap (xem login_screen.dart) nhung doi mau icon vi nen o day la mau trang.
    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: SystemUiOverlayStyle.dark.copyWith(
        statusBarColor: Colors.transparent,
      ),
      child: Scaffold(
        appBar: appBar,
        body: body,
        floatingActionButton: floatingActionButton,
        bottomNavigationBar: _FlatBottomBar(
          selectedIndex: selectedIndex ?? -1,
          onSelect: (index) {
            if (index == selectedIndex) return;
            _switchTab(context, _tabs[index].routeName);
          },
        ),
      ),
    );
  }

  /// Doi tab bang PageRouteBuilder thoi gian chuyen canh = 0 thay vi Nav.to (dung
  /// pushReplacementNamed, ke thua hieu ung truot/mo mac dinh cua MaterialPageRoute). Chuyen tab
  /// duoi phai la MOT KHOI thay man hinh tuc thi, khong phai "day man moi" nhu dieu huong sau
  /// (ví dụ mo chi tiet) — hai dieu do khac nghia nen khong dung chung Nav.to.
  static void _switchTab(BuildContext context, String routeName) {
    final builder = Routes().routes[routeName];
    if (builder == null) return;

    Navigator.of(context).pushReplacement<void, void>(
      PageRouteBuilder<void>(
        settings: RouteSettings(name: routeName),
        transitionDuration: Duration.zero,
        reverseTransitionDuration: Duration.zero,
        pageBuilder: (context, animation, secondaryAnimation) => builder(context),
      ),
    );
  }
}

class _FlatBottomBar extends StatelessWidget {
  const _FlatBottomBar({required this.selectedIndex, required this.onSelect});

  final int selectedIndex;
  final ValueChanged<int> onSelect;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        color: Color(0xFF181818),
        border: Border(top: BorderSide(color: AppColors.border)),
      ),
      child: SafeArea(
        top: false,
        child: SizedBox(
          height: 62,
          child: Row(
            children: [
              for (var i = 0; i < _tabs.length; i++)
                Expanded(
                  child: _NavItem(
                    tab: _tabs[i],
                    selected: i == selectedIndex,
                    onTap: () => onSelect(i),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _NavItem extends StatelessWidget {
  const _NavItem({required this.tab, required this.selected, required this.onTap});

  final _NavTab tab;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final color = selected ? const Color(0xFF3794FF) : AppColors.textSecondary;

    return InkWell(
      onTap: onTap,
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          AnimatedContainer(
            duration: const Duration(milliseconds: 160),
            padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 5),
            decoration: BoxDecoration(
              color: selected ? AppTheme.brandBlueSoft : Colors.transparent,
              borderRadius: BorderRadius.circular(14),
            ),
            child: PhosphorIcon(
              selected ? tab.filledIcon : tab.icon,
              size: 22,
              color: color,
            ),
          ),
          const SizedBox(height: 4),
          AppText(
            tab.label,
            variant: AppTextVariant.caption,
            fontSize: 10.5,
            weight: selected ? FontWeight.w700 : FontWeight.w500,
            color: color,
          ),
        ],
      ),
    );
  }
}
