import 'package:flutter/material.dart';

import '../../shared/widgets/placeholder_body.dart';

/// Chi hien thi voi user co quyen wteam.manage (xem AppScaffold + PermissionGate).
class TeamDashboardScreen extends StatelessWidget {
  const TeamDashboardScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Bang dieu khien To')),
      body: const PlaceholderBody(
        message: 'TODO: GET /api/team/dashboard (tuong ung TeamDashboardController)\n'
            'Tong hop tien do ca to, workload, danh sach du an.',
      ),
    );
  }
}
