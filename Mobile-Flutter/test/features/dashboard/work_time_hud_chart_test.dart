import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:ttkdgp_mobile/core/theme/app_colors.dart';
import 'package:ttkdgp_mobile/features/dashboard/dashboard_models.dart';
import 'package:ttkdgp_mobile/features/dashboard/widgets/work_time_hud_chart.dart';

void main() {
  group('WorkTimeHUDChart Unit & Widget Tests', () {
    test('getHourColor returns correct colors for all ranges', () {
      // >= 8h -> Xanh la (AppColors.success)
      expect(WorkTimeHUDChart.getHourColor(12.0), AppColors.success);
      expect(WorkTimeHUDChart.getHourColor(8.5), AppColors.success);
      expect(WorkTimeHUDChart.getHourColor(8.0), AppColors.success);

      // 6h - <8h -> Xanh duong (AppColors.info)
      expect(WorkTimeHUDChart.getHourColor(7.9), AppColors.info);
      expect(WorkTimeHUDChart.getHourColor(6.5), AppColors.info);
      expect(WorkTimeHUDChart.getHourColor(6.0), AppColors.info);

      // 4h - <6h -> Mau cam
      const orange = Color(0xFFFB923C);
      expect(WorkTimeHUDChart.getHourColor(5.9), orange);
      expect(WorkTimeHUDChart.getHourColor(4.5), orange);
      expect(WorkTimeHUDChart.getHourColor(4.0), orange);

      // 0h - <4h -> Mau do (AppColors.danger)
      expect(WorkTimeHUDChart.getHourColor(3.9), AppColors.danger);
      expect(WorkTimeHUDChart.getHourColor(1.0), AppColors.danger);
      expect(WorkTimeHUDChart.getHourColor(0.5), AppColors.danger);

      // 0h -> Border/xam
      expect(WorkTimeHUDChart.getHourColor(0.0), AppColors.border);
    });

    test('WorkTimeDashboard and DailyLogTime parse from Json correctly', () {
      final json = {
        'TotalHoursWeek': 38.5,
        'TotalHoursMonth': 152.0,
        'TodayHours': 8.0,
        'TargetHoursPerDay': 8.0,
        'MaxHoursPerDay': 12.0,
        'DailyLogs': [
          {'DayOfWeek': 'T2', 'Hours': 8.5, 'IsToday': false},
          {'DayOfWeek': 'T3', 'Hours': 7.0, 'IsToday': false},
          {'DayOfWeek': 'T4', 'Hours': 5.0, 'IsToday': false},
          {'DayOfWeek': 'T5', 'Hours': 3.0, 'IsToday': false},
          {'DayOfWeek': 'T6', 'Hours': 8.0, 'IsToday': true},
          {'DayOfWeek': 'T7', 'Hours': 0.0, 'IsToday': false},
          {'DayOfWeek': 'CN', 'Hours': 0.0, 'IsToday': false},
        ]
      };

      final workTime = WorkTimeDashboard.fromJson(json);
      expect(workTime.totalHoursWeek, 38.5);
      expect(workTime.totalHoursMonth, 152.0);
      expect(workTime.todayHours, 8.0);
      expect(workTime.dailyLogs.length, 7);
      expect(workTime.dailyLogs[0].dayOfWeek, 'T2');
      expect(workTime.dailyLogs[0].hours, 8.5);
      expect(workTime.dailyLogs[4].isToday, isTrue);
    });

    testWidgets('Renders WorkTimeHUDChart with full elements and metrics',
        (tester) async {
      const workTime = WorkTimeDashboard(
        totalHoursWeek: 36.5,
        totalHoursMonth: 140.0,
        todayHours: 8.5,
        targetHoursPerDay: 8.0,
        maxHoursPerDay: 12.0,
        dailyLogs: [
          DailyLogTime(date: null, dayOfWeek: 'T2', hours: 8.5, isToday: false),
          DailyLogTime(date: null, dayOfWeek: 'T3', hours: 7.0, isToday: false),
          DailyLogTime(date: null, dayOfWeek: 'T4', hours: 5.5, isToday: false),
          DailyLogTime(date: null, dayOfWeek: 'T5', hours: 3.5, isToday: false),
          DailyLogTime(date: null, dayOfWeek: 'T6', hours: 8.5, isToday: true),
          DailyLogTime(date: null, dayOfWeek: 'T7', hours: 0.0, isToday: false),
          DailyLogTime(date: null, dayOfWeek: 'CN', hours: 0.0, isToday: false),
        ],
      );

      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: SingleChildScrollView(
              child: WorkTimeHUDChart(workTime: workTime),
            ),
          ),
        ),
      );

      // Verify Header and Badges
      expect(find.text('THỜI GIAN LÀM VIỆC & LOGTIME'), findsOneWidget);
      expect(find.text('Hôm nay: 8.5h'), findsOneWidget);

      // Verify Weekday Labels
      expect(find.text('T2'), findsOneWidget);
      expect(find.text('T3'), findsOneWidget);
      expect(find.text('T4'), findsOneWidget);
      expect(find.text('T5'), findsOneWidget);
      expect(find.text('T6'), findsOneWidget);
      expect(find.text('T7'), findsOneWidget);
      expect(find.text('CN'), findsOneWidget);

      // Verify Metric Tiles
      expect(find.text('Hôm nay'), findsOneWidget);
      expect(find.text('Tuần này'), findsOneWidget);
      expect(find.text('Tháng này'), findsOneWidget);
      expect(find.text('36.5h'), findsOneWidget);
      expect(find.text('140.0h'), findsOneWidget);

      // Verify Legend
      expect(find.text('≥ 8h'), findsOneWidget);
      expect(find.text('6 - 8h'), findsOneWidget);
      expect(find.text('4 - 6h'), findsOneWidget);
      expect(find.text('< 4h'), findsOneWidget);
    });
  });
}
