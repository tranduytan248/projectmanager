import 'dart:async';
import 'dart:io';
import 'package:dio/dio.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_database/firebase_database.dart';
import 'package:flutter/foundation.dart';

import '../../config/api_endpoint.dart';
import '../../config/app_cache.dart';
import '../../config/app_http.dart';
import '../../core/classes/cache_manager.dart';
import 'project_discussion_models.dart';

/// Service quản lý trao đổi thời gian thực trong Dự án qua Firebase Realtime Database.
class ProjectDiscussionService {
  final _http = AppHttp();

  static const String databaseUrl =
      'https://brewtask-99719-default-rtdb.asia-southeast1.firebasedatabase.app';

  FirebaseDatabase? _databaseInstance;
  FirebaseDatabase get _database {
    if (_databaseInstance == null) {
      if (Firebase.apps.isNotEmpty) {
        _databaseInstance = FirebaseDatabase.instanceFor(
          app: Firebase.app(),
          databaseURL: databaseUrl,
        );
      } else {
        _databaseInstance = FirebaseDatabase.instance;
      }
    }
    return _databaseInstance!;
  }

  DatabaseReference _discussionsRef(int projectId) {
    return _database.ref('projects/$projectId/discussions');
  }

  DatabaseReference _summaryRef(int projectId) {
    return _database.ref('projects/$projectId/summary');
  }

  /// Lắng nghe luồng tin nhắn trao đổi thời gian thực
  Stream<List<ProjectDiscussionMessage>> streamMessages(int projectId) {
    if (kIsWeb || Firebase.apps.isEmpty) {
      return Stream.value([]);
    }

    return _discussionsRef(projectId).onValue.map((event) {
      final snapshot = event.snapshot;
      if (!snapshot.exists || snapshot.value == null) {
        return <ProjectDiscussionMessage>[];
      }

      final rawData = snapshot.value;
      final messages = <ProjectDiscussionMessage>[];

      if (rawData is Map) {
        rawData.forEach((key, val) {
          if (val is Map) {
            final id = key.toString();
            final mapData = Map<String, dynamic>.from(val);
            messages.add(ProjectDiscussionMessage.fromMap(id, mapData));
          }
        });
      }

      messages.sort((a, b) {
        final timeA = a.createdAt?.millisecondsSinceEpoch ?? 0;
        final timeB = b.createdAt?.millisecondsSinceEpoch ?? 0;
        return timeA.compareTo(timeB);
      });

      return messages;
    });
  }

  /// Lắng nghe tóm tắt tin nhắn cuối của một dự án
  Stream<Map<dynamic, dynamic>?> streamProjectSummary(int projectId) {
    if (kIsWeb || Firebase.apps.isEmpty) {
      return const Stream.empty();
    }

    return _summaryRef(projectId).onValue.map((event) {
      final val = event.snapshot.value;
      if (val is Map) {
        return val;
      }
      return null;
    });
  }

  static final ValueNotifier<int> readStateNotifier = ValueNotifier<int>(0);

  /// Đánh dấu là đã đọc toàn bộ tin nhắn của một dự án
  Future<void> markAsRead(int projectId) async {
    final now = DateTime.now().millisecondsSinceEpoch;
    await Cache.saveData('disc_last_read_$projectId', now);
    readStateNotifier.value = now;
  }

  /// Lấy thời điểm đọc cuối cùng của một dự án
  int getLastReadTime(int projectId) {
    return Cache.readData<int>('disc_last_read_$projectId') ?? 0;
  }

  /// Kiểm tra xem tin nhắn có phải do chính mình gửi hay không
  bool _isMyMessage(Map msgVal, Map<String, dynamic>? loginInfo) {
    final myDisplayName = (loginInfo?['displayName'] as String? ?? '').trim().toLowerCase();
    final myUsername = (loginInfo?['username'] as String? ?? '').trim().toLowerCase();
    final myUserId = loginInfo?['userId'] is int
        ? loginInfo!['userId'] as int
        : int.tryParse(loginInfo?['userId']?.toString() ?? '') ?? 0;

    final senderName = (msgVal['senderName'] as String? ?? '').trim().toLowerCase();
    final senderUsername = (msgVal['senderUsername'] as String? ?? '').trim().toLowerCase();
    final senderId = msgVal['senderId'] is int
        ? msgVal['senderId'] as int
        : int.tryParse(msgVal['senderId']?.toString() ?? '') ?? 0;

    if (myUserId > 0 && senderId == myUserId) return true;
    if (myUsername.isNotEmpty && senderUsername.isNotEmpty && senderUsername == myUsername) return true;
    if (myDisplayName.isNotEmpty && senderName.isNotEmpty && senderName == myDisplayName) return true;
    if (myUsername.isNotEmpty && senderName.isNotEmpty && senderName == myUsername) return true;
    if (senderName == 'tôi') return true;

    return false;
  }

