import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'team_dashboard_screen.dart';

class TeamDashboardController extends StatelessController {
  const TeamDashboardController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) =>
      const Display(title: 'Bang dieu khien To', mobile: TeamDashboardScreen());
}
