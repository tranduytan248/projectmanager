import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'my_projects_screen.dart';

class MyProjectsController extends StatelessController {
  const MyProjectsController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) {
    final args =
        ModalRoute.of(context)!.settings.arguments as Map<String, dynamic>?;
    final scope = args?['scope'] as String? ?? 'mine';
    final roleFilter = args?['roleFilter'] as String?;

    return Display(
      title: 'Dự án của tôi',
      mobile: MyProjectsScreen(scope: scope, initialRoleFilter: roleFilter),
    );
  }
}
