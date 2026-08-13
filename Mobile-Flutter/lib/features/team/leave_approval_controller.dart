import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'leave_approval_screen.dart';

class LeaveApprovalController extends StatelessController {
  const LeaveApprovalController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) =>
      const Display(title: 'Duyet nghi phep', mobile: LeaveApprovalScreen());
}
