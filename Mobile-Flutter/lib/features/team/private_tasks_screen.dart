import 'package:flutter/material.dart';

import '../../shared/widgets/placeholder_body.dart';

class PrivateTasksScreen extends StatelessWidget {
  const PrivateTasksScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Giao viec rieng')),
      body: const PlaceholderBody(
        message:
            'TODO: GET/POST /api/team/private-tasks (tuong ung PrivateTasksController)\n'
            'Tao/giao task ngoai du an cho thanh vien to.',
      ),
    );
  }
}
