import 'dart:convert';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';

import '../../features/app_routes.dart';
import '../classes/app_keys.dart';
import '../classes/cache_manager.dart';

/// Top-level background message handler cho Firebase Cloud Messaging.
/// Bắt buộc phải là hàm top-level và có annotation @pragma('vm:entry-point') để Flutter engine
/// có thể gọi khi app đang ở background hoặc terminated.
@pragma('vm:entry-point')
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  await Firebase.initializeApp();
  debugPrint('[FCM Background] Nhận tin nhắn ngầm ID: ${message.messageId}');
  debugPrint('[FCM Background] Title: ${message.notification?.title}, Data: ${message.data}');
}

/// Dịch vụ quản lý thông báo Firebase Cloud Messaging toàn app.
class FcmNotificationService {
  FcmNotificationService._();

  static final FcmNotificationService instance = FcmNotificationService._();

  static const String _channelId = 'brewtask_high_importance';
  static const String _channelName = 'Thông báo quan trọng';
  static const String _channelDesc = 'Nhận thông báo công việc, trao đổi và nhắc việc';

  final FirebaseMessaging _messaging = FirebaseMessaging.instance;
  final FlutterLocalNotificationsPlugin _localNotifications =
      FlutterLocalNotificationsPlugin();

  String? _fcmToken;
  String? get fcmToken => _fcmToken;

  /// Khởi tạo toàn bộ dịch vụ FCM và Local Notifications.
  Future<void> initialize() async {
    if (kIsWeb) {
      debugPrint('[FCM] Nền tảng Web không dùng cấu hình native FCM service.');
      return;
    }

    try {
      // 1. Cấu hình Local Notifications để hiện Popup Banner khi app đang mở (Foreground)
      const androidInit = AndroidInitializationSettings('@mipmap/ic_launcher');
      const iosInit = DarwinInitializationSettings(
        requestAlertPermission: true,
        requestBadgePermission: true,
        requestSoundPermission: true,
      );
      const initSettings = InitializationSettings(
        android: androidInit,
        iOS: iosInit,
      );

      await _localNotifications.initialize(
        initSettings,
        onDidReceiveNotificationResponse: (NotificationResponse response) {
          if (response.payload != null && response.payload!.isNotEmpty) {
            try {
              final Map<String, dynamic> data =
                  jsonDecode(response.payload!) as Map<String, dynamic>;
              handlePayload(data);
            } catch (e) {
              debugPrint('[FCM] Lỗi parse payload click notification: $e');
            }
          }
        },
      );

      // 2. Tạo Notification Channel độ ưu tiên cao trên Android
      final androidPlugin = _localNotifications
          .resolvePlatformSpecificImplementation<
              AndroidFlutterLocalNotificationsPlugin>();
      if (androidPlugin != null) {
        await androidPlugin.createNotificationChannel(
          const AndroidNotificationChannel(
            _channelId,
            _channelName,
            description: _channelDesc,
            importance: Importance.max,
            playSound: true,
            enableVibration: true,
          ),
        );
      }

      // 3. Xin quyền thông báo (bắt buộc từ Android 13+ và iOS)
      final settings = await _messaging.requestPermission(
        alert: true,
        announcement: false,
        badge: true,
        carPlay: false,
        criticalAlert: false,
        provisional: false,
        sound: true,
      );
      debugPrint('[FCM] Trạng thái cấp quyền: ${settings.authorizationStatus}');

      // 4. Lấy và lắng nghe FCM Token
      await _fetchAndStoreToken();
      _messaging.onTokenRefresh.listen((newToken) {
        debugPrint('[FCM] Token được làm mới: $newToken');
        _fcmToken = newToken;
        Cache.saveData('fcm_device_token', newToken);
      });

      // 5. Đăng ký nhận tin nhắn khi app đang mở (Foreground)
      FirebaseMessaging.onMessage.listen(_handleForegroundMessage);

      // 6. Xử lý khi người dùng chạm vào thông báo từ Background
      FirebaseMessaging.onMessageOpenedApp.listen((RemoteMessage message) {
        debugPrint('[FCM] Mở app từ thông báo Background: ${message.data}');
        handlePayload(message.data);
      });

      // 7. Xử lý khi mở app từ trạng thái Terminated (app đã tắt hoàn toàn)
      final initialMessage = await _messaging.getInitialMessage();
      if (initialMessage != null) {
        debugPrint('[FCM] Mở app từ trạng thái Terminated: ${initialMessage.data}');
        // Delay nhẹ để Navigator sẵn sàng sau khi Flutter dựng màn hình đầu tiên
        Future.delayed(const Duration(milliseconds: 500), () {
          handlePayload(initialMessage.data);
        });
      }

      debugPrint('[FCM] Khởi tạo FcmNotificationService thành công.');
    } catch (e, stack) {
      debugPrint('[FCM] Lỗi khởi tạo FcmNotificationService: $e\n$stack');
    }
  }

