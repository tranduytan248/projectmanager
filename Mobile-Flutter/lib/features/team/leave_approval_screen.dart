import 'package:flutter/material.dart';

import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../shared/widgets/placeholder_body.dart';

class LeaveApprovalScreen extends StatelessWidget {
  const LeaveApprovalScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const AppScaffold(
      appBar: AppAppBar(title: 'Duyệt nghỉ phép'),
      // API GET /api/team/leaves + POST /api/team/leaves/{id}/approve (LeavesController): danh
      // sach cho duyet + hanh dong duyet/tu choi — chua co, dang cho backend.
      body: PlaceholderBody(
        message: 'Tính năng đang được phát triển, vui lòng quay lại sau.',
      ),
    );
  }
}
