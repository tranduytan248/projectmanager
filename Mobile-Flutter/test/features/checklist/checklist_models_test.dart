import 'package:flutter_test/flutter_test.dart';
import 'package:ttkdgp_mobile/features/checklist/checklist_models.dart';

void main() {
  group('ChecklistData.fromJson - Phân quyền tạo task', () {
    test('Happy Path: Nhận CanCreateTask = true từ server', () {
      final json = {
        'ProjectId': 42,
        'ProjectName': 'Dự án Alpha',
        'PmName': 'Trần Duy Tân',
        'CanEdit': false,
        'CanCreateTask': true,
        'TotalCount': 10,
        'DoneCount': 5,
        'OverdueCount': 1,
        'DonePercent': 50,
        'Tasks': <dynamic>[],
        'Assignees': <dynamic>[],
      };

      final data = ChecklistData.fromJson(json);

      expect(data.projectId, 42);
      expect(data.canEdit, false);
      expect(data.canCreateTask, true);
    });

    test('Edge Case: Server cũ không trả CanCreateTask -> Fallback về CanEdit', () {
      final json = {
        'ProjectId': 10,
        'ProjectName': 'Dự án Beta',
        'PmName': 'Nguyễn Văn A',
        'CanEdit': true,
        'TotalCount': 0,
        'DoneCount': 0,
        'OverdueCount': 0,
        'DonePercent': 0,
        'Tasks': <dynamic>[],
        'Assignees': <dynamic>[],
      };

      final data = ChecklistData.fromJson(json);

      expect(data.canEdit, true);
      expect(data.canCreateTask, true);
    });

    test('Edge Case: Cả CanCreateTask và CanEdit đều false', () {
      final json = {
        'ProjectId': 10,
        'ProjectName': 'Dự án Gamma',
        'CanEdit': false,
        'CanCreateTask': false,
      };

      final data = ChecklistData.fromJson(json);

      expect(data.canEdit, false);
      expect(data.canCreateTask, false);
    });
  });
}
