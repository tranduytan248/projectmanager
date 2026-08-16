import 'package:dio/dio.dart';

import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';

/// Ket qua mot thao tac cua man "Quen mat khau". [message] luon co gia tri (server tra thong bao
/// ro rang du thanh cong hay khong — xem AuthApiController.RequestPasswordResetOtp/ResetPasswordWithOtp).
class ForgotPasswordResult {
  const ForgotPasswordResult._({required this.ok, required this.message});

  final bool ok;
  final String message;
}

/// Goi AuthApiController.RequestPasswordResetOtp/ResetPasswordWithOtp — cung khuon form-urlencoded
/// voi AuthService.login (tham so don gian cua ActionResult, khong phai [FromBody] JSON).
class ForgotPasswordService {
  final _http = AppHttp();

  Future<ForgotPasswordResult> requestOtp(String phone) async {
    try {
      final response = await _http.post(
        ApiEndpoint.forgotPasswordRequestOtp,
        data: {'phone': phone},
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      final data = response.data as Map<String, dynamic>;
      return ForgotPasswordResult._(
          ok: true, message: data['message'] as String? ?? '');
    } on DioException catch (e) {
      return ForgotPasswordResult._(ok: false, message: _errorOf(e));
    }
  }

  Future<ForgotPasswordResult> resetPassword(
      {required String phone, required String code}) async {
    try {
      await _http.post(
        ApiEndpoint.forgotPasswordReset,
        data: {'phone': phone, 'code': code},
        options: Options(contentType: Headers.formUrlEncodedContentType),
      );
      return const ForgotPasswordResult._(
          ok: true,
          message:
              'Mật khẩu mới đã được gửi qua tin nhắn tới số điện thoại của bạn.');
    } on DioException catch (e) {
      return ForgotPasswordResult._(ok: false, message: _errorOf(e));
    }
  }

  String _errorOf(DioException e) {
    final message = e.response?.statusCode == 400
        ? (e.response?.data is Map
            ? e.response?.data['error'] as String?
            : null)
        : null;
    return message ?? 'Không thực hiện được. Hãy thử lại.';
  }
}
