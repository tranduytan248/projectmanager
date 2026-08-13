import 'package:flutter/material.dart';

import '../../core/classes/controller_manager.dart';
import '../../core/classes/display_manager.dart';
import 'profile_screen.dart';

class ProfileController extends StatelessController {
  const ProfileController({super.key});

  @override
  bool get auth => true;

  @override
  Display view(BuildContext context) => const Display(title: 'Ca nhan', mobile: ProfileScreen());
}
