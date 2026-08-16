import 'package:flutter/material.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../config/app_theme.dart';
import '../theme/app_text_styles.dart';
import 'app_button.dart';
import 'app_text.dart';

/// Khoi "tai khong duoc + nut Thu lai" dung chung cho moi man co goi API — truoc day moi man
/// (Dashboard, Du an, Cong viec, Chi tiet cong viec, Thong bao, Thong tin ca nhan...) tu viet
/// lai y het khoi nay, nay gom mot cho theo dung tinh than "khong duplicate code".
class AppErrorState extends StatelessWidget {
  const AppErrorState(
      {super.key, required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const PhosphorIcon(PhosphorIconsRegular.warningCircle,
                color: AppTheme.statusDanger, size: 32),
            const SizedBox(height: 12),
            AppText(message,
                variant: AppTextVariant.body, align: TextAlign.center),
            const SizedBox(height: 12),
            AppButton(label: 'Thử lại', onPressed: onRetry),
          ],
        ),
      ),
    );
  }
}
