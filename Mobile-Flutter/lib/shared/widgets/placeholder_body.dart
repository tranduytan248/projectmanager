import 'package:flutter/material.dart';

import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_text.dart';

/// Body dung tam cho cac man hinh chua noi API that.
/// Thay noi dung nay bang danh sach/form thuc te khi da co backend API.
class PlaceholderBody extends StatelessWidget {
  const PlaceholderBody({super.key, required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: AppText(
          message,
          variant: AppTextVariant.body,
          align: TextAlign.center,
        ),
      ),
    );
  }
}
