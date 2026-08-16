import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'project_members_screen.dart';

class ProjectMembersController extends StatelessController {
  const ProjectMembersController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) {
    final args =
        ModalRoute.of(context)!.settings.arguments as Map<String, dynamic>?;
    final projectId = int.tryParse(args?['projectId'] as String? ?? '') ?? 0;
    final projectName = args?['projectName'] as String? ?? '';
    final canEdit = args?['canEdit'] as bool? ?? false;

    return Display(
      title: 'Nhân sự dự án',
      mobile: ProjectMembersScreen(
        projectId: projectId,
        projectName: projectName,
        canEdit: canEdit,
      ),
    );
  }
}
