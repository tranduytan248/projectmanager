import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'checklist_board_screen.dart';

class ChecklistBoardController extends StatelessController {
  const ChecklistBoardController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) {
    final args =
        ModalRoute.of(context)!.settings.arguments as Map<String, dynamic>?;
    final projectId = args?['projectId'] as String? ?? '';

    return Display(
      title: 'Checklist du an',
      mobile: ChecklistBoardScreen(projectId: projectId),
    );
  }
}
