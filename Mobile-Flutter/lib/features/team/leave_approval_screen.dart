import 'package:flutter/material.dart';

import '../../shared/widgets/placeholder_body.dart';

class LeaveApprovalScreen extends StatelessWidget {
  const LeaveApprovalScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Duyệt nghỉ phép')),
      body: const PlaceholderBody(
        message:
            'TODO: GET /api/team/leaves + POST /api/team/leaves/{id}/approve (LeavesController)\n'
            'Danh sách chờ duyệt + hành động duyệt/từ chối.',
      ),
    );
  }
}
