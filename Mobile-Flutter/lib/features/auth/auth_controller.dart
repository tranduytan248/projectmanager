import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/providers/core_providers.dart';
import '../../core/storage/token_storage.dart';

class AuthState {
  const AuthState({
    this.isAuthenticated = false,
    this.displayName,
    this.permissions = const [],
  });

  final bool isAuthenticated;
  final String? displayName;

  /// Danh sach quyen dang "module.action" tra ve tu API /auth/login hoac /auth/me.
  final List<String> permissions;

  /// Tuong ung co "Quan ly To" (wteam.manage) o web.
  bool get isTeamManager => permissions.contains('*') || permissions.contains('wteam.manage');

  AuthState copyWith({
    bool? isAuthenticated,
    String? displayName,
    List<String>? permissions,
  }) {
    return AuthState(
      isAuthenticated: isAuthenticated ?? this.isAuthenticated,
      displayName: displayName ?? this.displayName,
      permissions: permissions ?? this.permissions,
    );
  }
}

class AuthController extends StateNotifier<AuthState> {
  AuthController(this._tokenStorage) : super(const AuthState());

  final TokenStorage _tokenStorage;

  /// TODO: thay bang goi that qua ApiClient.dio.post(ApiEndpoints.login, ...)
  /// khi backend co API tra JWT. Hien tai chi dung de dung khung UI/dieu huong.
  Future<void> login(String username, String password) async {
    await _tokenStorage.saveTokens(accessToken: 'fake-token-for-scaffold');
    state = state.copyWith(
      isAuthenticated: true,
      displayName: username,
      permissions: const ['*'],
    );
  }

  Future<void> logout() async {
    await _tokenStorage.clear();
    state = const AuthState();
  }
}

final authControllerProvider = StateNotifierProvider<AuthController, AuthState>((ref) {
  return AuthController(ref.watch(tokenStorageProvider));
});
