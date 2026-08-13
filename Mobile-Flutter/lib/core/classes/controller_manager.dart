import 'package:flutter/material.dart';

import '../constants/route_names.dart';
import '../helpers/login_helper.dart';
import 'display_manager.dart';

/// Man hinh khong co state rieng, chi bao boc mot [Display] va (tuy chon) doi hoi dang nhap.
/// Moi route trong AppRoutes tuong ung dung mot lop con cua StatelessController — xem
/// features/dashboard/dashboard_controller.dart de biet khuon mau.
abstract class StatelessController extends StatelessWidget {
  const StatelessController({super.key});

  bool get auth => false;
  String get loginUrl => CoreRouteNames.login;

  Display view(BuildContext context);

  @override
  Widget build(BuildContext context) {
    checkLogin(context, auth: auth, loginUrl: loginUrl);
    return view(context);
  }
}

/// Ban co state cua StatelessController, dung khi man hinh can StatefulWidget (vi du co
/// AnimationController rieng) nhung van muon huong guard dang nhap + Display giong nhau.
abstract class StatefulController extends StatefulWidget {
  const StatefulController({super.key});
}

abstract class ControllerState<T extends StatefulController> extends State<T> {
  bool get auth => false;
  String get loginUrl => CoreRouteNames.login;

  Display view(BuildContext context);

  @override
  void initState() {
    super.initState();
    checkLogin(context, auth: auth, loginUrl: loginUrl);
  }

  @override
  Widget build(BuildContext context) => view(context);
}
