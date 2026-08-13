import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'private_tasks_screen.dart';

class PrivateTasksController extends StatelessController {
  const PrivateTasksController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) =>
      const Display(title: 'Giao việc riêng', mobile: PrivateTasksScreen());
}
