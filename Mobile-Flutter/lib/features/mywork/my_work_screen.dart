import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../shared/widgets/placeholder_body.dart';

class MyWorkScreen extends StatelessWidget {
  const MyWorkScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Cong viec cua toi')),
      body: const PlaceholderBody(
        message: 'TODO: GET /api/mywork (tuong ung MyWorkController)\n'
            'Danh sach task duoc giao. Tap 1 item de mo chi tiet.',
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => context.go('/my-work/task/demo-id'),
        tooltip: 'Xem thu chi tiet task (demo)',
        child: const Icon(Icons.arrow_forward),
      ),
    );
  }
}
