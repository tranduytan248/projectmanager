import 'dart:convert';

import '../core/classes/cache_manager.dart';

/// Lop ngu nghia phia tren [Cache]: luu TRANG THAI HIEN THI cua phien dang nhap (co dang nhap
/// hay khong, ten hien thi, danh sach quyen) — KHONG luu token. Token nhay cam nam rieng trong
/// TokenStorage (flutter_secure_storage). Tach hai key `auth_data`/`login_info` ro rang, khong
/// dung chung mot key cho ca co-dang-nhap va du-lieu-dang-nhap.
class AppCache {
  static const _authDataKey = 'auth_data';
  static const _loginInfoKey = 'login_info';

  Future<void> doLogin() => Cache.saveData(_authDataKey, true);

  Future<bool> isLogin() async => Cache.readData<bool>(_authDataKey) ?? false;

  Future<void> doLogout() async {
    await Cache.deleteData(_authDataKey);
    await Cache.deleteData(_loginInfoKey);
  }

  Future<void> saveLoginInfo(
      {required String displayName, required List<String> permissions}) {
    return Cache.saveData(
      _loginInfoKey,
      jsonEncode({'displayName': displayName, 'permissions': permissions}),
    );
  }

  Map<String, dynamic>? getLoginInfo() {
    final raw = Cache.readData<String>(_loginInfoKey);
    if (raw == null) return null;
    return jsonDecode(raw) as Map<String, dynamic>;
  }
}
