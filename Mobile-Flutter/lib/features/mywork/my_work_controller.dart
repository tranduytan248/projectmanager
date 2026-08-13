import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'my_work_screen.dart';

class MyWorkController extends StatelessController {
  const MyWorkController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) =>
      const Display(title: 'Việc của tôi', mobile: MyWorkScreen());
}
