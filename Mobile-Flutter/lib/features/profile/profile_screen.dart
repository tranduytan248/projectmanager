import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:phosphor_icons/phosphor_icons.dart';
import 'package:provider/provider.dart';

import '../../config/app_theme.dart';
import '../../core/classes/cache_manager.dart';
import '../../core/classes/route_manager.dart';
import '../../core/services/fcm_notification_service.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/dialog_service.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_card.dart';
import '../../core/widgets/app_text.dart';
import '../../shared/widgets/app_bottom_nav.dart';
import '../app_routes.dart';
import '../auth/auth_provider.dart';
import 'personal_info_screen.dart';
import 'policy_screen.dart';
import 'version_history_screen.dart';

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  Future<void> _confirmLogout(BuildContext context) async {
    final ok = await DialogService.showConfirm(
      'Bạn có chắc muốn đăng xuất khỏi ứng dụng?',
      title: 'Đăng xuất',
    );
    if (ok && context.mounted) {
      await context.read<AuthProvider>().logout(context);
    }
  }

  void _showFcmTokenDialog(BuildContext context) {
    final token = FcmNotificationService.instance.fcmToken ??
        Cache.readData<String>('fcm_device_token') ??
        'Đang lấy mã thiết bị hoặc chưa được cấp quyền...';

    showDialog<void>(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: AppColors.surface,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Row(
          children: [
            Icon(PhosphorIconsRegular.bellRinging, color: AppColors.primary),
            SizedBox(width: AppDimens.space8),
            Expanded(
              child: AppText(
                'FCM Device Token',
                variant: AppTextVariant.heading,
                weight: FontWeight.bold,
              ),
            ),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const AppText(
              'Mã định danh thiết bị nhận thông báo đẩy Firebase Cloud Messaging:',
              variant: AppTextVariant.caption,
              color: AppColors.textSecondary,
            ),
            const SizedBox(height: AppDimens.space12),
            Container(
              padding: const EdgeInsets.all(AppDimens.space12),
              decoration: BoxDecoration(
                color: AppColors.background,
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: AppColors.border),
              ),
              child: SelectableText(
                token,
                style: const TextStyle(
                  fontFamily: 'monospace',
                  fontSize: 11,
                  color: AppColors.textPrimary,
                ),
              ),
            ),
          ],
        ),
        actions: [
          AppButton(
            label: 'Sao chép mã',
            icon: PhosphorIconsRegular.copy,
            onPressed: () {
              Clipboard.setData(ClipboardData(text: token));
              Navigator.of(ctx).pop();
              ToastService.show('Đã sao chép FCM Token vào bộ nhớ tạm!',
                  type: ToastType.success);
            },
          ),
          const SizedBox(height: AppDimens.space8),
          AppButton(
            label: 'Đóng',
            type: AppButtonType.secondary,
            onPressed: () => Navigator.of(ctx).pop(),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final initial = (auth.displayName?.trim().isNotEmpty == true
            ? auth.displayName!.trim()[0]
            : '?')
        .toUpperCase();

    return AppBottomNav(
      currentIndex: 3,
      appBar: const AppAppBar(title: 'Cài đặt'),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            Row(
              children: [
                CircleAvatar(
                  radius: 28,
                  backgroundColor: AppTheme.brandBlueSoft,
                  child: AppText(initial,
                      variant: AppTextVariant.title,
                      fontSize: 22,
                      weight: FontWeight.w800,
                      color: AppTheme.brandBlue),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      AppText(auth.displayName ?? '—',
                          variant: AppTextVariant.body,
                          fontSize: 17,
                          weight: FontWeight.w700),
                      const SizedBox(height: 2),
                      AppText(auth.isTeamManager ? 'Quản lý Tổ' : 'Nhân viên',
                          variant: AppTextVariant.caption,
                          fontSize: 12.5,
                          color: AppColors.textSecondary),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 24),
            _SettingsGroup(
              title: 'Cá nhân',
              children: [
                _SettingsTile(
                  icon: PhosphorIconsRegular.userCircle,
                  label: 'Thông tin cá nhân',
                  onTap: () =>
                      Navigator.of(context).push(MaterialPageRoute<void>(
                    builder: (context) => const PersonalInfoScreen(),
                  )),
                ),
                _SettingsTile(
                  icon: PhosphorIconsRegular.calendarCheck,
                  label: 'Đăng ký nghỉ phép',
                  onTap: () => Nav.toNamed(context, AppRoutes.leaves),
                ),
              ],
            ),
            // Nhom "Quan ly" chi danh cho Quan ly To — an han ca nhom (khong xam mo) voi nhan
            // vien thuong, cac route ben duoi da co san nhung truoc day mo coi (khong gan noi nao
            // trong UI).
            if (auth.isTeamManager) ...[
              const SizedBox(height: AppDimens.space24),
              _SettingsGroup(
                title: 'Quản lý',
                children: [
                  _SettingsTile(
                    icon: PhosphorIconsRegular.folders,
                    label: 'Danh sách dự án',
                    onTap: () => Nav.toNamed(context, AppRoutes.projects,
                        arguments: const {'scope': 'team'}),
                  ),
                  _SettingsTile(
                    icon: PhosphorIconsRegular.checks,
                    label: 'Duyệt nghỉ phép',
                    onTap: () =>
                        Nav.toNamed(context, AppRoutes.teamLeaveApprovals),
                  ),
                  _SettingsTile(
                    icon: PhosphorIconsRegular.userPlus,
                    label: 'Giao việc riêng',
                    onTap: () =>
                        Nav.toNamed(context, AppRoutes.teamPrivateTasks),
                  ),
                  _SettingsTile(
                    icon: PhosphorIconsRegular.gauge,
                    label: 'Bảng điều khiển tổ',
                    onTap: () => Nav.toNamed(context, AppRoutes.teamDashboard),
                  ),
                  _SettingsTile(
                    icon: PhosphorIconsRegular.star,
                    label: 'KPI theo tháng',
                    onTap: () => Nav.toNamed(context, AppRoutes.kpi),
                  ),
                ],
              ),
            ],
            const SizedBox(height: AppDimens.space24),
            _SettingsGroup(
              title: 'Hệ thống',
              children: [
                _SettingsTile(
                  icon: PhosphorIconsRegular.shieldCheck,
                  label: 'Chính sách bảo mật',
                  onTap: () =>
                      Navigator.of(context).push(MaterialPageRoute<void>(
                    builder: (context) => const PolicyScreen(
                      title: 'Chính sách bảo mật',
                      paragraphs: privacyPolicyParagraphs,
                    ),
                  )),
                ),
                _SettingsTile(
                  icon: PhosphorIconsRegular.fileText,
                  label: 'Điều khoản sử dụng',
                  onTap: () =>
                      Navigator.of(context).push(MaterialPageRoute<void>(
                    builder: (context) => const PolicyScreen(
                      title: 'Điều khoản sử dụng',
                      paragraphs: termsOfUseParagraphs,
                    ),
                  )),
                ),
                _SettingsTile(
                  icon: PhosphorIconsRegular.bellRinging,
                  label: 'Mã thiết bị nhận thông báo (FCM Token)',
                  onTap: () => _showFcmTokenDialog(context),
                ),
                _SettingsTile(
                  icon: PhosphorIconsRegular.clockCounterClockwise,
                  label: 'Các phiên bản cập nhật',
                  onTap: () =>
                      Navigator.of(context).push(MaterialPageRoute<void>(
                    builder: (context) => const VersionHistoryScreen(),
                  )),
                ),
              ],
            ),
            const SizedBox(height: 20),
            _SettingsGroup(
              children: [
                _SettingsTile(
                  icon: PhosphorIconsRegular.signOut,
                  label: 'Thoát',
                  danger: true,
                  onTap: () => _confirmLogout(context),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

/// Khoi cac muc cai dat dang danh sach (khong phai luoi icon) — hop voi noi dung co do dai
/// nhan khac nhau va mot muc nguy hiem (Thoat) can tach rieng khoi nhom con lai.
///
/// [title] la nhan nhom hien phia tren the (vi du "Cá nhân", "Quản lý", "Hệ thống") — de trong
/// (null) cho nhom khong can tieu de (vi du nhom "Thoat" chi co 1 muc, tu no da ro nghia).
class _SettingsGroup extends StatelessWidget {
  const _SettingsGroup({this.title, required this.children});

  final String? title;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    final card = AppCard(
      padding: EdgeInsets.zero,
      child: Column(
        children: [
          for (var i = 0; i < children.length; i++) ...[
            children[i],
            if (i < children.length - 1) const Divider(height: 1, indent: 56),
          ],
        ],
      ),
    );

    if (title == null) return card;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(
              left: AppDimens.space4, bottom: AppDimens.space8),
          child: AppText(title!.toUpperCase(),
              variant: AppTextVariant.overline, color: AppColors.textSecondary),
        ),
        card,
      ],
    );
  }
}

class _SettingsTile extends StatelessWidget {
  const _SettingsTile({
    required this.icon,
    required this.label,
    required this.onTap,
    this.danger = false,
  });

  final IconData icon;
  final String label;
  final VoidCallback onTap;
  final bool danger;

  @override
  Widget build(BuildContext context) {
    final color = danger ? AppTheme.statusDanger : AppColors.textPrimary;

    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        child: Row(
          children: [
            PhosphorIcon(icon,
                size: 20,
                color: danger ? AppTheme.statusDanger : AppTheme.brandBlue),
            const SizedBox(width: 16),
            Expanded(
              child: AppText(label,
                  variant: AppTextVariant.body,
                  fontSize: 14,
                  weight: FontWeight.w600,
                  color: color),
            ),
            if (!danger)
              const PhosphorIcon(PhosphorIconsRegular.caretRight,
                  size: 16, color: AppColors.textFaint),
          ],
        ),
      ),
    );
  }
}
