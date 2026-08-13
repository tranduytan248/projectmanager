import 'package:flutter/material.dart';

import '../../core/classes/route_manager.dart';
import '../../shared/widgets/placeholder_body.dart';
import '../app_routes.dart';

class LeaveListScreen extends StatelessWidget {
  const LeaveListScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Nghi phep cua toi')),
      body: const PlaceholderBody(
        message: 'TODO: GET /api/leaves/mine (tuong ung LeavesController)\n'
            'Lich su dang ky nghi phep.',
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => Nav.toNamed(context, AppRoutes.leaveNew),
        icon: const Icon(Icons.add),
        label: const Text('Dang ky'),
      ),
    );
  }
}
