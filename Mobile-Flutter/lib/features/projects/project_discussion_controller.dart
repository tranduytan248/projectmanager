import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'project_detail_models.dart';
import 'project_discussion_screen.dart';

class ProjectDiscussionController extends StatelessController {
  const ProjectDiscussionController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) {
    final args =
        ModalRoute.of(context)!.settings.arguments as Map<String, dynamic>?;
    final rawProjectId = args?['projectId'];
    final projectId = rawProjectId is int
        ? rawProjectId
        : int.tryParse(rawProjectId?.toString() ?? '0') ?? 0;
    final projectName = args?['projectName'] as String? ?? '';
    final members = (args?['members'] as List<dynamic>?)
            ?.whereType<ProjectMember>()
            .toList() ??
        const [];

    return Display(
      title: 'Trao đổi dự án',
      mobile: ProjectDiscussionScreen(
        projectId: projectId,
        projectName: projectName,
        members: members,
      ),
    );
  }
}
