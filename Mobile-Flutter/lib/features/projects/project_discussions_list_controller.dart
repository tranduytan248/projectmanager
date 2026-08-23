import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'project_discussions_list_screen.dart';

class ProjectDiscussionsListController extends StatelessController {
  const ProjectDiscussionsListController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) {
    return const Display(
      title: 'Trao đổi dự án',
      mobile: ProjectDiscussionsListScreen(),
    );
  }
}
