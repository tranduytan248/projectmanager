import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'task_detail_screen.dart';

class TaskDetailController extends StatelessController {
  const TaskDetailController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) {
    final args =
        ModalRoute.of(context)!.settings.arguments as Map<String, dynamic>?;
    final taskId = args?['taskId'] as String? ?? '';

    return Display(
      title: 'Chi tiết công việc',
      mobile: TaskDetailScreen(taskId: taskId),
    );
  }
}
