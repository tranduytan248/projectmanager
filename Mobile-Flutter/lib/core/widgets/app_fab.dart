import 'package:flutter/material.dart';

import '../../config/app_theme.dart';
import '../theme/app_colors.dart';
import '../theme/app_text_styles.dart';
import 'app_text.dart';

/// Nut noi (Floating Action Button) — dung `.claude/rules/FLUTTER_RULES.md`, thuoc nhom "moi
/// widget cua thu vien UI ben thu ba" phai boc lai. Man hinh KHONG duoc dung
/// FloatingActionButton truc tiep, luon qua AppFab. Phang (elevation 0) dung tinh than flat
/// design — noi bat bang mau nen dam thay vi do bong.
class AppFab extends StatelessWidget {
  const AppFab({super.key, required this.label, required this.icon, required this.onPressed});

  final String label;
  final IconData icon;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    return FloatingActionButton.extended(
      onPressed: onPressed,
      backgroundColor: AppTheme.brandBlue,
      elevation: 0,
      highlightElevation: 0,
      icon: Icon(icon, color: AppColors.textOnPrimary, size: 18),
      label: AppText(label, variant: AppTextVariant.body, color: AppColors.textOnPrimary),
    );
  }
}
