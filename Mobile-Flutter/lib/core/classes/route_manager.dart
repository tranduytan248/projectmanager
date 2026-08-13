import 'package:flutter/material.dart';

import 'app_keys.dart';

/// Bang dang ky route phang cho mot module (feature). Moi module tu mo rong lop nay, khai bao
/// route cua rieng minh trong constructor bang [addRoute]/[addAll], va export ten route thanh
/// hang so static de noi khac tham chieu. Khong co nested navigator, khong onGenerateRoute —
/// dung dung cho MaterialApp.routes.
abstract class RouteManager {
  final Map<String, WidgetBuilder> _routes = {};

  void addRoute(String name, WidgetBuilder builder) {
    _routes[name] = builder;
  }

  void addAll(Map<String, WidgetBuilder> routes) {
    _routes.addAll(routes);
  }

  Map<String, WidgetBuilder> get routes => _routes;
}

/// Helper dieu huong dung navigatorKey toan cuc — goi duoc tu bat ky dau (kho core khong can
/// BuildContext cua man hinh hien tai, vi du HttpManager khi force-logout).
class Nav {
  Nav._();

  /// Day mot man moi len tren cung (giu lai man cu trong stack).
  static Future<T?> toNamed<T>(BuildContext context, String routeName,
      {Object? arguments}) {
    return Navigator.of(context).pushNamed<T>(routeName, arguments: arguments);
  }

  /// Thay man hien tai bang man moi (dung khi chuyen tab, hoac khi dang nhap xong thi khong
  /// muon nguoi dung bam Back quay lai man Login).
  static Future<T?> to<T>(BuildContext context, String routeName,
      {Object? arguments}) {
    return Navigator.of(context)
        .pushReplacementNamed<T, void>(routeName, arguments: arguments);
  }

  /// Dong man hien tai, quay lai man truoc.
  static void close<T extends Object?>(BuildContext context, [T? result]) {
    Navigator.of(context).pop<T>(result);
  }

  /// Day mot man moi len tren cung va xoa toan bo stack cu — dung khi force-logout.
  static Future<T?> toAndClearStack<T>(String routeName, {Object? arguments}) {
    final state = navigatorKey.currentState;
    if (state == null) return Future.value(null);
    return state.pushNamedAndRemoveUntil<T>(routeName, (route) => false,
        arguments: arguments);
  }
}
