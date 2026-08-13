import 'package:flutter/material.dart';

import '../../shared/widgets/app_bottom_nav.dart';
import '../../shared/widgets/placeholder_body.dart';

class MyProjectsScreen extends StatelessWidget {
  const MyProjectsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppBottomNav(
      currentIndex: 1,
      appBar: AppBar(title: const Text('Dự án của tôi')),
      body: const PlaceholderBody(
        message:
            'TODO: GET /api/projects/mine (tương ứng WorkProjectsController)\n'
            'Danh sách dự án đang tham gia.',
      ),
    );
  }
}
