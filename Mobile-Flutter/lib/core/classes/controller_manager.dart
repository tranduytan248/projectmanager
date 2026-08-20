import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../features/auth/auth_provider.dart';
import '../constants/route_names.dart';
import '../helpers/login_helper.dart';
import 'display_manager.dart';

/// Cho man hinh yeu cau dang nhap tu doc lai displayName/permissions tu cache (xem
/// AuthProvider.ensureFresh) moi khi mo — luoi an toan cho mot loi da ghi nhan: sau khi vua
/// dang nhap xong, man dau tien duoc day sang (Navigator.pushReplacementNamed) doi khi doc
/// duoc AuthProvider voi displayName rong dù login() vua gan dung, dan den hien "Chao, ban!"
/// thay ten that. Goi lai o day (diem chung moi man hinh yeu cau dang nhap deu di qua) dam
/// bao du roi vao truong hop nao thi man cung tu sua dung ngay khi mo.
void _refreshAuthIfNeeded(BuildContext context, {required bool auth}) {
  if (!auth) return;
  context.read<AuthProvider>().ensureFresh();
}

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
    _refreshAuthIfNeeded(context, auth: auth);
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
    _refreshAuthIfNeeded(context, auth: auth);
  }

  @override
  Widget build(BuildContext context) => view(context);
}
