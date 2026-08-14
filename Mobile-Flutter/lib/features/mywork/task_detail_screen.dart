import 'package:flutter/material.dart';

import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../shared/widgets/placeholder_body.dart';

class TaskDetailScreen extends StatelessWidget {
  const TaskDetailScreen({super.key, required this.taskId});

  final String taskId;

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: AppAppBar(title: 'Chi tiết công việc #$taskId'),
      // TODO: GET /api/checklist/$taskId (tuong ung ChecklistController) — mo ta, trang thai,
      // comment, timelog, dinh kem.
      body: const PlaceholderBody(
        message: 'Tính năng đang được phát triển, vui lòng quay lại sau.',
      ),
    );
  }
}
