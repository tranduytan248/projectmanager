import 'package:flutter/material.dart';

import '../../shared/widgets/placeholder_body.dart';

class PrivateTasksScreen extends StatelessWidget {
  const PrivateTasksScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Giao việc riêng')),
      body: const PlaceholderBody(
        message:
            'TODO: GET/POST /api/team/private-tasks (tương ứng PrivateTasksController)\n'
            'Tạo/giao task ngoài dự án cho thành viên tổ.',
      ),
    );
  }
}
