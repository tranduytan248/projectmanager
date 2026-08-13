import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'notifications_screen.dart';

class NotificationsController extends StatelessController {
  const NotificationsController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) =>
      const Display(title: 'Thong bao', mobile: NotificationsScreen());
}
