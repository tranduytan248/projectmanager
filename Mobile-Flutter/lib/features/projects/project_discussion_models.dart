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
  });

  final String id;
  final int projectId;
  final int senderId;
  final String senderName;
  final String senderUsername;
  final String senderAvatar;
  final String content;
  final DateTime? createdAt;

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
      content: data['content'] as String? ?? '',
      createdAt: created,
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
    };
  }
}
