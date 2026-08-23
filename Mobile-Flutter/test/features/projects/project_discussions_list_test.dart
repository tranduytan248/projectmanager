import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:ttkdgp_mobile/core/classes/cache_manager.dart';
import 'package:ttkdgp_mobile/features/projects/my_projects_models.dart';
import 'package:ttkdgp_mobile/features/projects/project_discussions_list_screen.dart';

void main() {
  setUp(() async {
    TestWidgetsFlutterBinding.ensureInitialized();
    SharedPreferences.setMockInitialValues({});
    await Cache.init();
  });

  group('ProjectDiscussionsListScreen widget tests', () {
    testWidgets('renders search box and app bar correctly', (tester) async {
      final mockData = Future.value(
        const MyProjectsData(
          projects: [
            MyProjectRow(
              id: 1,
              code: 'DA-01',
              name: 'Dự án Nâng cấp Hệ thống',
              customer: 'VNPT',
              pmName: 'Trần Duy Tân',
              phase: 'Đang triển khai',
              state: 'Đang làm',
              isOpen: true,
              isPm: true,
              isActiveMember: true,
              role: 'PM',
              joinedAt: null,
              leftAt: null,
              checklistDone: 5,
              checklistTotal: 10,
              checklistPercent: 50,
              myOpenCount: 2,
              myOverdueCount: 0,
              reportStatus: 'ChuaNop',
            ),
          ],
          totalCount: 1,
          pmCount: 1,
          closedCount: 0,
        ),
      );

      await tester.pumpWidget(
        MaterialApp(
          home: ProjectDiscussionsListScreen(initialFuture: mockData),
        ),
      );
      await tester.pump();

      expect(find.text('Trao đổi Dự án'), findsOneWidget);
      expect(find.text('Chưa xem'), findsOneWidget);
      expect(find.text('Tất cả dự án'), findsOneWidget);
      expect(find.byType(TextField), findsOneWidget);

      // Chuyển sang tab Tất cả dự án để kiểm tra danh sách dự án
      await tester.tap(find.text('Tất cả dự án'));
      await tester.pumpAndSettle();

      expect(find.text('Dự án Nâng cấp Hệ thống'), findsOneWidget);
    });
  });
}
