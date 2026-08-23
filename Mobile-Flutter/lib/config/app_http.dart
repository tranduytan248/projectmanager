import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../core/classes/http_manager.dart';
import 'api_endpoint.dart';
import 'app_cache.dart';
import 'token_storage.dart';

/// HttpManager rieng cua du an: su dung Singleton de tai su dung Connection Pool (Keep-Alive),
/// gan header `Authorization: Bearer <token>` cho moi request va xu ly 401 tap trung.
class AppHttp extends HttpManager {
  factory AppHttp({Map<String, String>? headers}) {
    if (headers != null) {
      return AppHttp._internal(headers: headers);
    }
    return _instance ??= AppHttp._internal();
  }

  AppHttp._internal({super.headers})
      : super(
          baseUrl: ApiEndpoint.baseUrl,
          onUnauthorized: () async {
            await TokenStorage(const FlutterSecureStorage()).clear();
            await AppCache().doLogout();
          },
        ) {
    dio.options.connectTimeout = const Duration(seconds: 15);
    dio.options.receiveTimeout = const Duration(seconds: 15);
    dio.options.sendTimeout = const Duration(seconds: 15);
    dio.interceptors.add(InterceptorsWrapper(onRequest: _onRequest));
  }

  static AppHttp? _instance;

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
