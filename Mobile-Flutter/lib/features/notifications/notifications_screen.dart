import 'package:flutter/material.dart';

import '../../shared/widgets/app_bottom_nav.dart';
import '../../shared/widgets/placeholder_body.dart';

class NotificationsScreen extends StatelessWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppBottomNav(
      currentIndex: 3,
      appBar: AppBar(title: const Text('Thông báo')),
      body: const PlaceholderBody(
        message:
            'TODO: GET /api/notifications (tương ứng UserNotificationsController)\n'
            'Kết hợp Firebase Cloud Messaging để báo push.',
      ),
    );
  }
}
