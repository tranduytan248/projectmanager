import 'package:flutter/material.dart';

import '../../core/classes/route_manager.dart';

class LeaveRequestFormScreen extends StatelessWidget {
  const LeaveRequestFormScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Đăng ký nghỉ phép')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
                'TODO: form ngày bắt đầu/kết thúc, lý do, gửi POST /api/leaves.'),
            const SizedBox(height: 16),
            const TextField(decoration: InputDecoration(labelText: 'Lý do')),
            const SizedBox(height: 24),
            FilledButton(
              onPressed: () => Nav.close(context),
              child: const Text('Gửi yêu cầu'),
            ),
          ],
        ),
      ),
    );
  }
}
