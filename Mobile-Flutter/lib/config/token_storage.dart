import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Luu access/refresh token trong secure storage cua thiet bi (Keychain/Keystore)
/// ket hop In-Memory Cache de tranh doc ghi disk/keystore cham tren tung request.
class TokenStorage {
  TokenStorage(this._storage);

  final FlutterSecureStorage _storage;

  static const _accessTokenKey = 'access_token';
  static const _refreshTokenKey = 'refresh_token';

  // In-Memory cache giup truy xuat token voi do tre 0ms tren moi request
  static String? _cachedAccessToken;
  static String? _cachedRefreshToken;

  Future<void> saveTokens({
    required String accessToken,
    String? refreshToken,
  }) async {
    _cachedAccessToken = accessToken;
    if (refreshToken != null) {
      _cachedRefreshToken = refreshToken;
    }

    await _storage.write(key: _accessTokenKey, value: accessToken);
    if (refreshToken != null) {
      await _storage.write(key: _refreshTokenKey, value: refreshToken);
    }
  }

  Future<String?> readAccessToken() async {
    if (_cachedAccessToken != null) {
      return _cachedAccessToken;
    }
    _cachedAccessToken = await _storage.read(key: _accessTokenKey);
    return _cachedAccessToken;
  }

  Future<String?> readRefreshToken() async {
    if (_cachedRefreshToken != null) {
      return _cachedRefreshToken;
    }
    _cachedRefreshToken = await _storage.read(key: _refreshTokenKey);
    return _cachedRefreshToken;
  }

  Future<void> clear() async {
    _cachedAccessToken = null;
    _cachedRefreshToken = null;
    await _storage.delete(key: _accessTokenKey);
    await _storage.delete(key: _refreshTokenKey);
  }
}
