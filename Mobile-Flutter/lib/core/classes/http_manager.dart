import 'dart:async';
import 'dart:io';

import 'package:dio/dio.dart';

import '../constants/route_names.dart';
import '../utils/dialog_service.dart';
import 'route_manager.dart';

/// Dio wrapper voi bo verb chuan + interceptor loi toan cuc dung chung cho moi Service.
///
/// - Loi mang (timeout/connection error/SocketException): gom nhom theo `options.extra['group']`
///   de CHI hien mot dialog "Mat ket noi" cho ca nhom, thay vi moi request loi la mot dialog —
///   cac request cung nhom dang cho se dung chung ket qua cua request dau tien qua Completer.
/// - Loi 401: goi [onUnauthorized] (do lop con — vi du AppHttp — tiem vao de xoa cache/token),
///   roi day nguoi dung ve man dang nhap va xoa het stack dieu huong. Co flag tinh chong goi lap
///   khi nhieu request cung tra 401 mot luc.
/// - Loi 403/404/500/502/503: hien dialog canh bao chung chung — Service khong can tu xu ly UI.
class HttpManager {
  HttpManager({
    required String baseUrl,
    Map<String, String>? headers,
    this.onUnauthorized,
    this.loginRouteName = CoreRouteNames.login,
  }) : dio = Dio(BaseOptions(baseUrl: baseUrl, headers: headers)) {
    dio.interceptors.add(InterceptorsWrapper(onError: _onError));
  }

  final Dio dio;
  final Future<void> Function()? onUnauthorized;
  final String loginRouteName;

  static bool _isLoggingOut = false;

  final Map<String, Completer<bool>> _groupLocks = {};

  bool _isNetworkError(DioException error) {
    return error.type == DioExceptionType.connectionTimeout ||
        error.type == DioExceptionType.sendTimeout ||
        error.type == DioExceptionType.receiveTimeout ||
        error.type == DioExceptionType.connectionError ||
        error.error is SocketException;
  }

  Future<void> _onError(
      DioException error, ErrorInterceptorHandler handler) async {
    final statusCode = error.response?.statusCode;

    if (statusCode == 401) {
      await _forceLogout();
    } else if (_isNetworkError(error)) {
      await _showOfflineDialogOnce(error);
    } else if (statusCode != null &&
        {403, 404, 500, 502, 503}.contains(statusCode)) {
      await DialogService.showWarning(
        'May chu tra ve loi ($statusCode). Vui long thu lai sau.',
      );
    }

    handler.next(error);
  }

  Future<void> _showOfflineDialogOnce(DioException error) async {
    final group = error.requestOptions.extra['group']?.toString() ??
        '${error.requestOptions.method}:${error.requestOptions.uri}';

    final existing = _groupLocks[group];
    if (existing != null) {
      await existing.future;
      return;
    }

    final completer = Completer<bool>();
    _groupLocks[group] = completer;
    try {
      await DialogService.showWarning(
          'Mat ket noi mang. Vui long kiem tra va thu lai.');
    } finally {
      _groupLocks.remove(group);
      completer.complete(false);
    }
  }

  Future<void> _forceLogout() async {
    if (_isLoggingOut) return;
    _isLoggingOut = true;
    try {
      await onUnauthorized?.call();
      await Nav.toAndClearStack(loginRouteName);
      await DialogService.showWarning(
          'Phien dang nhap da het han, vui long dang nhap lai.');
    } finally {
      _isLoggingOut = false;
    }
  }

  Future<Response<T>> get<T>(String path,
      {Map<String, dynamic>? params, Options? options}) {
    return dio.get<T>(path, queryParameters: params, options: options);
  }

  Future<Response<T>> post<T>(String path, {dynamic data, Options? options}) {
    return dio.post<T>(path, data: data, options: options);
  }

  Future<Response<T>> put<T>(String path, {dynamic data, Options? options}) {
    return dio.put<T>(path, data: data, options: options);
  }

  Future<Response<T>> patch<T>(String path, {dynamic data, Options? options}) {
    return dio.patch<T>(path, data: data, options: options);
  }

  Future<Response<T>> delete<T>(String path, {dynamic data, Options? options}) {
    return dio.delete<T>(path, data: data, options: options);
  }
}
