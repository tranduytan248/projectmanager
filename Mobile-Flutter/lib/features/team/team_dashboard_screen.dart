import 'package:flutter/material.dart';

import '../../shared/widgets/app_bottom_nav.dart';
import '../../shared/widgets/placeholder_body.dart';

/// Chi co lối vao qua tab "To" khi user co quyen wteam.manage (xem AppBottomNav +
/// PermissionGate).
class TeamDashboardScreen extends StatelessWidget {
  const TeamDashboardScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppBottomNav(
      currentIndex: 5,
      appBar: AppBar(title: const Text('Bang dieu khien To')),
      body: const PlaceholderBody(
        message: 'TODO: GET /api/team/dashboard (tuong ung TeamDashboardController)\n'
            'Tong hop tien do ca to, workload, danh sach du an.',
      ),
    );
  }
}
