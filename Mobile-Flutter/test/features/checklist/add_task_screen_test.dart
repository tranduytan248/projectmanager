import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:ttkdgp_mobile/core/widgets/app_text_field.dart';
import 'package:ttkdgp_mobile/features/checklist/add_task_screen.dart';
import 'package:ttkdgp_mobile/features/checklist/checklist_service.dart';

/// Test man "Thêm công việc mới" — tap trung vao quy tac so sanh NGAY giua "Ngay bat dau" va
/// "Han hoan thanh" trong add_task_screen.dart (validator cua truong Han hoan thanh + logic
/// mac dinh Ngay bat dau = hom nay khi khong chon).
///
/// Vi hai truong ngay duoc luu trong State rieng (_startDate/_dueDate, khong loi ra ngoai) va
/// man hinh theo kien truc App* (khong duoc dung DatePicker truc tiep trong test thay cho man
/// hinh), test o day lai dung THAT giao dien: mo showDatePicker qua AppTextField, chon ngay tren
/// lich, roi kich hoat Form.validate() y het luc nguoi dung bam "Thêm công việc" — khong sua bat
/// ky dong nao trong add_task_screen.dart.
void main() {
  // Thu tu AppTextField tren man hinh (khong co Key rieng nen phai lay theo vi tri): 0 = Ten
  // cong viec, 1 = Ma viec, 2 = Ngay bat dau, 3 = Han hoan thanh, 4 = Them viec can lam.
  const startDateFieldIndex = 2;
  const dueDateFieldIndex = 3;

  Future<void> pumpScreen(WidgetTester tester) async {
    await tester.pumpWidget(
      MaterialApp(
        home: AddTaskScreen(
          service: ChecklistService(),
          projectId: 1,
          assignees: const [],
        ),
      ),
    );
  }

  /// Mo lich cua truong ngay tai [fieldIndex], chuyen thang neu can roi bam dung ngay [target],
  /// cuoi cung bam OK de xac nhan — mo phong dung thao tac tay cua nguoi dung.
  Future<void> pickDate(
    WidgetTester tester, {
    required int fieldIndex,
    required DateTime target,
    required DateTime monthShownWhenOpened,
  }) async {
    await tester.tap(find.byType(AppTextField).at(fieldIndex));
    await tester.pumpAndSettle();

    final monthDiff =
        (target.year - monthShownWhenOpened.year) * 12 + (target.month - monthShownWhenOpened.month);
    if (monthDiff != 0) {
      final tooltip = monthDiff > 0 ? 'Next month' : 'Previous month';
      for (var i = 0; i < monthDiff.abs(); i++) {
        await tester.tap(find.byTooltip(tooltip));
        await tester.pumpAndSettle();
      }
    }

    await tester.tap(find.text('${target.day}'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('OK'));
    await tester.pumpAndSettle();
  }

  const errorText = 'Hạn hoàn thành phải sau ngày bắt đầu';

  testWidgets(
    'Han hoan thanh chon dung bang Ngay bat dau (ca hai qua lich) thi hop le',
    (tester) async {
      final now = DateTime.now();
      // Chon mot ngay tuong lai du xa de chac chan khac ngay hom nay, tranh truong hop ngay mo
      // lich da san "duoc chon" trung ngay muc tieu lam sai lech y nghia test.
      final sameDay = DateTime(now.year, now.month, now.day).add(const Duration(days: 20));

      await pumpScreen(tester);

      // Ngay bat dau: luc mo lich lan dau, thang hien thi la thang cua hom nay (initialDate =
      // _startDate mac dinh = DateTime.now()).
      await pickDate(
        tester,
        fieldIndex: startDateFieldIndex,
        target: sameDay,
        monthShownWhenOpened: DateTime(now.year, now.month),
      );

      // Han hoan thanh: sau khi Ngay bat dau da doi, lich Han hoan thanh mo len o thang cua
      // (Ngay bat dau + 7 ngay) — xem _pickDueDate trong add_task_screen.dart.
      final dueInitialShown = sameDay.add(const Duration(days: 7));
      await pickDate(
        tester,
        fieldIndex: dueDateFieldIndex,
        target: sameDay,
        monthShownWhenOpened: DateTime(dueInitialShown.year, dueInitialShown.month),
      );

      final formState = tester.state<FormState>(find.byType(Form));
      final isValid = formState.validate();
      await tester.pump();

      expect(isValid, isTrue,
          reason: 'Han hoan thanh bang dung Ngay bat dau phai duoc coi la hop le');
      expect(find.text(errorText), findsNothing);
    },
  );

  testWidgets(
    'Han hoan thanh cung ngay nhung khac gio voi Ngay bat dau (mac dinh la hom nay) thi khong bao loi',
    (tester) async {
      final now = DateTime.now();
      final today = DateTime(now.year, now.month, now.day);

      await pumpScreen(tester);
      // KHONG dung den truong Ngay bat dau — giu nguyen gia tri mac dinh DateTime.now() (co gio/
      // phut/giay thuc te), chi chon Han hoan thanh la ngay hom nay (lich luon tra ve dung 00:00).

      final dueInitialShown = today.add(const Duration(days: 7));
      await pickDate(
        tester,
        fieldIndex: dueDateFieldIndex,
        target: today,
        monthShownWhenOpened: DateTime(dueInitialShown.year, dueInitialShown.month),
      );

      final formState = tester.state<FormState>(find.byType(Form));
      final isValid = formState.validate();
      await tester.pump();

      expect(isValid, isTrue,
          reason: 'Han hoan thanh cung ngay voi Ngay bat dau (chi khac gio) khong duoc coi la loi');
      expect(find.text(errorText), findsNothing);
    },
  );

  testWidgets(
    'Han hoan thanh truoc Ngay bat dau 1 ngay thi bao loi',
    (tester) async {
      final now = DateTime.now();
      final today = DateTime(now.year, now.month, now.day);
      final dueDay = today.add(const Duration(days: 5));
      final startDay = dueDay.add(const Duration(days: 1));

      await pumpScreen(tester);

      // Chon Han hoan thanh TRUOC (luc nay Ngay bat dau van la mac dinh hom nay).
      final dueInitialShown = today.add(const Duration(days: 7));
      await pickDate(
        tester,
        fieldIndex: dueDateFieldIndex,
        target: dueDay,
        monthShownWhenOpened: DateTime(dueInitialShown.year, dueInitialShown.month),
      );

      // Sau do doi Ngay bat dau sang ngay hom sau Han hoan thanh — _pickStartDate khong chan
      // theo Han hoan thanh nen thao tac nay hop le tren giao dien, dung y luc validate se bat loi.
      await pickDate(
        tester,
        fieldIndex: startDateFieldIndex,
        target: startDay,
        monthShownWhenOpened: DateTime(today.year, today.month),
      );

      final formState = tester.state<FormState>(find.byType(Form));
      final isValid = formState.validate();
      await tester.pump();

      expect(isValid, isFalse,
          reason: 'Han hoan thanh truoc Ngay bat dau 1 ngay phai bi tu choi');
      expect(find.text(errorText), findsOneWidget);
    },
  );
}