  /// Lắng nghe số lượng tin nhắn CHƯA ĐỌC trong một dự án
  Stream<int> streamMessageCount(int projectId) {
    if (kIsWeb || Firebase.apps.isEmpty) {
      return Stream.value(0);
    }

    late StreamController<int> controller;
    DataSnapshot? latestSnapshot;
    StreamSubscription? dbSub;
    VoidCallback? listener;

    void update() {
      if (controller.isClosed) return;
      if (latestSnapshot == null || !latestSnapshot!.exists || latestSnapshot!.value == null) {
        controller.add(0);
        return;
      }

      final loginInfo = AppCache().getLoginInfo();
      final lastRead = getLastReadTime(projectId);
      final rawData = latestSnapshot!.value;
      int unread = 0;

      if (rawData is Map) {
        rawData.forEach((key, val) {
          if (val is Map) {
            final isFromMe = _isMyMessage(val, loginInfo);
            final createdAt = val['createdAt'];
            int timeMs = 0;
            if (createdAt is int) {
              timeMs = createdAt;
            } else if (createdAt is String) {
              timeMs = DateTime.tryParse(createdAt)?.millisecondsSinceEpoch ?? 0;
            }

            if (!isFromMe && timeMs > lastRead) {
              unread++;
            }
          }
        });
      }
      controller.add(unread);
    }

    controller = StreamController<int>(
      onListen: () {
        dbSub = _discussionsRef(projectId).onValue.listen((event) {
          latestSnapshot = event.snapshot;
          update();
        });

        listener = () => update();
        readStateNotifier.addListener(listener!);
      },
      onCancel: () {
        dbSub?.cancel();
        if (listener != null) {
          readStateNotifier.removeListener(listener!);
        }
        controller.close();
      },
    );

    return controller.stream;
  }

  /// Lắng nghe tổng số tin nhắn CHƯA ĐỌC trên các dự án người dùng tham gia
  Stream<int> streamTotalDiscussionsCount({List<int>? userProjectIds}) {
    if (kIsWeb || Firebase.apps.isEmpty) {
      return Stream.value(0);
    }

    late StreamController<int> controller;
    DataSnapshot? latestSnapshot;
    StreamSubscription? dbSub;
    VoidCallback? listener;

    void update() {
      if (controller.isClosed) return;
      if (latestSnapshot == null || !latestSnapshot!.exists || latestSnapshot!.value == null) {
        controller.add(0);
        return;
      }

      final loginInfo = AppCache().getLoginInfo();
      final rawData = latestSnapshot!.value;
      int totalUnread = 0;

      // Lấy danh sách ID dự án được phép đếm (từ tham số hoặc từ cache)
      Set<int>? allowedProjectIds = userProjectIds?.where((id) => id > 0).toSet();
      if (allowedProjectIds == null || allowedProjectIds.isEmpty) {
        final cached = Cache.readData<List>('my_user_project_ids');
        if (cached != null && cached.isNotEmpty) {
          allowedProjectIds = cached
              .map((e) => int.tryParse(e.toString()) ?? 0)
              .where((id) => id > 0)
              .toSet();
        }
      }

      if (rawData is Map) {
        rawData.forEach((projKey, projVal) {
          final pid = int.tryParse(projKey.toString()) ?? 0;
          if (pid > 0 && projVal is Map) {
            // Chỉ đếm nếu là dự án người dùng tham gia
            if (allowedProjectIds != null && allowedProjectIds.isNotEmpty && !allowedProjectIds.contains(pid)) {
              return;
            }

            final discussions = projVal['discussions'];
            if (discussions is Map && discussions.isNotEmpty) {
              final lastRead = getLastReadTime(pid);
              discussions.forEach((msgKey, msgVal) {
                if (msgVal is Map) {
                  final isFromMe = _isMyMessage(msgVal, loginInfo);
                  final createdAt = msgVal['createdAt'];
                  int timeMs = 0;
                  if (createdAt is int) {
                    timeMs = createdAt;
                  } else if (createdAt is String) {
                    timeMs = DateTime.tryParse(createdAt)?.millisecondsSinceEpoch ?? 0;
                  }

                  if (!isFromMe && timeMs > lastRead) {
                    totalUnread++;
                  }
                }
              });
            }
          }
        });
      }
      controller.add(totalUnread);
    }

    controller = StreamController<int>(
      onListen: () {
        dbSub = _database.ref('projects').onValue.listen((event) {
          latestSnapshot = event.snapshot;
          update();
        });

        listener = () => update();
        readStateNotifier.addListener(listener!);
      },
      onCancel: () {
        dbSub?.cancel();
        if (listener != null) {
          readStateNotifier.removeListener(listener!);
        }
        controller.close();
      },
    );

    return controller.stream;
  }

