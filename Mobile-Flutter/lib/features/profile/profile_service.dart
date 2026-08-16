import 'package:dio/dio.dart';

import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import 'profile_models.dart';

/// Ket qua mot thao tac tren man Thong tin ca nhan. loi != null nghia la backend tu choi (mat
/// khau hien tai sai, ho ten trong...) — hien nguyen van cho nguoi dung, khop thong diep
/// BadRequest ben backend (AccountApiController), cung khuon voi ChecklistService.CreateTaskResult.
class ProfileActionResult {
  const ProfileActionResult._({this.profile, this.error});

  final ProfileInfo? profile;
  final String? error;

  bool get isSuccess => error == null;
}

class ProfileService {
  final _http = AppHttp();

  Future<ProfileInfo> fetch() async {
    final response = await _http.get(ApiEndpoint.profileMe);
    return ProfileInfo.fromJson(response.data as Map<String, dynamic>);
  }

  Future<ProfileActionResult> updateFullName(String fullName) async {
    try {
      final response = await _http.post(
        ApiEndpoint.profileUpdate,
        data: {'fullName': fullName},
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      return ProfileActionResult._(
          profile: ProfileInfo.fromJson(response.data as Map<String, dynamic>));
    } on DioException catch (e) {
      return ProfileActionResult._(
          error: _errorOf(e, 'Không lưu được thông tin. Hãy thử lại.'));
    }
  }

  Future<ProfileActionResult> changePassword({
    required String currentPassword,
    required String newPassword,
    required String confirmPassword,
  }) async {
    try {
      await _http.post(
        ApiEndpoint.profileChangePassword,
        data: {
          'currentPassword': currentPassword,
          'newPassword': newPassword,
          'confirmPassword': confirmPassword,
        },
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      return const ProfileActionResult._();
    } on DioException catch (e) {
      return ProfileActionResult._(
          error: _errorOf(e, 'Không đổi được mật khẩu. Hãy thử lại.'));
    }
  }

  String _errorOf(DioException e, String fallback) {
    final message = e.response?.statusCode == 400
        ? (e.response?.data is Map
            ? e.response?.data['error'] as String?
            : null)
        : null;
    return message ?? fallback;
  }
}
