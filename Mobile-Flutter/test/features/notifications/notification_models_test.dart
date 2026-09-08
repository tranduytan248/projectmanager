import 'package:flutter_test/flutter_test.dart';
import 'package:ttkdgp_mobile/features/notifications/notification_models.dart';

void main() {
  group('NotificationModels - Kiểm tra hằng số và parsing thông báo mới', () {
    test('Happy Path: NotificationTypes chứa đủ các loại thông báo công việc mới', () {
      expect(NotificationTypes.taskCompleted, 'HoanThanhCongViec');
      expect(NotificationTypes.taskStatusChanged, 'TrangThaiViecThayDoi');
      expect(NotificationTypes.taskAssigned, 'GiaoViecRieng');
      expect(NotificationTypes.projectTaskAssigned, 'GiaoViecDuAn');
      expect(NotificationTypes.commentAdded, 'TraoDoiMoi');
      expect(NotificationTypes.todoToggled, 'ViecConThayDoi');
    });

    test('Happy Path: Parse NotificationItem cho sự kiện Hoàn thành công việc', () {
      final json = {
        'Id': 101,
        'Type': 'HoanThanhCongViec',
        'Message': 'Nguyễn Văn B đã hoàn thành công việc "Thiết kế API".',
        'ProjectId': 5,
        'TaskId': 42,
        'IsRead': false,
        'CreatedAt': '/Date(1725792000000)/',
      };

      final item = NotificationItem.fromJson(json);

      expect(item.id, 101);
      expect(item.type, NotificationTypes.taskCompleted);
      expect(item.message, 'Nguyễn Văn B đã hoàn thành công việc "Thiết kế API".');
      expect(item.projectId, 5);
      expect(item.taskId, 42);
      expect(item.isRead, false);
      expect(item.createdAt, isNotNull);

      // Đánh dấu đã đọc
      final readItem = item.markedRead();
      expect(readItem.isRead, true);
      expect(readItem.id, 101);
      expect(readItem.taskId, 42);
    });

    test('Happy Path: Parse NotificationItem cho sự kiện Cập nhật trạng thái công việc', () {
      final json = {
        'Id': 102,
        'Type': 'TrangThaiViecThayDoi',
        'Message': 'Nguyễn Văn B đã đổi trạng thái công việc "Thiết kế API" sang "Đang làm" (60%).',
        'ProjectId': 5,
        'TaskId': 42,
        'IsRead': true,
        'CreatedAt': null,
      };

      final item = NotificationItem.fromJson(json);

      expect(item.id, 102);
      expect(item.type, NotificationTypes.taskStatusChanged);
      expect(item.message, contains('Đang làm'));
      expect(item.taskId, 42);
      expect(item.isRead, true);
      expect(item.createdAt, isNull);
    });

    test('Edge Case: Dữ liệu json rỗng / thiếu trường tự động gán giá trị mặc định an toàn', () {
      final json = <String, dynamic>{
        'Id': 999,
      };

      final item = NotificationItem.fromJson(json);

      expect(item.id, 999);
      expect(item.type, '');
      expect(item.message, '');
      expect(item.projectId, 0);
      expect(item.taskId, 0);
      expect(item.isRead, false);
      expect(item.createdAt, isNull);
    });
  });
}
