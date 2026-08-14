import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_dimens.dart';
import '../theme/app_text_styles.dart';
import 'app_text.dart';

/// O chon 1 gia tri tu danh sach — dung `.claude/rules/FLUTTER_RULES.md`. Man hinh KHONG duoc
/// dung DropdownButton/DropdownButtonFormField truc tiep, luon qua AppDropdown. [items] anh xa
/// gia tri -> nhan hien thi (khong lo DropdownMenuItem cua Material ra ngoai API). Nhan dat theo
/// kieu floating label — dong bo voi AppTextField, khong tach rieng AppText nhan phia tren.
class AppDropdown<T> extends StatelessWidget {
  const AppDropdown({
    super.key,
    required this.label,
    required this.value,
    required this.items,
    required this.onChanged,
    this.hint = 'Tất cả',
  });

  final String label;
  final T? value;
  final Map<T, String> items;
  final ValueChanged<T?> onChanged;
  final String hint;

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<T>(
      initialValue: value,
      isExpanded: true,
      borderRadius: BorderRadius.circular(AppDimens.radiusMd),
      decoration: InputDecoration(
        labelText: label,
        filled: true,
        fillColor: AppColors.surface,
        constraints: const BoxConstraints(minHeight: AppDimens.minTapTarget),
        contentPadding:
            const EdgeInsets.symmetric(horizontal: AppDimens.space12, vertical: AppDimens.space12),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppDimens.radiusMd),
          borderSide: const BorderSide(color: AppColors.border),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppDimens.radiusMd),
          borderSide: const BorderSide(color: AppColors.border),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(AppDimens.radiusMd),
          borderSide: const BorderSide(color: AppColors.primary, width: 1.5),
        ),
      ),
      hint: AppText(hint, variant: AppTextVariant.body, color: AppColors.textFaint),
      items: [
        for (final entry in items.entries)
          DropdownMenuItem<T>(value: entry.key, child: AppText(entry.value, variant: AppTextVariant.body)),
      ],
      onChanged: onChanged,
    );
  }
}
