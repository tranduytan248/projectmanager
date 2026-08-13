import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../config/app_cache.dart';
import '../../config/token_storage.dart';
import '../classes/route_manager.dart';

final _appCache = AppCache();
final _tokenStorage = TokenStorage(const FlutterSecureStorage());

/// Dang nhap. TODO: thay bang goi that qua AppHttp/ApiEndpoint.login khi backend co API tra JWT.
/// Hien tai luon thanh cong (dung de dung khung UI/dieu huong) — tra 1 = thanh cong, khop quy
/// uoc "1 thanh cong / 0 sai tai khoan / -1 loi server" cua CLAUDE.md du chua co nhanh 0/-1 that.
Future<int> doAuth(BuildContext context, String username, String password) async {
  await _tokenStorage.saveTokens(accessToken: 'fake-token-for-scaffold');
  await _appCache.doLogin();
  await _appCache.saveLoginInfo(displayName: username, permissions: const ['*']);
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
Future<void> checkLogin(BuildContext context, {required bool auth, required String loginUrl}) async {
  if (!auth) return;

  final isLogin = await _appCache.isLogin();
  if (isLogin || !context.mounted) return;

  WidgetsBinding.instance.addPostFrameCallback((_) {
    if (context.mounted) Nav.toAndClearStack(loginUrl);
  });
}
