import 'package:flutter/material.dart';

import '../../config/app_cache.dart';
import '../../core/constants/route_names.dart';
import '../../core/helpers/login_helper.dart';
import '../../core/services/fcm_notification_service.dart';
import '../../core/utils/toast_service.dart';

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
  bool get isTeamManager =>
      _permissions.contains('*') || _permissions.contains('wteam.manage');

  /// Duoc tao du an moi — CHI Quan tri (toan quyen "*") HOAC tai khoan duoc tich "Là Quản lý Tổ"
  /// (isTeamManager). KHONG dung rieng permission "wprojects.create" — nham voi "nhom quyen Quan
  /// ly" (mot nhom quyen co the co wprojects.create nhung KHONG duoc coi la Quan ly To, nguoi
  /// dung da xac nhan ro nhom "Quan ly" khong duoc thay chuc nang nay). "*" duoc kiem rieng du
  /// isTeamManager da bao gom no (Can("wteam.manage") luon dung voi "*") — de ro y hon la dua vao
  /// mot hieu ung phu cua isTeamManager.
  bool get canCreateProject => _permissions.contains('*') || isTeamManager;

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
    FcmNotificationService.instance.syncTokenWithBackend();
  }

  /// Doc lai displayName/permissions tu cache — goi tu ControllerManager moi khi mot man hinh
  /// yeu cau dang nhap duoc mo (xem StatelessController/ControllerState). Da ghi nhan (khong
  /// tim ra duoc dong code cu the gay ra du dieu tra ky): sau khi login() vua gan dung
  /// _displayName/_permissions, man Dashboard duoc Navigator.pushReplacementNamed sang NGAY
  /// SAU DO lai doc duoc gia tri rong o lan build dau tien — hien "Chao, ban!" thay vi ten
  /// that, chi tu het khi thoat han app roi mo lai (luc do _hydrate() o tren chay dung tu dau).
  /// Goi lai ham nay o MOI man hinh yeu cau dang nhap la luoi an toan: du roi vao dung truong
  /// hop nao thi man cung tu doc lai dung tu cache khi mo, khong phu thuoc thoi diem
  /// notifyListeners() cua login() lan truyen toi dau.
  Future<void> ensureFresh() async {
    final isLogin = await _appCache.isLogin();
    if (!isLogin) return;

    final info = _appCache.getLoginInfo();
    final name = info?['displayName'] as String?;
    final perms = (info?['permissions'] as List?)?.cast<String>() ?? const [];
    if (_isAuthenticated && _displayName == name && _sameList(_permissions, perms)) {
      return;
    }

    _isAuthenticated = true;
    _displayName = name;
    _permissions = perms;
    notifyListeners();
  }

  static bool _sameList(List<String> a, List<String> b) {
    if (a.length != b.length) return false;
    for (var i = 0; i < a.length; i++) {
      if (a[i] != b[i]) return false;
    }
    return true;
  }

  /// Tra ve true/false de LoginScreen biet co dieu huong sang Dashboard hay khong. Loi (sai
  /// tai khoan hoac loi server) hien thi ngay tai day qua ToastService thay vi bat LoginScreen
  /// tu doc ma loi 0/-1 — cung mot cho xu ly du sau nay them nhieu diem goi login() khac.
  Future<bool> login(
      BuildContext context, String username, String password) async {
    final result = await doAuth(context, username, password);
    if (result == 0) {
      ToastService.show('Sai tài khoản hoặc mật khẩu.', type: ToastType.error);
      return false;
    }
    if (result == -1) {
      ToastService.show('Lỗi hệ thống, vui lòng thử lại sau.',
          type: ToastType.error);
      return false;
    }

    // Doc lai tu AppCache (khong dung thang username/['*']) de khop chinh xac voi nhung gi
    // doAuth vua luu — cung mot nguon voi _hydrate() dung sau khi app khoi dong lai.
    final info = _appCache.getLoginInfo();
    _isAuthenticated = true;
    _displayName = info?['displayName'] as String? ?? username;
    _permissions = (info?['permissions'] as List?)?.cast<String>() ?? const [];
    notifyListeners();
    FcmNotificationService.instance.syncTokenWithBackend();
    return true;
  }

  Future<void> logout(BuildContext context) async {
    await doLogout(context, loginUrl: CoreRouteNames.login);
    _isAuthenticated = false;
    _displayName = null;
    _permissions = const [];
    notifyListeners();
  }
}
