import 'package:flutter_test/flutter_test.dart';
import 'package:ttkdgp_mobile/features/leaves/leave_models.dart';

void main() {
  group('LeaveApproval Models Test', () {
    test('LeaveMemberOption parses JSON correctly', () {
      final json = {'UserId': 10, 'FullName': 'Nguyễn Văn A'};
      final member = LeaveMemberOption.fromJson(json);
      expect(member.userId, 10);
      expect(member.fullName, 'Nguyễn Văn A');
    });

    test('LeaveApprovalsData parses JSON correctly', () {
      final json = {
        'TotalCount': 5,
        'PendingCount': 2,
        'ApprovedCount': 2,
        'RejectedCount': 1,
        'Years': [2026, 2025],
        'Members': [
          {'UserId': 1, 'FullName': 'Lê Đình Minh Trí'},
          {'UserId': 2, 'FullName': 'Trần Duy Tân'},
        ],
        'Items': [
          {
            'Id': 101,
            'UserId': 1,
            'UserFullName': 'Lê Đình Minh Trí',
            'Kind': 'PhepNam',
            'FromDate': '/Date(1787328000000)/',
            'ToDate': '/Date(1787414400000)/',
            'IsHalfDay': false,
            'Days': 2.0,
            'Reason': 'Việc gia đình',
            'State': 'ChoDuyet',
            'CreatedAt': '/Date(1787328000000)/',
          },
        ],
      };

      final data = LeaveApprovalsData.fromJson(json);
      expect(data.totalCount, 5);
      expect(data.pendingCount, 2);
      expect(data.approvedCount, 2);
      expect(data.rejectedCount, 1);
      expect(data.years.length, 2);
      expect(data.members.length, 2);
      expect(data.items.length, 1);
      expect(data.items.first.userFullName, 'Lê Đình Minh Trí');
      expect(data.items.first.state, LeaveStates.pending);
    });
  });
}
