import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_dimens.dart';
import '../theme/app_text_styles.dart';
import 'app_loading.dart';
import 'app_text.dart';

/// Loai nut theo NGU NGHIA cua du an (khong theo ten widget Material).
enum AppButtonType { primary, secondary, danger, outline }

/// Nut bam chuan cua du an — dung `.claude/rules/FLUTTER_RULES.md`. Man hinh KHONG duoc dung
/// ElevatedButton/TextButton/OutlinedButton truc tiep, luon qua AppButton. Vung cham toi thieu
/// 48dp (AppDimens.minTapTarget), phang (khong do bong) — vien mong thay cho elevation.
class AppButton extends StatelessWidget {
  const AppButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.type = AppButtonType.primary,
    this.isLoading = false,
    this.fullWidth = false,
    this.icon,
  });

  final String label;
  final VoidCallback? onPressed;
  final AppButtonType type;
  final bool isLoading;
  final bool fullWidth;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    final disabled = isLoading || onPressed == null;
    final child = isLoading
        ? const AppLoading(size: 18, color: Colors.white)
        : icon == null
            ? AppText(label, variant: AppTextVariant.body, weight: FontWeight.w600, color: _fg())
            : Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(icon, size: 18, color: _fg()),
                  const SizedBox(width: AppDimens.space8),
                  AppText(label, variant: AppTextVariant.body, weight: FontWeight.w600, color: _fg()),
                ],
              );

    final button = type == AppButtonType.outline
        ? OutlinedButton(
            onPressed: disabled ? null : onPressed,
            style: OutlinedButton.styleFrom(
              minimumSize: const Size.fromHeight(AppDimens.minTapTarget),
              side: BorderSide(color: _bg()),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppDimens.radiusMd)),
            ),
            child: child,
          )
        : FilledButton(
            onPressed: disabled ? null : onPressed,
            style: FilledButton.styleFrom(
              backgroundColor: _bg(),
              minimumSize: const Size.fromHeight(AppDimens.minTapTarget),
              elevation: 0,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(AppDimens.radiusMd)),
            ),
            child: child,
          );

    return fullWidth ? SizedBox(width: double.infinity, child: button) : button;
  }

  Color _bg() {
    switch (type) {
      case AppButtonType.primary:
        return AppColors.primary;
      case AppButtonType.secondary:
        return AppColors.buttonSecondary;
      case AppButtonType.danger:
        return AppColors.danger;
      case AppButtonType.outline:
        return AppColors.borderStrong;
    }
  }

  Color _fg() {
    switch (type) {
      case AppButtonType.primary:
      case AppButtonType.danger:
        return AppColors.textOnPrimary;
      case AppButtonType.secondary:
      case AppButtonType.outline:
        return AppColors.textPrimary;
    }
  }
}
