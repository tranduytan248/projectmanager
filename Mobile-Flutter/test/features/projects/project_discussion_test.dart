import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:ttkdgp_mobile/core/classes/cache_manager.dart';
import 'package:ttkdgp_mobile/features/projects/project_detail_models.dart';
import 'package:ttkdgp_mobile/features/projects/project_discussion_models.dart';
import 'package:ttkdgp_mobile/features/projects/project_discussion_screen.dart';

void main() {
  setUp(() async {
    TestWidgetsFlutterBinding.ensureInitialized();
    SharedPreferences.setMockInitialValues({});
    await Cache.init();
  });

  group('ProjectDiscussionMessage model tests', () {
    test('isMe returns true when senderId matches currentUserId', () {
      final msg = ProjectDiscussionMessage(
        id: 'msg1',
        projectId: 10,
        senderId: 100,
        senderName: 'Trần Duy Tân',
        senderUsername: 'tantd.kha',
        senderAvatar: '',
        content: 'Tin nhắn thử nghiệm',
        createdAt: DateTime(2026, 8, 23, 10, 30),
      );

      expect(msg.isMe(100, 'tantd.kha'), isTrue);
      expect(msg.isMe(200, 'other.user'), isFalse);
    });

    test('isMe returns true when username matches case-insensitively', () {
      final msg = ProjectDiscussionMessage(
        id: 'msg2',
        projectId: 10,
        senderId: 0,
        senderName: 'Trần Duy Tân',
        senderUsername: 'tantd.kha',
        senderAvatar: '',
        content: 'Tin nhắn thử nghiệm',
        createdAt: DateTime(2026, 8, 23, 10, 30),
      );

      expect(msg.isMe(0, 'TANTD.KHA'), isTrue);
      expect(msg.isMe(0, 'other'), isFalse);
    });

    test('fromMap and toMap serialize and deserialize correctly', () {
      final map = {
        'projectId': 15,
        'senderId': 99,
        'senderName': 'Nguyễn Văn A',
        'senderUsername': 'anv',
        'senderAvatar': 'avatar.png',
        'content': 'Nội dung test',
        'createdAt': 1755938400000,
      };

      final msg = ProjectDiscussionMessage.fromMap('msg_test', map);
      expect(msg.id, equals('msg_test'));
      expect(msg.projectId, equals(15));
      expect(msg.senderId, equals(99));
      expect(msg.senderName, equals('Nguyễn Văn A'));
      expect(msg.content, equals('Nội dung test'));

      final outMap = msg.toMap();
      expect(outMap['projectId'], equals(15));
      expect(outMap['content'], equals('Nội dung test'));
    });
  });

  group('ProjectDiscussionScreen widget tests', () {
    testWidgets('renders empty state when no messages available',
        (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: ProjectDiscussionScreen(
            projectId: 10,
            projectName: 'Dự án Nâng cấp Hệ thống',
            members: [
              ProjectMember(
                id: 1,
                userFullName: 'Nguyễn Văn A',
                isPm: true,
                role: 'Quản trị',
                phase: 'Triển khai',
                joinedAt: null,
                leftAt: null,
                isActive: true,
                note: null,
              ),
            ],
          ),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.text('Trao đổi: Dự án Nâng cấp Hệ thống'), findsOneWidget);
      expect(find.text('Chưa có trao đổi nào'), findsOneWidget);
      expect(find.byType(TextField), findsOneWidget);
    });
  });
}
