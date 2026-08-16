import '../dashboard/dashboard_models.dart' show parseAspNetDate;

/// Cac gia tri NotificationTypes.* ben backend (Models/Work/UserNotification.cs) — dung de chon
/// icon va noi den khi bam vao, KHOP Y HET voi UserNotificationsController.Open ben web.
class NotificationTypes {
  NotificationTypes._();

  static const projectAdded = 'VaoDuAn';
  static const projectRemoved = 'RoiDuAn';
  static const mentioned = 'DuocNhac';
  static const dueSoon = 'SapDenHan';
  static const taskAssigned = 'GiaoViecRieng';
  static const projectTaskAssigned = 'GiaoViecDuAn';
  static const commentAdded = 'TraoDoiMoi';
  static const todoToggled = 'ViecConThayDoi';
  static const todoAdded = 'ViecConMoi';
  static const leaveRequested = 'leave.request';
  static const leaveResult = 'leave.result';
}

class NotificationItem {
  const NotificationItem({
    required this.id,
    required this.type,
    required this.message,
    required this.projectId,
    required this.taskId,
    required this.isRead,
    required this.createdAt,
  });

  final int id;
  final String type;
  final String message;
  final int projectId;
  final int taskId;
  final bool isRead;
  final DateTime? createdAt;

  factory NotificationItem.fromJson(Map<String, dynamic> json) =>
      NotificationItem(
        id: json['Id'] as int,
        type: json['Type'] as String? ?? '',
        message: json['Message'] as String? ?? '',
        projectId: json['ProjectId'] as int? ?? 0,
        taskId: json['TaskId'] as int? ?? 0,
        isRead: json['IsRead'] as bool? ?? false,
        createdAt: parseAspNetDate(json['CreatedAt'] as String?),
      );

  NotificationItem markedRead() => NotificationItem(
        id: id,
        type: type,
        message: message,
        projectId: projectId,
        taskId: taskId,
        isRead: true,
        createdAt: createdAt,
      );
}

class NotificationPage {
  const NotificationPage({
    required this.items,
    required this.page,
    required this.hasMore,
    required this.unreadCount,
  });

  final List<NotificationItem> items;
  final int page;
  final bool hasMore;
  final int unreadCount;

  factory NotificationPage.fromJson(Map<String, dynamic> json) =>
      NotificationPage(
        items: (json['Items'] as List<dynamic>? ?? [])
            .map((e) => NotificationItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        page: json['Page'] as int? ?? 1,
        hasMore: json['HasMore'] as bool? ?? false,
        unreadCount: json['UnreadCount'] as int? ?? 0,
      );
}
