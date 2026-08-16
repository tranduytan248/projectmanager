import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'my_work_screen.dart';

class MyWorkController extends StatelessController {
  const MyWorkController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) {
    final args =
        ModalRoute.of(context)!.settings.arguments as Map<String, dynamic>?;
    final scope = args?['scope'] as String? ?? 'mine';
    final filter = args?['filter'] as String?;

    return Display(
      title: 'Việc của tôi',
      mobile: MyWorkScreen(scope: scope, filter: filter),
    );
  }
}
