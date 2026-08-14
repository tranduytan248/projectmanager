import 'package:flutter/material.dart';

import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../shared/widgets/placeholder_body.dart';

class PrivateTasksScreen extends StatelessWidget {
  const PrivateTasksScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const AppScaffold(
      appBar: AppAppBar(title: 'Giao việc riêng'),
      // API GET/POST /api/team/private-tasks (PrivateTasksController): tao/giao task ngoai du an
      // cho thanh vien to — chua co, dang cho backend.
      body: PlaceholderBody(
        message: 'Tính năng đang được phát triển, vui lòng quay lại sau.',
      ),
    );
  }
}
