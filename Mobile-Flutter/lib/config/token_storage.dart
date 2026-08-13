import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Luu access/refresh token trong secure storage cua thiet bi (Keychain/Keystore).
/// Thay the co che FormsAuthentication cookie ma web dang dung, vi mobile can token API.
///
/// Co y giu flutter_secure_storage thay vi SharedPreferences (khac voi AppCache) — token la
/// du lieu nhay cam, khong nen luu plaintext du kien truc con lai cua app theo mau CLAUDE.md.
class TokenStorage {
  TokenStorage(this._storage);

  final FlutterSecureStorage _storage;

  static const _accessTokenKey = 'access_token';
  static const _refreshTokenKey = 'refresh_token';

  Future<void> saveTokens(
      {required String accessToken, String? refreshToken}) async {
    await _storage.write(key: _accessTokenKey, value: accessToken);
    if (refreshToken != null) {
      await _storage.write(key: _refreshTokenKey, value: refreshToken);
    }
  }

  Future<String?> readAccessToken() => _storage.read(key: _accessTokenKey);

  Future<String?> readRefreshToken() => _storage.read(key: _refreshTokenKey);

  Future<void> clear() async {
    await _storage.delete(key: _accessTokenKey);
    await _storage.delete(key: _refreshTokenKey);
  }
}
