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
      currentIndex: 4,
      appBar: AppBar(title: const Text('Bảng điều khiển Tổ')),
      body: const PlaceholderBody(
        message:
            'TODO: GET /api/team/dashboard (tương ứng TeamDashboardController)\n'
            'Tổng hợp tiến độ cả tổ, workload, danh sách dự án.',
      ),
    );
  }
}
