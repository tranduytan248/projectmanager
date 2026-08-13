import 'package:flutter/material.dart';

import '../../config/app_cache.dart';
import '../../core/constants/route_names.dart';
import '../../core/helpers/login_helper.dart';

/// Trang thai hien thi cua phien dang nhap — thay AuthController (StateNotifier) cu bang
/// ChangeNotifier, dung mau CLAUDE.md "mot ChangeNotifier moi vung tinh nang, dang ky trong
/// appProviders". Viec dang nhap/dang xuat thuc thi qua login_helper (doAuth/doLogout); provider
/// nay chi giu ket qua de UI doc.
class AuthProvider extends ChangeNotifier {
  AuthProvider() {
    _hydrate();
  }

  final _appCache = AppCache();

  bool _isAuthenticated = false;
  String? _displayName;
  List<String> _permissions = const [];

  bool get isAuthenticated => _isAuthenticated;
  String? get displayName => _displayName;
  List<String> get permissions => _permissions;

  /// Tuong ung co "Quan ly To" (wteam.manage) o web.
  bool get isTeamManager => _permissions.contains('*') || _permissions.contains('wteam.manage');

  /// Doc lai trang thai da luu (neu co) ngay khi provider duoc tao — de app biet ngay tu dau
  /// nguoi dung da dang nhap tu truoc hay chua, khong phai doi mot vong build.
  Future<void> _hydrate() async {
    final isLogin = await _appCache.isLogin();
    if (!isLogin) return;

    final info = _appCache.getLoginInfo();
    _isAuthenticated = true;
    _displayName = info?['displayName'] as String?;
    _permissions = (info?['permissions'] as List?)?.cast<String>() ?? const [];
    notifyListeners();
  }

  Future<void> login(BuildContext context, String username, String password) async {
    final result = await doAuth(context, username, password);
    if (result != 1) return;

    _isAuthenticated = true;
    _displayName = username;
    _permissions = const ['*'];
    notifyListeners();
  }

  Future<void> logout(BuildContext context) async {
    await doLogout(context, loginUrl: CoreRouteNames.login);
    _isAuthenticated = false;
    _displayName = null;
    _permissions = const [];
    notifyListeners();
  }
}
