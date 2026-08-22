import 'package:flutter_test/flutter_test.dart';
import 'package:ttkdgp_mobile/features/kpi/kpi_models.dart';

void main() {
  group('Kpi Models Test', () {
    test('KpiSummaryData parses PascalCase and camelCase correctly', () {
      final pascalJson = {
        'Total': 12,
        'AveragePoint': 24.8,
        'PassCount': 0,
        'PassPercent': 0,
        'FailCount': 12,
        'ShortHoursCount': 12,
        'TopName': 'Phan Lương Bằng',
        'TopPoint': 51.0,
        'StandardDays': 21,
        'ScaleMax': 100.0,
      };

      final summary = KpiSummaryData.fromJson(pascalJson);
      expect(summary.total, 12);
      expect(summary.averagePoint, 24.8);
      expect(summary.passCount, 0);
      expect(summary.failCount, 12);
      expect(summary.topName, 'Phan Lương Bằng');
      expect(summary.topPoint, 51.0);
      expect(summary.standardDays, 21);
      expect(summary.scaleMax, 100.0);
    });

    test('KpiMemberRow computes rankColor and short hours correctly', () {
      final rowJson = {
        'Id': 1,
        'UserId': 18,
        'FullName': 'Phan Lương Bằng',
        'Year': 2026,
        'Month': 8,
        'SupportPoint': 10.0,
        'SupportGrossPoint': 10.0,
        'SupportHours': 20.0,
        'SupportCapHours': 40.0,
        'ExecutePoint': 60.0,
        'ExecuteGrossPoint': 60.0,
        'ExecuteHours': 100.0,
        'ExecuteTargetHours': 100.0,
        'AssignedPoint': 5.0,
        'LatePenalty': 0.0,
        'QualityPoint': 75.0,
        'StandardDays': 21,
        'LeaveDays': 1.0,
        'RequiredHours': 160.0,
        'WorkedHours': 120.0,
        'AttendanceRate': 75,
        'FinalPoint': 56.0,
        'Rank': 'Đạt',
        'IsSaved': true,
        'RankIndex': 1,
      };

      final row = KpiMemberRow.fromJson(rowJson);
      expect(row.fullName, 'Phan Lương Bằng');
      expect(row.isSaved, isTrue);
      expect(row.rank, 'Đạt');
      expect(row.isShortHours, isTrue);
      expect(row.hoursShort, 40.0);
      expect(row.rankIndex, 1);
    });

    test('KpiIndexData parses full API payload resiliently', () {
      final indexJson = {
        'Year': 2026,
        'Month': 8,
        'IsCurrentMonth': true,
        'SelectedUserId': 0,
        'StandardDays': 21,
        'ScaleMax': 100.0,
        'CanGenerate': true,
        'CanConfig': true,
        'Summary': {
          'Total': 4,
          'AveragePoint': 50.0,
          'PassCount': 2,
          'PassPercent': 50,
          'FailCount': 2,
          'ShortHoursCount': 1,
          'TopName': 'Trần Duy Tân',
          'TopPoint': 85.0,
        },
        'Rows': [
          {
            'Id': 10,
            'UserId': 1,
            'FullName': 'Trần Duy Tân',
            'FinalPoint': 85.0,
            'Rank': 'Tốt',
            'AttendanceRate': 100,
            'RequiredHours': 168.0,
            'WorkedHours': 170.0,
          },
        ],
        'Users': [
          {'UserId': 1, 'FullName': 'Trần Duy Tân'},
        ],
      };

      final data = KpiIndexData.fromJson(indexJson);
      expect(data.year, 2026);
      expect(data.month, 8);
      expect(data.isCurrentMonth, isTrue);
      expect(data.canGenerate, isTrue);
      expect(data.summary.total, 4);
      expect(data.rows.length, 1);
      expect(data.rows.first.fullName, 'Trần Duy Tân');
      expect(data.users.length, 1);
    });

    test('KpiDetailData parses task breakdown and row correctly', () {
      final detailJson = {
        'CanGenerate': true,
        'CanConfig': false,
        'Row': {
          'UserId': 1,
          'FullName': 'Trần Duy Tân',
          'FinalPoint': 90.0,
          'Rank': 'Xuất sắc',
        },
        'SupportTasks': [
          {
            'Id': 101,
            'Title': 'Hỗ trợ khách hàng A',
            'ProjectId': 5,
            'ProjectName': 'Dự án Cổng TTĐT',
            'Kind': 'Support',
            'State': 'HoanThanh',
            'Progress': 100,
            'LoggedHours': 12.5,
            'IsOverdue': false,
          },
        ],
        'ExecuteTasks': [],
        'AssignedTasks': [],
      };

      final detail = KpiDetailData.fromJson(detailJson);
      expect(detail.row.fullName, 'Trần Duy Tân');
      expect(detail.row.rank, 'Xuất sắc');
      expect(detail.supportTasks.length, 1);
      expect(detail.supportTasks.first.title, 'Hỗ trợ khách hàng A');
      expect(detail.supportTasks.first.loggedHours, 12.5);
    });
  });
}
