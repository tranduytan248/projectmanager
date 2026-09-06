import 'dart:convert';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';

import '../../features/app_routes.dart';
import '../../features/notifications/notification_models.dart';
import '../../features/notifications/notifications_service.dart';
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

  FirebaseMessaging? _messagingInstance;
  FirebaseMessaging get _messaging {
    _messagingInstance ??= FirebaseMessaging.instance;
    return _messagingInstance!;
  }

  final FlutterLocalNotificationsPlugin _localNotifications =
      FlutterLocalNotificationsPlugin();

  String? _fcmToken;
  String? get fcmToken => _fcmToken;

  /// Khởi tạo toàn bộ dịch vụ FCM và Local Notifications.
  Future<void> initialize() async {
    if (kIsWeb || Firebase.apps.isEmpty) {
      debugPrint('[FCM] Không khởi tạo FCM native (web hoặc test environment).');
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
      await syncTokenWithBackend();
      _messaging.onTokenRefresh.listen((newToken) {
        debugPrint('[FCM] Token được làm mới: $newToken');
        _fcmToken = newToken;
        Cache.saveData('fcm_device_token', newToken);
        syncTokenWithBackend();
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
      if (kIsWeb || Firebase.apps.isEmpty) return;

      // Trên nền tảng iOS, APNs Token từ Apple phải sẵn sàng thì Firebase mới có thể sinh ra FCM Registration Token.
      if (defaultTargetPlatform == TargetPlatform.iOS) {
        String? apnsToken = await _messaging.getAPNSToken();
        int retryCount = 0;
        while (apnsToken == null && retryCount < 5) {
          retryCount++;
          debugPrint('[FCM] Đang chờ APNs Token từ Apple (lần $retryCount/5)...');
          await Future.delayed(const Duration(seconds: 2));
          apnsToken = await _messaging.getAPNSToken();
        }
        if (apnsToken == null) {
          debugPrint(
              '[FCM] Chưa nhận được APNs Token từ Apple sau 10s. Vui lòng kiểm tra quyền Push Notifications và cấu hình APNs trên Firebase.');
          return;
        }
        debugPrint('[FCM] APNs Token nhận thành công từ Apple.');
      }

      _fcmToken = await _messaging.getToken();
      if (_fcmToken != null) {
        debugPrint('====================================================');
        debugPrint('[FCM] DEVICE TOKEN: $_fcmToken');
        debugPrint('====================================================');
        Cache.saveData('fcm_device_token', _fcmToken!);
      }
    } catch (e, stack) {
      debugPrint('[FCM] Không lấy được FCM token: $e\n$stack');
    }
  }

  /// Kiểm tra xem thiết bị đã sẵn sàng nhận thông báo hay chưa
  bool get isNotificationEnabled {
    final token = _fcmToken ?? Cache.readData<String>('fcm_device_token');
    return token != null && token.isNotEmpty;
  }

  /// Yêu cầu cấp quyền và lưu lại Token thiết bị khi người dùng xác nhận
  Future<bool> requestPermissionAndRegister() async {
    try {
      if (kIsWeb || Firebase.apps.isEmpty) return false;
      final settings = await _messaging.requestPermission(
        alert: true,
        announcement: false,
        badge: true,
        carPlay: false,
        criticalAlert: false,
        provisional: false,
        sound: true,
      );

      final isAuthorized =
          settings.authorizationStatus == AuthorizationStatus.authorized ||
              settings.authorizationStatus == AuthorizationStatus.provisional;

      if (isAuthorized) {
        await _fetchAndStoreToken();
        Cache.saveData('fcm_notification_confirmed', true);
        await syncTokenWithBackend();
        return true;
      }
      return false;
    } catch (e) {
      debugPrint('[FCM] Lỗi yêu cầu cấp quyền thông báo: $e');
      return false;
    }
  }

  /// Đồng bộ FCM Token lên server (chỉ lưu token cho thiết bị mới nhất của tài khoản)
  Future<void> syncTokenWithBackend() async {
    final token = _fcmToken ?? Cache.readData<String>('fcm_device_token');
    if (token == null || token.isEmpty) return;

    try {
      final platform = kIsWeb
          ? 'Web'
          : (defaultTargetPlatform == TargetPlatform.iOS ? 'iOS' : 'Android');
      await NotificationsService()
          .registerDeviceToken(token: token, platform: platform);
      debugPrint('[FCM] Đã đồng bộ Token lên máy chủ thành công.');
    } catch (e) {
      debugPrint('[FCM] Chưa thể đồng bộ Token (có thể chưa đăng nhập): $e');
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

  /// Xử lý điều hướng thông minh theo dữ liệu payload — khớp 100% logic điều hướng khi mở thông báo
  void handlePayload(Map<String, dynamic> data) {
    if (data.isEmpty) {
      _navigateSafely(AppRoutes.notifications);
      return;
    }

    final rawType = (data['type'] ?? data['Type'] ?? '').toString().trim();
    final rawProjectId =
        (data['projectId'] ?? data['project_id'] ?? data['ProjectId'] ?? '')
            .toString()
            .trim();
    final rawTaskId =
        (data['taskId'] ?? data['task_id'] ?? data['TaskId'] ?? '')
            .toString()
            .trim();

    final projectId = int.tryParse(rawProjectId) ?? 0;
    final taskId = int.tryParse(rawTaskId) ?? 0;

    // 0. Trao đổi dự án -> Mở thẳng phòng chat dự án
    if (rawType.toLowerCase() == 'traodoiduan' ||
        rawType.toLowerCase() == 'traodoi') {
      if (projectId > 0) {
        _navigateSafely(AppRoutes.projectDiscussion, arguments: {
          'projectId': projectId,
          'projectName': '',
        });
        return;
      }
    }

    // 1. Vào/rút dự án -> Màn "Dự án của tôi"
    if (rawType == NotificationTypes.projectAdded ||
        rawType == NotificationTypes.projectRemoved ||
        rawType.toLowerCase() == 'vaoduan' ||
        rawType.toLowerCase() == 'roiduan') {
      _navigateSafely(AppRoutes.projects);
      return;
    }

    // 2. Xin nghỉ phép mới -> Màn "Duyệt nghỉ phép" (Toàn Tổ)
    if (rawType == NotificationTypes.leaveRequested ||
        rawType.toLowerCase() == 'leave.request' ||
        rawType.toLowerCase() == 'leaverequested') {
      _navigateSafely(AppRoutes.teamLeaveApprovals);
      return;
    }

    // 3. Kết quả duyệt nghỉ phép -> Màn "Nghỉ phép của tôi"
    if (rawType == NotificationTypes.leaveResult ||
        rawType.toLowerCase() == 'leave.result' ||
        rawType.toLowerCase() == 'leaveresult') {
      _navigateSafely(AppRoutes.leaves);
      return;
    }

    // 4. Nếu có ProjectId -> Mở Checklist dự án
    if (projectId > 0) {
      _navigateSafely(AppRoutes.checklist,
          arguments: {'projectId': projectId.toString()});
      return;
    }

    // 5. Nếu có TaskId (việc ngoài dự án) -> Mở Chi tiết công việc
    if (taskId > 0) {
      _navigateSafely(AppRoutes.taskDetail,
          arguments: {'taskId': taskId.toString()});
      return;
    }

    // 6. Mặc định chuyển đến màn Công việc của tôi
    _navigateSafely(AppRoutes.myWork);
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
