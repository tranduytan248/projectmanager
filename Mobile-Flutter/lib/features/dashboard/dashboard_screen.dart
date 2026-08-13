import 'package:flutter/material.dart';

import '../../shared/widgets/app_bottom_nav.dart';
import '../../shared/widgets/placeholder_body.dart';

class DashboardScreen extends StatelessWidget {
  const DashboardScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppBottomNav(
      currentIndex: 0,
      appBar: AppBar(title: const Text('Tong quan')),
      body: const PlaceholderBody(
        message: 'TODO: GET /api/dashboard (tuong ung DashboardController)\n'
            'So viec, tien do, deadline gan cua toi.',
      ),
    );
  }
}
