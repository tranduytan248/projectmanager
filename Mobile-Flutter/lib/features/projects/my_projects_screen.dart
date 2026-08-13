import 'package:flutter/material.dart';

import '../../shared/widgets/placeholder_body.dart';

class MyProjectsScreen extends StatelessWidget {
  const MyProjectsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Du an cua toi')),
      body: const PlaceholderBody(
        message: 'TODO: GET /api/projects/mine (tuong ung WorkProjectsController)\n'
            'Danh sach du an dang tham gia.',
      ),
    );
  }
}
