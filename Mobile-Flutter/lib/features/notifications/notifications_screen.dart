import 'package:flutter/material.dart';

import '../../shared/widgets/placeholder_body.dart';

class NotificationsScreen extends StatelessWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Thong bao')),
      body: const PlaceholderBody(
        message: 'TODO: GET /api/notifications (tuong ung UserNotificationsController)\n'
            'Ket hop Firebase Cloud Messaging de bao push.',
      ),
    );
  }
}
