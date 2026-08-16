import 'package:flutter/material.dart';

import '../../config/app_theme.dart';
import '../theme/app_colors.dart';
import '../theme/app_dimens.dart';
import '../theme/app_text_styles.dart';
import 'app_text.dart';

/// O chon dung/sai + nhan di kem — dung `.claude/rules/FLUTTER_RULES.md`. Man hinh KHONG duoc
/// dung Checkbox truc tiep, luon qua AppCheckbox. Bao ca nhan trong vung cham toi thieu 48dp
/// (Checkbox mac dinh chi ~40dp ke ca tapTargetSize.padded).
class AppCheckbox extends StatelessWidget {
  const AppCheckbox({
    super.key,
    required this.label,
    required this.value,
    required this.onChanged,
    this.strikeThrough = false,
  });

  final String label;
  final bool value;
  final ValueChanged<bool> onChanged;

  /// Ke ngang qua nhan khi [value] la true — dung cho danh sach "Viec can lam" (dong da xong hien
  /// mo va gach ngang), khong anh huong cac noi da dung AppCheckbox truoc day (mac dinh false).
  final bool strikeThrough;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: () => onChanged(!value),
      borderRadius: BorderRadius.circular(AppDimens.radiusSm),
      child: ConstrainedBox(
        constraints: const BoxConstraints(minHeight: AppDimens.minTapTarget),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Checkbox(
              value: value,
              activeColor: AppTheme.brandBlue,
              onChanged: (v) => onChanged(v ?? !value),
            ),
            Flexible(
              child: AppText(
                label,
                variant: AppTextVariant.body,
                color: strikeThrough && value ? AppColors.textSecondary : null,
                decoration:
                    strikeThrough && value ? TextDecoration.lineThrough : null,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
