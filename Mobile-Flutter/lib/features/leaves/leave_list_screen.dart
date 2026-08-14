import 'package:flutter/material.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../core/classes/route_manager.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_fab.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../shared/widgets/placeholder_body.dart';
import '../app_routes.dart';

class LeaveListScreen extends StatelessWidget {
  const LeaveListScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: const AppAppBar(title: 'Nghỉ phép của tôi'),
      // GET /api/leaves/mine (tương ứng LeavesController) - lịch sử đăng ký nghỉ phép.
      body: const PlaceholderBody(
        message: 'Tính năng đang được phát triển, vui lòng quay lại sau.',
      ),
      floatingActionButton: AppFab(
        label: 'Đăng ký',
        icon: PhosphorIconsRegular.plus,
        onPressed: () => Nav.toNamed(context, AppRoutes.leaveNew),
      ),
    );
  }
}
