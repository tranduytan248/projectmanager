import 'package:flutter/material.dart';

import '../../core/classes/route_manager.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';

class LeaveRequestFormScreen extends StatelessWidget {
  const LeaveRequestFormScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: const AppAppBar(title: 'Đăng ký nghỉ phép'),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Form ngày bắt đầu/kết thúc, lý do, gửi POST /api/leaves.
            const AppText(
                'Tính năng đang được phát triển, vui lòng quay lại sau.',
                variant: AppTextVariant.caption),
            const SizedBox(height: 16),
            const AppTextField(
              label: 'Lý do nghỉ phép',
              maxLines: 3,
              keyboardType: TextInputType.multiline,
            ),
            const SizedBox(height: 24),
            AppButton(
              label: 'Gửi yêu cầu',
              onPressed: () => Nav.close(context),
            ),
          ],
        ),
      ),
    );
  }
}
