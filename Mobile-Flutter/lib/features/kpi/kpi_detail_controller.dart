import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'kpi_detail_screen.dart';

class KpiDetailController extends StatelessController {
  const KpiDetailController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) {
    final args =
        ModalRoute.of(context)!.settings.arguments as Map<String, dynamic>?;

    final userId = args?['userId'] is int
        ? args!['userId'] as int
        : int.tryParse(args?['userId']?.toString() ?? '0') ?? 0;

    final userName = args?['userName']?.toString() ?? 'Nhân sự';
    final year = args?['year'] is int
        ? args!['year'] as int
        : int.tryParse(args?['year']?.toString() ?? '') ?? DateTime.now().year;

    final month = args?['month'] is int
        ? args!['month'] as int
        : int.tryParse(args?['month']?.toString() ?? '') ?? DateTime.now().month;

    return Display(
      title: 'Chi tiết KPI',
      mobile: KpiDetailScreen(
        userId: userId,
        userName: userName,
        year: year,
        month: month,
      ),
    );
  }
}
