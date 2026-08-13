import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'kpi_screen.dart';

class KpiController extends StatelessController {
  const KpiController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) => const Display(title: 'KPI cua toi', mobile: KpiScreen());
}
