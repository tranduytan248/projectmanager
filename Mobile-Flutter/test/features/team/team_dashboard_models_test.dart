import 'package:flutter_test/flutter_test.dart';
import 'package:ttkdgp_mobile/features/team/team_dashboard_models.dart';

void main() {
  group('TeamDashboard Models Test', () {
    test('TeamDashboardData parses PascalCase from C# API correctly', () {
      final json = {
        'Today': '2026-08-22T00:00:00',
        'Year': 2026,
        'Month': 8,
        'IsCurrentMonth': true,
        'TotalMembers': 4,
        'IdleCount': 1,
        'OverdueTodayCount': 0,
        'Members': [
          {
            'UserId': 10,
            'FullName': 'Châu Hoàng',
            'TodayTaskCount': 0,
            'OverdueTodayCount': 0,
            'TotalPenalty': 0.0,
            'TotalTasks': 44,
            'TodayTasks': [],
            'Kpi': {
              'FinalPoint': 22.0,
              'Rank': 'Chưa đạt',
              'WorkedHours': 72.0,
              'RequiredHours': 152.0,
              'PenaltyPoint': 0.0,
              'BonusPercent': 0.0,
            },
            'Implement': {'Projects': 1, 'Tasks': 44},
            'Support': {'Projects': 0, 'Tasks': 0},
          },
        ],
      };

      final data = TeamDashboardData.fromJson(json);
      expect(data.year, 2026);
      expect(data.month, 8);
      expect(data.isCurrentMonth, isTrue);
      expect(data.totalMembers, 4);
      expect(data.idleCount, 1);
      expect(data.members.length, 1);

      final m = data.members.first;
      expect(m.userId, 10);
      expect(m.fullName, 'Châu Hoàng');
      expect(m.todayTaskCount, 0);
      expect(m.kpi.finalPoint, 22.0);
      expect(m.kpi.rank, 'Chưa đạt');
      expect(m.implement.projects, 1);
      expect(m.implement.tasks, 44);
    });

    test('TeamMemberTasksResult parses project tasks correctly', () {
      final json = {
        'UserId': 2,
        'MemberName': 'Trần Thiên Long',
        'Year': 2026,
        'Month': 8,
        'Kind': 'Checklist',
        'KindLabel': 'Triển khai',
        'TotalProjects': 3,
        'TotalTasks': 4,
        'Tasks': [
          {
            'Id': 253,
            'Code': 'TSK-253',
            'Title': 'Xây dựng chức năng SimKit',
            'ProjectId': 34,
            'ProjectName': 'Biệt phái OneBSS',
            'State': 'DangLam',
            'Priority': 'BinhThuong',
            'Progress': 50,
            'IsOverdue': false,
            'LoggedHours': 16.0,
          },
        ],
      };

      final res = TeamMemberTasksResult.fromJson(json);
      expect(res.userId, 2);
      expect(res.memberName, 'Trần Thiên Long');
      expect(res.kindLabel, 'Triển khai');
      expect(res.totalProjects, 3);
      expect(res.totalTasks, 4);
      expect(res.tasks.length, 1);
      expect(res.tasks.first.code, 'TSK-253');
      expect(res.tasks.first.loggedHours, 16.0);
    });
  });
}
