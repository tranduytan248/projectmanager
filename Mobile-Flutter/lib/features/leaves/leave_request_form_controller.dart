import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'leave_request_form_screen.dart';

class LeaveRequestFormController extends StatelessController {
  const LeaveRequestFormController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) => const Display(
      title: 'Dang ky nghi phep', mobile: LeaveRequestFormScreen());
}
