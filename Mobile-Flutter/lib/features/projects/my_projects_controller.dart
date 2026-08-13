import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'my_projects_screen.dart';

class MyProjectsController extends StatelessController {
  const MyProjectsController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) =>
      const Display(title: 'Du an cua toi', mobile: MyProjectsScreen());
}