  /// Lấy FCM Token từ thiết bị và lưu cache
  Future<void> _fetchAndStoreToken() async {
    try {
      _fcmToken = await _messaging.getToken();
      if (_fcmToken != null) {
        debugPrint('====================================================');
        debugPrint('[FCM] DEVICE TOKEN: $_fcmToken');
        debugPrint('====================================================');
        Cache.saveData('fcm_device_token', _fcmToken!);
      }
    } catch (e) {
      debugPrint('[FCM] Không lấy được FCM token: $e');
    }
  }

  /// Xử lý tin nhắn khi app đang mở (Foreground)
  void _handleForegroundMessage(RemoteMessage message) {
    debugPrint('[FCM Foreground] Nhận tin nhắn: ${message.notification?.title} - ${message.notification?.body}');
    debugPrint('[FCM Foreground] Data payload: ${message.data}');

    final notification = message.notification;
    final title = notification?.title ?? message.data['title'] ?? 'BrewTask';
    final body = notification?.body ?? message.data['body'] ?? message.data['message'] ?? '';

    const androidDetails = AndroidNotificationDetails(
      _channelId,
      _channelName,
      channelDescription: _channelDesc,
      importance: Importance.max,
      priority: Priority.high,
      icon: '@mipmap/ic_launcher',
      playSound: true,
      enableVibration: true,
    );
    const iosDetails = DarwinNotificationDetails(
      presentAlert: true,
      presentBadge: true,
      presentSound: true,
    );

    const notificationDetails = NotificationDetails(
      android: androidDetails,
      iOS: iosDetails,
    );

    final notificationId = DateTime.now().millisecondsSinceEpoch ~/ 1000;

    _localNotifications.show(
      notificationId,
      title,
      body,
      notificationDetails,
      payload: jsonEncode(message.data),
    );
  }

  /// Xử lý điều hướng thông minh theo dữ liệu payload
  void handlePayload(Map<String, dynamic> data) {
    if (data.isEmpty) {
      _navigateSafely(AppRoutes.notifications);
      return;
    }

    final taskId = data['taskId'] ?? data['task_id'] ?? data['TaskId'];
    final projectId = data['projectId'] ?? data['project_id'] ?? data['ProjectId'];
    final type = (data['type'] ?? data['Type'] ?? '').toString().toLowerCase();

    if (taskId != null && taskId.toString().isNotEmpty) {
      _navigateSafely(AppRoutes.taskDetail, arguments: {'taskId': taskId.toString()});
      return;
    }

    if (projectId != null && projectId.toString().isNotEmpty) {
      _navigateSafely(AppRoutes.projectDetail, arguments: {'projectId': projectId.toString()});
      return;
    }

    if (type.contains('leave') || type.contains('phep')) {
      _navigateSafely(AppRoutes.teamLeaveApprovals);
      return;
    }

    if (type.contains('kpi')) {
      _navigateSafely(AppRoutes.kpi);
      return;
    }

    // Mặc định chuyển đến màn danh sách thông báo
    _navigateSafely(AppRoutes.notifications);
  }

  void _navigateSafely(String routeName, {Object? arguments}) {
    final navState = navigatorKey.currentState;
    if (navState != null) {
      navState.pushNamed(routeName, arguments: arguments);
    } else {
      debugPrint('[FCM] navigatorKey chưa sẵn sàng để điều hướng đến $routeName');
    }
  }
}
