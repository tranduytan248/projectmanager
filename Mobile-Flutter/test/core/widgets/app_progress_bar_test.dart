import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:ttkdgp_mobile/core/widgets/app_progress_bar.dart';

void main() {
  group('AppProgressBar - Thanh tiến độ ngang chuẩn', () {
    testWidgets('Happy Path: Render đúng tiến độ và chiều cao chỉ định',
        (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: AppProgressBar(
              value: 0.65,
              height: 6.0,
            ),
          ),
        ),
      );

      expect(find.byType(AppProgressBar), findsOneWidget);
      final indicatorFinder = find.byType(LinearProgressIndicator);
      expect(indicatorFinder, findsOneWidget);

      final indicator = tester.widget<LinearProgressIndicator>(indicatorFinder);
      expect(indicator.value, equals(0.65));
      expect(indicator.minHeight, equals(6.0));
    });

    testWidgets('Edge Case: Tự động clamp giá trị vượt biên (âm hoặc > 1.0)',
        (WidgetTester tester) async {
      // Case 1: Âm
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: AppProgressBar(value: -0.2),
          ),
        ),
      );

      var indicator = tester.widget<LinearProgressIndicator>(
          find.byType(LinearProgressIndicator));
      expect(indicator.value, equals(0.0));

      // Case 2: > 1.0
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: AppProgressBar(value: 1.5),
          ),
        ),
      );

      indicator = tester.widget<LinearProgressIndicator>(
          find.byType(LinearProgressIndicator));
      expect(indicator.value, equals(1.0));
    });

    testWidgets('Tùy biến màu sắc và bo góc', (WidgetTester tester) async {
      const customColor = Colors.orange;
      const customBgColor = Colors.black12;

      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: AppProgressBar(
              value: 0.5,
              color: customColor,
              backgroundColor: customBgColor,
              borderRadius: 8.0,
            ),
          ),
        ),
      );

      final indicator = tester.widget<LinearProgressIndicator>(
          find.byType(LinearProgressIndicator));
      expect(indicator.backgroundColor, equals(customBgColor));
    });
  });
}
