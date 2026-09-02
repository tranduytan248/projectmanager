import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:ttkdgp_mobile/core/widgets/app_progress_circle.dart';
import 'package:ttkdgp_mobile/core/widgets/app_text.dart';

void main() {
  group('AppProgressCircle - Vòng tròn tiến độ chuẩn', () {
    testWidgets('Happy Path: Render đúng tiến độ và hiển thị child text ở tâm',
        (WidgetTester tester) async {
      // Arrange & Act
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: AppProgressCircle(
              value: 0.75,
              size: 60,
              child: AppText('75%'),
            ),
          ),
        ),
      );

      // Assert
      expect(find.byType(AppProgressCircle), findsOneWidget);
      expect(find.text('75%'), findsOneWidget);

      final progressFinder = find.byType(CircularProgressIndicator);
      expect(progressFinder, findsOneWidget);

      final progressWidget =
          tester.widget<CircularProgressIndicator>(progressFinder);
      expect(progressWidget.value, equals(0.75));
    });

    testWidgets('Edge Case: Clamp giá trị vượt ngưỡng (âm hoặc lớn hơn 1.0)',
        (WidgetTester tester) async {
      // Case 1: Giá trị âm
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: AppProgressCircle(
              value: -0.5,
              size: 50,
            ),
          ),
        ),
      );

      var progressWidget = tester
          .widget<CircularProgressIndicator>(find.byType(CircularProgressIndicator));
      expect(progressWidget.value, equals(0.0));

      // Case 2: Giá trị lớn hơn 1.0
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: AppProgressCircle(
              value: 1.8,
              size: 50,
            ),
          ),
        ),
      );

      progressWidget = tester
          .widget<CircularProgressIndicator>(find.byType(CircularProgressIndicator));
      expect(progressWidget.value, equals(1.0));
    });

    testWidgets('Tùy biến màu sắc và độ dày nét vẽ (strokeWidth)',
        (WidgetTester tester) async {
      const customColor = Colors.green;
      const customBgColor = Colors.grey;

      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: AppProgressCircle(
              value: 0.5,
              strokeWidth: 8.0,
              color: customColor,
              backgroundColor: customBgColor,
            ),
          ),
        ),
      );

      final progressWidget = tester
          .widget<CircularProgressIndicator>(find.byType(CircularProgressIndicator));
      expect(progressWidget.strokeWidth, equals(8.0));
      expect(progressWidget.backgroundColor, equals(customBgColor));
    });
  });
}
