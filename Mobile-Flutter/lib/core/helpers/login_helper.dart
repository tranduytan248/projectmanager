import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../config/app_cache.dart';
import '../../config/token_storage.dart';
import '../../features/auth/auth_service.dart';
import '../classes/route_manager.dart';

final _appCache = AppCache();
final _tokenStorage = TokenStorage(const FlutterSecureStorage());
final _authService = AuthService();

/// Dang nhap that qua AuthApi/Login. Tra 1 = thanh cong / 0 sai tai khoan / -1 loi he thong,
/// khop quy uoc cua CLAUDE.md.
Future<int> doAuth(
    BuildContext context, String username, String password) async {
  final result = await _authService.login(username, password);
  if (result.code != 1) return result.code;

  await _tokenStorage.saveTokens(accessToken: result.token!);
  await _appCache.doLogin();
  // AuthApi/Login chi tra Role (chuoi don), chua co danh sach quyen chi tiet nhu web — de trong
  // permissions cho toi khi backend tra them, man "Quan ly To" se tu an vi AuthProvider.isTeamManager
  // doc rong.
  await _appCache.saveLoginInfo(
      displayName: result.displayName ?? username, permissions: const []);
  return 1;
}

Future<void> doLogout(BuildContext context, {required String loginUrl}) async {
  await _tokenStorage.clear();
  await _appCache.doLogout();
  if (context.mounted) {
    await Nav.toAndClearStack(loginUrl);
  }
}

/// Guard dung trong ControllerManager: neu man can dang nhap (`auth == true`) ma chua dang nhap,
/// day nguoi dung ve [loginUrl] SAU KHI FRAME HIEN TAI DA VE XONG (post-frame) — tranh goi
/// Navigator giua luc build() dang chay.
Future<void> checkLogin(BuildContext context,
    {required bool auth, required String loginUrl}) async {
  if (!auth) return;

  final isLogin = await _appCache.isLogin();
  if (isLogin || !context.mounted) return;

  WidgetsBinding.instance.addPostFrameCallback((_) {
    if (context.mounted) Nav.toAndClearStack(loginUrl);
  });
}
