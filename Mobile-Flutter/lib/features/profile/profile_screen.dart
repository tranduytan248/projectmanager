import 'package:flutter/material.dart';
import 'package:phosphor_icons/phosphor_icons.dart';
import 'package:provider/provider.dart';

import '../../config/app_theme.dart';
import '../../core/classes/route_manager.dart';
import '../../core/utils/dialog_service.dart';
import '../../shared/widgets/app_bottom_nav.dart';
import '../app_routes.dart';
import '../auth/auth_provider.dart';
import 'policy_screen.dart';

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

  void _showPersonalInfo(BuildContext context, AuthProvider auth) {
    showModalBottomSheet<void>(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (context) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(20, 20, 20, 28),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('Thông tin cá nhân',
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700)),
              const SizedBox(height: 16),
              _InfoRow(label: 'Họ tên', value: auth.displayName ?? '—'),
              const SizedBox(height: 10),
              _InfoRow(
                  label: 'Vai trò',
                  value: auth.isTeamManager ? 'Quản lý Tổ' : 'Nhân viên'),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final initial =
        (auth.displayName?.trim().isNotEmpty == true ? auth.displayName!.trim()[0] : '?')
            .toUpperCase();

    return AppBottomNav(
      currentIndex: 3,
      appBar: AppBar(title: const Text('Cài đặt')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            Row(
              children: [
                CircleAvatar(
                  radius: 28,
                  backgroundColor: AppTheme.brandBlueSoft,
                  child: Text(initial,
                      style: const TextStyle(
                          fontSize: 22,
                          fontWeight: FontWeight.w800,
                          color: AppTheme.brandBlue)),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(auth.displayName ?? '',
                          style: const TextStyle(
                              fontSize: 17, fontWeight: FontWeight.w700, color: Colors.black87)),
                      const SizedBox(height: 2),
                      Text(auth.isTeamManager ? 'Quản lý Tổ' : 'Nhân viên',
                          style: const TextStyle(fontSize: 12.5, color: Colors.black45)),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 24),
            _SettingsGroup(
              children: [
                _SettingsTile(
                  icon: PhosphorIconsRegular.userCircle,
                  label: 'Thông tin cá nhân',
                  onTap: () => _showPersonalInfo(context, auth),
                ),
                _SettingsTile(
                  icon: PhosphorIconsRegular.shieldCheck,
                  label: 'Chính sách bảo mật',
                  onTap: () => Navigator.of(context).push(MaterialPageRoute<void>(
                    builder: (context) => const PolicyScreen(
                      title: 'Chính sách bảo mật',
                      paragraphs: privacyPolicyParagraphs,
                    ),
                  )),
                ),
                _SettingsTile(
                  icon: PhosphorIconsRegular.fileText,
                  label: 'Điều khoản sử dụng',
                  onTap: () => Navigator.of(context).push(MaterialPageRoute<void>(
                    builder: (context) => const PolicyScreen(
                      title: 'Điều khoản sử dụng',
                      paragraphs: termsOfUseParagraphs,
                    ),
                  )),
                ),
                _SettingsTile(
                  icon: PhosphorIconsRegular.calendarCheck,
                  label: 'Đăng ký nghỉ phép',
                  onTap: () => Nav.toNamed(context, AppRoutes.leaves),
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

class _InfoRow extends StatelessWidget {
  const _InfoRow({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        SizedBox(
          width: 80,
          child: Text(label, style: const TextStyle(fontSize: 13, color: Colors.black45)),
        ),
        Expanded(
          child: Text(value,
              style: const TextStyle(
                  fontSize: 14, fontWeight: FontWeight.w600, color: Colors.black87)),
        ),
      ],
    );
  }
}

/// Khoi cac muc cai dat dang danh sach (khong phai luoi icon) — hop voi noi dung co do dai
/// nhan khac nhau va mot muc nguy hiem (Thoat) can tach rieng khoi nhom con lai.
class _SettingsGroup extends StatelessWidget {
  const _SettingsGroup({required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: Colors.black.withValues(alpha: 0.08)),
      ),
      child: Column(
        children: [
          for (var i = 0; i < children.length; i++) ...[
            children[i],
            if (i < children.length - 1)
              const Divider(height: 1, indent: 56),
          ],
        ],
      ),
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
    final color = danger ? AppTheme.statusDanger : Colors.black87;

    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        child: Row(
          children: [
            PhosphorIcon(icon, size: 20, color: danger ? AppTheme.statusDanger : AppTheme.brandBlue),
            const SizedBox(width: 16),
            Expanded(
              child: Text(label,
                  style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: color)),
            ),
            if (!danger)
              const PhosphorIcon(PhosphorIconsRegular.caretRight, size: 16, color: Colors.black26),
          ],
        ),
      ),
    );
  }
}
