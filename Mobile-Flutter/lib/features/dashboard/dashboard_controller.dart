import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'dashboard_screen.dart';

class DashboardController extends StatelessController {
  const DashboardController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) =>
      const Display(title: 'Tổng quan', mobile: DashboardScreen());
}
