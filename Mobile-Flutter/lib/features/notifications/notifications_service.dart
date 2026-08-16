import 'package:dio/dio.dart';

import '../../config/api_endpoint.dart';
import '../../config/app_http.dart';
import 'notification_models.dart';

class NotificationsService {
  final _http = AppHttp();

  Future<NotificationPage> fetch({int page = 1}) async {
    final response = await _http.get(
      ApiEndpoint.notifications,
      params: {'page': page},
    );
    return NotificationPage.fromJson(response.data as Map<String, dynamic>);
  }

  Future<void> markRead(int id) async {
    await _http.post(
      ApiEndpoint.notificationMarkRead,
      data: {'id': id},
      options: Options(contentType: Headers.formUrlEncodedContentType),
    );
  }

  Future<void> markAllRead() async {
    await _http.post(ApiEndpoint.notificationMarkAllRead);
  }
}
