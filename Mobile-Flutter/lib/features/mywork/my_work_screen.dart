import 'package:flutter/material.dart';

import '../../core/classes/route_manager.dart';
import '../../shared/widgets/app_bottom_nav.dart';
import '../../shared/widgets/placeholder_body.dart';
import '../app_routes.dart';

class MyWorkScreen extends StatelessWidget {
  const MyWorkScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppBottomNav(
      currentIndex: 1,
      appBar: AppBar(title: const Text('Cong viec cua toi')),
      body: const PlaceholderBody(
        message: 'TODO: GET /api/mywork (tuong ung MyWorkController)\n'
            'Danh sach task duoc giao. Tap 1 item de mo chi tiet.',
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => Nav.toNamed(
          context,
          AppRoutes.taskDetail,
          arguments: {'taskId': 'demo-id'},
        ),
        tooltip: 'Xem thu chi tiet task (demo)',
        child: const Icon(Icons.arrow_forward),
      ),
    );
  }
}
