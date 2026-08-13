import 'package:flutter/material.dart';

import '../../shared/widgets/placeholder_body.dart';

class TaskDetailScreen extends StatelessWidget {
  const TaskDetailScreen({super.key, required this.taskId});

  final String taskId;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Chi tiết công việc #$taskId')),
      body: PlaceholderBody(
        message:
            'TODO: GET /api/checklist/$taskId (tương ứng ChecklistController)\n'
            'Mô tả, trạng thái, comment, timelog, đính kèm.',
      ),
    );
  }
}
