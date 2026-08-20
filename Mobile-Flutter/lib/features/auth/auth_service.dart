import 'package:dio/dio.dart';

import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';

/// Ket qua goi AuthApi/Login. [code] khop quy uoc CLAUDE.md ma [doAuth] tra ve: 1 thanh cong,
/// 0 sai tai khoan/mat khau, -1 loi he thong (mang hoac server) — cac truong con lai chi co gia
/// tri khi code == 1.
class LoginResult {
  const LoginResult._(
      this.code, {this.token, this.displayName, this.role, this.permissions});

  final int code;
  final String? token;
  final String? displayName;
  final String? role;

  /// Danh sach ma quyen tu server (hien chi co the gom "wteam.manage") — dung de
  /// AuthProvider.isTeamManager doc dung ngay sau khi dang nhap, khong con de trong cung.
  final List<String>? permissions;
}

/// Goi that AuthApiController.Login. Action nay nhan (username, password) qua form-encoded body
/// (tham so don gian cua ActionResult, khong phai [FromBody] JSON) nen phai set contentType
/// x-www-form-urlencoded thay vi de Dio tu JSON-hoa Map.
class AuthService {
  final _http = AppHttp();

  Future<LoginResult> login(String username, String password) async {
    try {
      final response = await _http.post(
        ApiEndpoint.login,
        data: {'username': username, 'password': password},
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      final data = response.data as Map<String, dynamic>;
      return LoginResult._(
        1,
        token: data['Token'] as String?,
        displayName: data['DisplayName'] as String?,
        role: data['Role'] as String?,
        permissions:
            (data['Permissions'] as List<dynamic>?)?.cast<String>(),
      );
    } on DioException catch (e) {
      if (e.response?.statusCode == 400) return const LoginResult._(0);
      return const LoginResult._(-1);
    }
  }
}
