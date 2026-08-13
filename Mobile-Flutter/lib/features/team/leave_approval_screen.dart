import 'package:flutter/material.dart';

import '../../shared/widgets/placeholder_body.dart';

class LeaveApprovalScreen extends StatelessWidget {
  const LeaveApprovalScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Duyet nghi phep')),
      body: const PlaceholderBody(
        message:
            'TODO: GET /api/team/leaves + POST /api/team/leaves/{id}/approve (LeavesController)\n'
            'Danh sach cho duyet + hanh dong duyet/tu choi.',
      ),
    );
  }
}
