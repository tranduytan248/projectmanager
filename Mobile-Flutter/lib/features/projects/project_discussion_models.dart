import 'package:flutter/foundation.dart';
import '../../config/api_endpoint.dart';

/// Model tin nhắn trao đổi trong Dự án (lưu trữ trên Firebase Realtime Database).
class ProjectDiscussionMessage {
  const ProjectDiscussionMessage({
    required this.id,
    required this.projectId,
    required this.senderId,
    required this.senderName,
    required this.senderUsername,
    required this.senderAvatar,
    required this.content,
    required this.createdAt,
    this.type = 'text',
    this.attachmentUrl = '',
    this.attachmentName = '',
    this.attachmentSize = 0,
    this.attachmentSizeLabel = '',
  });

  final String id;
  final int projectId;
  final int senderId;
  final String senderName;
  final String senderUsername;
  final String senderAvatar;
  final String content;
  final DateTime? createdAt;
  final String type;
  final String attachmentUrl;
  final String attachmentName;
  final int attachmentSize;
  final String attachmentSizeLabel;

  bool get hasAttachment => attachmentUrl.isNotEmpty;
  bool get isImage => type == 'image' || RegExp(r'\.(png|jpg|jpeg|gif|webp|bmp)$', caseSensitive: false).hasMatch(attachmentUrl);
  bool get isVideo => type == 'video' || RegExp(r'\.(mp4|mov|webm|m4v)$', caseSensitive: false).hasMatch(attachmentUrl);

  /// Trả về URL đầy đủ có thể tải được trên mobile (hỗ trợ cả relative URL và máy ảo Android)
  String get resolvedAttachmentUrl {
    if (attachmentUrl.isEmpty) return '';
    if (attachmentUrl.startsWith('http://') || attachmentUrl.startsWith('https://')) {
      if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) {
        final uri = Uri.tryParse(attachmentUrl);
        if (uri != null &&
            (uri.host == 'localhost' ||
                uri.host == '127.0.0.1' ||
                uri.host == 'prm.vn' ||
                uri.host == 'pm.vn')) {
          return uri.replace(host: '10.0.2.2', port: 8080).toString();
        }
      }
      return attachmentUrl;
    }

    final path = attachmentUrl.startsWith('/') ? attachmentUrl : '/$attachmentUrl';
    final base = ApiEndpoint.baseUrl.endsWith('/')
        ? ApiEndpoint.baseUrl.substring(0, ApiEndpoint.baseUrl.length - 1)
        : ApiEndpoint.baseUrl;
    return '$base$path';
  }

  bool isMe(int currentUserId, String currentUsername) {
    if (currentUserId > 0 && senderId > 0) {
      return currentUserId == senderId;
    }
    if (currentUsername.isNotEmpty && senderUsername.isNotEmpty) {
      return currentUsername.toLowerCase() == senderUsername.toLowerCase();
    }
    return false;
  }

  factory ProjectDiscussionMessage.fromMap(String id, Map<dynamic, dynamic> data) {
    DateTime? created;
    final rawTimestamp = data['createdAt'];
    if (rawTimestamp is int) {
      created = DateTime.fromMillisecondsSinceEpoch(rawTimestamp);
    } else if (rawTimestamp is String) {
      created = DateTime.tryParse(rawTimestamp);
    } else if (rawTimestamp is DateTime) {
      created = rawTimestamp;
    }

    return ProjectDiscussionMessage(
      id: id,
      projectId: (data['projectId'] as num?)?.toInt() ?? 0,
      senderId: (data['senderId'] as num?)?.toInt() ?? 0,
      senderName: data['senderName'] as String? ?? 'Thành viên',
      senderUsername: data['senderUsername'] as String? ?? '',
      senderAvatar: data['senderAvatar'] as String? ?? '',
      content: data['content'] as String? ?? (data['text'] as String? ?? ''),
      createdAt: created,
      type: data['type'] as String? ?? 'text',
      attachmentUrl: data['attachmentUrl'] as String? ?? '',
      attachmentName: data['attachmentName'] as String? ?? '',
      attachmentSize: (data['attachmentSize'] as num?)?.toInt() ?? 0,
      attachmentSizeLabel: data['attachmentSizeLabel'] as String? ?? '',
    );
  }

  Map<String, dynamic> toMap() {
    return {
      'projectId': projectId,
      'senderId': senderId,
      'senderName': senderName,
      'senderUsername': senderUsername,
      'senderAvatar': senderAvatar,
      'content': content,
      'createdAt': createdAt?.millisecondsSinceEpoch ?? DateTime.now().millisecondsSinceEpoch,
      'type': type,
      'attachmentUrl': attachmentUrl,
      'attachmentName': attachmentName,
      'attachmentSize': attachmentSize,
      'attachmentSizeLabel': attachmentSizeLabel,
    };
  }
}

/// Model lựa chọn công việc (Task) trong dự án khi gõ '/'
class ProjectTaskOption {
  const ProjectTaskOption({
    required this.id,
    this.code,
    required this.title,
    this.state,
    this.assigneeName,
    this.priority,
  });

  final int id;
  final String? code;
  final String title;
  final String? state;
  final String? assigneeName;
  final String? priority;

  factory ProjectTaskOption.fromJson(Map<String, dynamic> json) {
    return ProjectTaskOption(
      id: (json['id'] as num?)?.toInt() ?? 0,
      code: json['code'] as String?,
      title: json['title'] as String? ?? 'Công việc',
      state: json['state'] as String?,
      assigneeName: json['assigneeName'] as String?,
      priority: json['priority'] as String?,
    );
  }
}
