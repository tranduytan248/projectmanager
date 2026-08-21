import 'package:flutter_test/flutter_test.dart';
import 'package:ttkdgp_mobile/features/team/private_tasks_models.dart';

void main() {
  group('PrivateTasksModels Tests', () {
    test('PrivateTaskItem parse from JSON correctly', () {
      final json = {
        'Id': 101,
        'Title': 'Soạn thảo quy trình kiểm thử',
        'Description': 'Tạo tài liệu kiểm thử chi tiết',
        'State': 'DangLam',
        'Priority': 'Cao',
        'Progress': 60,
        'AssigneeUserId': 12,
        'AssigneeName': 'Nguyễn Văn A',
        'AssignedByUserId': 1,
        'AssignedByName': 'Trần Duy Tân',
        'StartDate': '2026-08-20T00:00:00',
        'DueDate': '2026-08-25T00:00:00',
        'IsOverdue': false,
        'BonusPercent': 0.8,
        'HasAttachment': true,
        'AttachmentName': 'tailieu.pdf',
        'AttachmentSize': 20480,
        'CanEdit': true,
        'CanDelete': true,
        'CreatedAt': '2026-08-20T08:30:00',
      };

      final item = PrivateTaskItem.fromJson(json);

      expect(item.id, 101);
      expect(item.title, 'Soạn thảo quy trình kiểm thử');
      expect(item.state, 'DangLam');
      expect(item.stateLabel, 'Đang làm');
      expect(item.priority, 'Cao');
      expect(item.progress, 60);
      expect(item.assigneeName, 'Nguyễn Văn A');
      expect(item.assignedByName, 'Trần Duy Tân');
      expect(item.bonusPercent, 0.8);
      expect(item.hasAttachment, true);
      expect(item.canEdit, true);
      expect(item.canDelete, true);
    });

    test('PrivateTasksData parses items and members correctly', () {
      final json = {
        'TotalCount': 10,
        'OverdueCount': 2,
        'InProgressCount': 4,
        'DoneCount': 3,
        'Items': [
          {
            'Id': 1,
            'Title': 'Task 1',
            'State': 'HoanThanh',
            'Progress': 100,
          }
        ],
        'Members': [
          {'UserId': 1, 'FullName': 'Thành viên 1'},
          {'UserId': 2, 'FullName': 'Thành viên 2'},
        ]
      };

      final data = PrivateTasksData.fromJson(json);

      expect(data.totalCount, 10);
      expect(data.overdueCount, 2);
      expect(data.inProgressCount, 4);
      expect(data.doneCount, 3);
      expect(data.items.length, 1);
      expect(data.items.first.title, 'Task 1');
      expect(data.items.first.stateLabel, 'Hoàn thành');
      expect(data.members.length, 2);
      expect(data.members.first.fullName, 'Thành viên 1');
    });

    test('PrivateTaskFormOptions parses correctly', () {
      final json = {
        'Members': [
          {'UserId': 5, 'FullName': 'Lê Văn C'}
        ],
        'BonusOptions': [0.2, 0.5, 1.0, 1.5],
        'Priorities': ['Cao', 'TrungBinh', 'Thap']
      };

      final options = PrivateTaskFormOptions.fromJson(json);

      expect(options.members.length, 1);
      expect(options.bonusOptions, [0.2, 0.5, 1.0, 1.5]);
      expect(options.priorities, ['Cao', 'TrungBinh', 'Thap']);
    });
  });
}
