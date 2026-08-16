import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_text_styles.dart';
import 'app_card.dart';
import 'app_text.dart';

/// Mot khoi thong tin co tieu de trong man Chi tiet (du an/cong viec/ca nhan) — truoc day cac
/// man nay tu viet lai y het khoi nay rieng le, nay gom mot cho theo dung tinh than "khong
/// duplicate code".
class AppDetailSection extends StatelessWidget {
  const AppDetailSection({super.key, required this.title, required this.child});

  final String title;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          AppText(title,
              variant: AppTextVariant.body,
              fontSize: 13,
              weight: FontWeight.w700),
          const SizedBox(height: 10),
          child,
        ],
      ),
    );
  }
}

/// Mot dong nhan/gia tri trong [AppDetailSection] — cot nhan co do rong co dinh de nhieu dong
/// thang hang nhau. [labelWidth] tuy chinh vi man Chi tiet du an dung nhan ngan hon (Khach hang,
/// PM...) nen can cot hep hon man Chi tiet cong viec/Thong tin ca nhan.
class AppDetailInfoRow extends StatelessWidget {
  const AppDetailInfoRow(
      {super.key,
      required this.label,
      required this.value,
      this.labelWidth = 112});

  final String label;
  final String value;
  final double labelWidth;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: labelWidth,
            child: AppText(label,
                variant: AppTextVariant.caption,
                fontSize: 12.5,
                color: AppColors.textSecondary),
          ),
          Expanded(
            child: AppText(value,
                variant: AppTextVariant.body,
                fontSize: 13,
                weight: FontWeight.w600),
          ),
        ],
      ),
    );
  }
}
