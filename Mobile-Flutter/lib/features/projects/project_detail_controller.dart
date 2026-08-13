import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'project_detail_screen.dart';

class ProjectDetailController extends StatelessController {
  const ProjectDetailController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) {
    final args = ModalRoute.of(context)!.settings.arguments as Map<String, dynamic>?;
    final projectId = args?['projectId'] as String? ?? '';

    return Display(
      title: 'Chi tiet du an',
      mobile: ProjectDetailScreen(projectId: projectId),
    );
  }
}
