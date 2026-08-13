import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'leave_list_screen.dart';

class LeaveListController extends StatelessController {
  const LeaveListController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) =>
      const Display(title: 'Nghi phep cua toi', mobile: LeaveListScreen());
}
