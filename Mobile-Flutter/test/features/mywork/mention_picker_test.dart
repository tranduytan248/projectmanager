import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:ttkdgp_mobile/core/widgets/app_rich_editor.dart';
import 'package:ttkdgp_mobile/features/mywork/mention_picker.dart';
import 'package:ttkdgp_mobile/features/mywork/task_detail_models.dart';

void main() {
  group('detectMention', () {
    test('"@" o dau chuoi duoc nhan dang', () {
      final match = detectMention('@long', 5);

      expect(match, isNotNull);
      expect(match!.start, 0);
      expect(match.end, 5);
      expect(match.query, 'long');
    });

    test('"@" ngay sau khoang trang duoc nhan dang', () {
      final match = detectMention('chao @long', 10);

      expect(match, isNotNull);
      expect(match!.query, 'long');
    });

    test('"@" ngay sau dau cau duoc nhan dang', () {
      final match = detectMention('xin chao, @long', 15);

      expect(match, isNotNull);
      expect(match!.query, 'long');
    });

    test('"@" nam giua mot email (khong co khoang trang truoc) khong duoc nhan dang', () {
      final match = detectMention('user@domain', 11);

      expect(match, isNull);
    });

    test('query dai qua 40 ky tu thi tra ve null', () {
      final query = 'a' * 41;
      final text = '@$query';

      final match = detectMention(text, text.length);

      expect(match, isNull);
    });

    test('query dai dung 40 ky tu van duoc nhan dang', () {
      final query = 'a' * 40;
      final text = '@$query';

      final match = detectMention(text, text.length);

      expect(match, isNotNull);
      expect(match!.query, query);
    });

    test('query chua ky tu xuong dong thi tra ve null', () {
      final match = detectMention('@long\nnguyen', 12);

      expect(match, isNull);
    });

    test('khong co "@" nao truoc con tro thi tra ve null', () {
      final match = detectMention('khong co ky tu bat dau', 10);

      expect(match, isNull);
    });
  });

  group('stripDiacritics', () {
    test('bo dau cac nhom nguyen am va chu d dac biet', () {
      expect(stripDiacritics('Trần Thiên Long'), 'tran thien long');
      expect(stripDiacritics('Đặng Ánh Dương'), 'dang anh duong');
      expect(stripDiacritics('Nguyễn Thị Uyên'), 'nguyen thi uyen');
      expect(stripDiacritics('Ế Ề Ể Ễ Ệ'), 'e e e e e');
      expect(stripDiacritics('Ý Ỹ Ỷ Ỵ'), 'y y y y');
    });
  });

  group('filterMentionParticipants', () {
    final participants = [
      const TaskMentionOption(userId: 1, fullName: 'Trần Thiên Long', userName: 'longtt'),
      const TaskMentionOption(userId: 2, fullName: 'Nguyễn Văn An', userName: 'annv'),
      const TaskMentionOption(userId: 3, fullName: 'Lê Thị Hồng', userName: 'hongle'),
    ];

    test('khop theo ten day du bat dau bang query (da bo dau)', () {
      final result = filterMentionParticipants(participants, 'tran');

      expect(result.map((e) => e.fullName), ['Trần Thiên Long']);
    });

    test('khop theo username', () {
      final result = filterMentionParticipants(participants, 'annv');

      expect(result.map((e) => e.fullName), ['Nguyễn Văn An']);
    });

    test('khop theo tung tu trong ten (khong chi tu dau)', () {
      final result = filterMentionParticipants(participants, 'long');

      expect(result.map((e) => e.fullName), ['Trần Thiên Long']);
    });

    test('query rong thi tra ve tat ca nguoi tham gia', () {
      final result = filterMentionParticipants(participants, '');

      expect(result.length, participants.length);
    });
  });

  group('insertMention', () {
    test('chen dung "@Ho Ten " tai vi tri query, con tro dat sau khoang trang vua chen', () {
      final controller = AppRichEditorController(text: '@lo xin chao');
      // MentionMatch tuong ung doan "@lo" tu vi tri 0 den 3 (query = "lo").
      const match = MentionMatch(start: 0, end: 3, query: 'lo');
      const option = TaskMentionOption(userId: 1, fullName: 'Trần Thiên Long', userName: 'longtt');

      insertMention(controller, match, option);

      expect(controller.text, '@Trần Thiên Long  xin chao');
      final expectedOffset = '@Trần Thiên Long '.length;
      expect(controller.selection, TextSelection.collapsed(offset: expectedOffset));
    });

    test('chen dung khi "@query" nam o cuoi chuoi', () {
      final controller = AppRichEditorController(text: 'giao viec cho @an');
      const match = MentionMatch(start: 14, end: 17, query: 'an');
      const option = TaskMentionOption(userId: 2, fullName: 'Nguyễn Văn An', userName: 'annv');

      insertMention(controller, match, option);

      expect(controller.text, 'giao viec cho @Nguyễn Văn An ');
      expect(controller.selection, TextSelection.collapsed(offset: controller.text.length));
    });
  });
}
