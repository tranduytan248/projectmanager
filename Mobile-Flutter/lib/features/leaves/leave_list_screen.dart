import 'package:flutter/material.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../core/classes/route_manager.dart';
import '../../shared/widgets/placeholder_body.dart';
import '../app_routes.dart';

class LeaveListScreen extends StatelessWidget {
  const LeaveListScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Nghỉ phép của tôi')),
      body: const PlaceholderBody(
        message: 'TODO: GET /api/leaves/mine (tương ứng LeavesController)\n'
            'Lịch sử đăng ký nghỉ phép.',
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => Nav.toNamed(context, AppRoutes.leaveNew),
        icon: const PhosphorIcon(PhosphorIconsRegular.plus),
        label: const Text('Đăng ký'),
      ),
    );
  }
}
