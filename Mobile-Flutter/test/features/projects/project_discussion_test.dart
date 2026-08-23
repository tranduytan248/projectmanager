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

    test('Image and Video attachment detection and properties', () {
      // 1. Image message
      final imgMsg = ProjectDiscussionMessage(
        id: 'msg_img',
        projectId: 10,
        senderId: 1,
        senderName: 'Tân',
        senderUsername: 'tan',
        senderAvatar: '',
        content: 'Ảnh chụp',
        createdAt: DateTime.now(),
        type: 'image',
        attachmentUrl: '/Discussions/Attachment?id=123&name=photo.png',
        attachmentName: 'photo.png',
        attachmentSize: 1024 * 500,
        attachmentSizeLabel: '500 KB',
      );
      expect(imgMsg.hasAttachment, isTrue);
      expect(imgMsg.isImage, isTrue);
      expect(imgMsg.isVideo, isFalse);
      expect(imgMsg.resolvedAttachmentUrl, contains('/Discussions/Attachment?id=123'));
      expect(imgMsg.resolvedAttachmentUrl.startsWith('http://'), isTrue);

      // 2. Video message
      final vidMsg = ProjectDiscussionMessage(
        id: 'msg_vid',
        projectId: 10,
        senderId: 1,
        senderName: 'Tân',
        senderUsername: 'tan',
        senderAvatar: '',
        content: 'Video demo',
        createdAt: DateTime.now(),
        type: 'video',
        attachmentUrl: '/Discussions/Attachment?id=456&name=demo.mp4',
        attachmentName: 'demo.mp4',
        attachmentSize: 1024 * 1024 * 5,
        attachmentSizeLabel: '5.0 MB',
      );
      expect(vidMsg.hasAttachment, isTrue);
      expect(vidMsg.isImage, isFalse);
      expect(vidMsg.isVideo, isTrue);

      // 3. Normal text message without attachment
      final textMsg = ProjectDiscussionMessage(
        id: 'msg_txt',
        projectId: 10,
        senderId: 1,
        senderName: 'Tân',
        senderUsername: 'tan',
        senderAvatar: '',
        content: 'Chỉ có chữ',
        createdAt: DateTime.now(),
      );
      expect(textMsg.hasAttachment, isFalse);
      expect(textMsg.isImage, isFalse);
      expect(textMsg.isVideo, isFalse);
    });

    test('fromMap and toMap serialize and deserialize correctly with media fields', () {
      final map = {
        'projectId': 15,
        'senderId': 99,
        'senderName': 'Nguyễn Văn A',
        'senderUsername': 'anv',
        'senderAvatar': 'avatar.png',
        'content': 'Nội dung test',
        'createdAt': 1755938400000,
        'type': 'image',
        'attachmentUrl': 'https://example.com/test.jpg',
        'attachmentName': 'test.jpg',
        'attachmentSize': 204800,
        'attachmentSizeLabel': '200 KB',
      };

      final msg = ProjectDiscussionMessage.fromMap('msg_test', map);
      expect(msg.id, equals('msg_test'));
      expect(msg.projectId, equals(15));
      expect(msg.senderId, equals(99));
      expect(msg.senderName, equals('Nguyễn Văn A'));
      expect(msg.content, equals('Nội dung test'));
      expect(msg.type, equals('image'));
      expect(msg.attachmentUrl, equals('https://example.com/test.jpg'));
      expect(msg.attachmentName, equals('test.jpg'));
      expect(msg.attachmentSize, equals(204800));
      expect(msg.attachmentSizeLabel, equals('200 KB'));

      final outMap = msg.toMap();
      expect(outMap['projectId'], equals(15));
      expect(outMap['content'], equals('Nội dung test'));
      expect(outMap['type'], equals('image'));
      expect(outMap['attachmentUrl'], equals('https://example.com/test.jpg'));
    });
  });

  group('ProjectTaskOption model tests', () {
    test('fromJson parses full data and handles null/edge cases', () {
      // Happy path
      final json = {
        'id': 101,
        'code': 'TASK-01',
        'title': 'Thiết kế giao diện',
        'state': 'Đang thực hiện',
        'assigneeName': 'Trần Duy Tân',
        'priority': 'Cao',
      };
      final task = ProjectTaskOption.fromJson(json);
      expect(task.id, equals(101));
      expect(task.code, equals('TASK-01'));
      expect(task.title, equals('Thiết kế giao diện'));
      expect(task.state, equals('Đang thực hiện'));
      expect(task.assigneeName, equals('Trần Duy Tân'));
      expect(task.priority, equals('Cao'));

      // Edge case: null fields
      final nullJson = <String, dynamic>{
        'id': null,
        'code': null,
        'title': null,
        'state': null,
      };
      final fallbackTask = ProjectTaskOption.fromJson(nullJson);
      expect(fallbackTask.id, equals(0));
      expect(fallbackTask.title, equals('Công việc'));
      expect(fallbackTask.state, isNull);
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
    });
  });
}
