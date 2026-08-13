import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../core/classes/http_manager.dart';
import 'api_endpoint.dart';
import 'app_cache.dart';
import 'token_storage.dart';

/// HttpManager rieng cua du an: gan header `Authorization: Bearer <token>` cho moi request, va
/// khi gap 401 thi xoa sach token + trang thai dang nhap truoc khi HttpManager day nguoi dung
/// ve man dang nhap. Moi Service tu khoi tao rieng mot AppHttp (khong dung DI) — dung mau
/// `final AppHttp _http = AppHttp();` trong tung Service.
class AppHttp extends HttpManager {
  AppHttp({super.headers})
      : super(
          baseUrl: ApiEndpoint.baseUrl,
          onUnauthorized: () async {
            await TokenStorage(const FlutterSecureStorage()).clear();
            await AppCache().doLogout();
          },
        ) {
    dio.interceptors.add(InterceptorsWrapper(onRequest: _onRequest));
  }

  Future<void> _onRequest(
      RequestOptions options, RequestInterceptorHandler handler) async {
    final token =
        await TokenStorage(const FlutterSecureStorage()).readAccessToken();
    if (token != null) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }
}
