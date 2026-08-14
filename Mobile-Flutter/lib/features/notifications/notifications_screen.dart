import 'package:flutter/material.dart';

import '../../core/widgets/app_app_bar.dart';
import '../../shared/widgets/app_bottom_nav.dart';
import '../../shared/widgets/placeholder_body.dart';

class NotificationsScreen extends StatelessWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const AppBottomNav(
      // Man Thong bao khong nam trong 4 tab chinh (chi mo qua chuong thong bao tren Dashboard),
      // nen dung chi so ngoai pham vi 4 tab de khong tab nao bi to sang nham.
      currentIndex: 4,
      appBar: AppAppBar(title: 'Thông báo'),
      // API GET /api/notifications (UserNotificationsController) ket hop Firebase Cloud
      // Messaging de bao push — chua co, dang cho backend.
      body: PlaceholderBody(
        message: 'Tính năng đang được phát triển, vui lòng quay lại sau.',
      ),
    );
  }
}