  /// Lấy danh sách công việc (Task) trong dự án để gợi ý khi gõ '/'
  Future<List<ProjectTaskOption>> fetchProjectTasks(int projectId) async {
    try {
      final response = await _http.get(
        ApiEndpoint.discussionsTasks,
        params: {'projectId': projectId},
      );
      if (response.data is Map) {
        final rawList = response.data['tasks'];
        if (rawList is List) {
          return rawList
              .whereType<Map<String, dynamic>>()
              .map((json) => ProjectTaskOption.fromJson(json))
              .toList();
        }
      }
      return [];
    } catch (e) {
      debugPrint('[Discussions] Khong the tai danh sach task: $e');
      return [];
    }
  }

  /// Tải lên file đính kèm (Ảnh, Video <= 10MB, Tài liệu)
  Future<Map<String, dynamic>?> uploadAttachment({
    required int projectId,
    required String filePath,
    required String fileName,
  }) async {
    final file = File(filePath);
    if (!file.existsSync()) {
      throw Exception('Tập tin không tồn tại trên thiết bị.');
    }

    final fileSize = file.lengthSync();
    if (fileSize > 10 * 1024 * 1024) {
      throw Exception('File/Video đính kèm vượt quá giới hạn tối đa 10 MB.');
    }

    final formData = FormData.fromMap({
      'file': await MultipartFile.fromFile(filePath, filename: fileName),
    });

    final response = await _http.post(
      '${ApiEndpoint.discussionsUpload}?projectId=$projectId',
      data: formData,
    );

    if (response.data is Map && response.data['success'] == true) {
      return Map<String, dynamic>.from(response.data as Map);
    } else {
      final errorMsg = response.data is Map ? response.data['error'] : null;
      throw Exception(errorMsg ?? 'Không thể tải file đính kèm lên.');
    }
  }

  /// Gửi một tin nhắn trao đổi mới vào Realtime Database (hỗ trợ ảnh/video/task)
  Future<void> sendMessage({
    required int projectId,
    required String content,
    required int senderId,
    required String senderName,
    required String senderUsername,
    String senderAvatar = '',
    String projectName = '',
    String type = 'text',
    String attachmentUrl = '',
    String attachmentName = '',
    int attachmentSize = 0,
    String attachmentSizeLabel = '',
  }) async {
    final trimmed = content.trim();
    if (trimmed.isEmpty && attachmentUrl.isEmpty) return;

    if (Firebase.apps.isNotEmpty) {
      try {
        debugPrint('[RTDB] Dang gui tin nhan toi du an $projectId...');
        final newMsgRef = _discussionsRef(projectId).push();
        final payload = {
          'projectId': projectId,
          'senderId': senderId,
          'senderName': senderName,
          'senderUsername': senderUsername,
          'senderAvatar': senderAvatar,
          'content': trimmed,
          'createdAt': ServerValue.timestamp,
          'type': type,
          'attachmentUrl': attachmentUrl,
          'attachmentName': attachmentName,
          'attachmentSize': attachmentSize,
          'attachmentSizeLabel': attachmentSizeLabel,
        };
        await newMsgRef.set(payload);
        debugPrint('[RTDB] Da tao message thanh cong: ${newMsgRef.key}');

        // Cập nhật tóm tắt tin nhắn cuối lên node summary
        final summaryText = trimmed.isNotEmpty
            ? trimmed
            : (type == 'image'
                ? '[Hình ảnh]'
                : type == 'video'
                    ? '[Video]'
                    : '[Tài liệu đính kèm]');

        await _summaryRef(projectId).set({
          'projectId': projectId,
          'projectName': projectName,
          'lastMessage': summaryText,
          'lastSenderName': senderName,
          'lastSenderId': senderId,
          'lastUpdatedAt': ServerValue.timestamp,
        });
        debugPrint('[RTDB] Da cap nhat project summary thanh cong');
      } catch (e, stack) {
        debugPrint(
            '[RTDB] Loi khi gui tin nhan vao Realtime Database: $e\n$stack');
        rethrow;
      }
    }

    // Gửi Push Notification ngầm tới các thành viên dự án qua Backend
    try {
      final summaryText = trimmed.isNotEmpty
          ? trimmed
          : (type == 'image'
              ? '[Hình ảnh]'
              : type == 'video'
                  ? '[Video]'
                  : '[Tài liệu đính kèm]');

      _notifyMembersInBackground(
        projectId: projectId,
        content: summaryText,
        senderName: senderName,
      );
    } catch (_) {}
  }

  /// Gọi API Backend ngầm để gửi Push Notification cho các thành viên trong dự án
  void _notifyMembersInBackground({
    required int projectId,
    required String content,
    required String senderName,
  }) {
    Future.microtask(() async {
      try {
        await _http.post(
          '/api/NotificationsApi/SendProjectDiscussionNotification',
          data: {
            'ProjectId': projectId,
            'Content': content,
            'SenderName': senderName,
          },
        );
      } catch (_) {
        // Nuốt lỗi thông báo để không gián đoạn giao diện chat
      }
    });
  }
}
