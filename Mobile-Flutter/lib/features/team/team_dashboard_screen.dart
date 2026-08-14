import 'package:flutter/material.dart';

import '../../core/widgets/app_app_bar.dart';
import '../../shared/widgets/app_bottom_nav.dart';
import '../../shared/widgets/placeholder_body.dart';

/// Khong con loi vao qua tab bottom nav (xem AppBottomNav) — chi mo duoc qua route truc tiep/deep
/// link khi user co quyen team.manage.
class TeamDashboardScreen extends StatelessWidget {
  const TeamDashboardScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const AppBottomNav(
      currentIndex: 4,
      appBar: AppAppBar(title: 'Bảng điều khiển Tổ'),
      // API GET /api/team/dashboard (TeamDashboardController): tong hop tien do ca to, workload,
      // danh sach du an — chua co, dang cho backend.
      body: PlaceholderBody(
        message: 'Tính năng đang được phát triển, vui lòng quay lại sau.',
      ),
    );
  }
}
