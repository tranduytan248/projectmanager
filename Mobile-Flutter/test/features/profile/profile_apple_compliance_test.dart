import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:ttkdgp_mobile/features/auth/auth_provider.dart';
import 'package:ttkdgp_mobile/core/classes/app_keys.dart';
import 'package:ttkdgp_mobile/core/classes/cache_manager.dart';
import 'package:ttkdgp_mobile/features/profile/policy_screen.dart';
import 'package:ttkdgp_mobile/features/profile/profile_screen.dart';

void main() {
  setUp(() async {
    TestWidgetsFlutterBinding.ensureInitialized();
    SharedPreferences.setMockInitialValues({});
    await Cache.init();
  });

  group('Kiểm tra tuân thủ chính sách Apple trên Profile & Policy', () {
    test('Chính sách bảo mật chứa mục Xóa tài khoản & Quyền xóa dữ liệu', () {
      final deletionSection = privacyPolicySections.firstWhere(
        (s) => s.title.contains('Xóa tài khoản'),
      );
      expect(deletionSection, isNotNull);
      expect(deletionSection.items.isNotEmpty, isTrue);
      expect(
        deletionSection.items.any((item) => item.contains('Yêu cầu xóa tài khoản')),
        isTrue,
      );
    });

    testWidgets('PolicyScreen hiển thị đầy đủ mục Xóa tài khoản', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: PolicyScreen(
            title: 'Chính sách bảo mật',
            sections: privacyPolicySections,
          ),
        ),
      );
      await tester.pumpAndSettle();

      expect(find.text('Chính sách bảo mật'), findsWidgets);
      final itemFinder = find.textContaining('Xóa tài khoản & Quyền xóa dữ liệu');
      await tester.scrollUntilVisible(itemFinder, 200);
      expect(itemFinder, findsOneWidget);
    });

    testWidgets('ProfileScreen hiển thị nút Yêu cầu xóa tài khoản', (tester) async {
      final authProvider = AuthProvider();

      await tester.pumpWidget(
        MultiProvider(
          providers: [
            ChangeNotifierProvider<AuthProvider>.value(value: authProvider),
          ],
          child: MaterialApp(
            navigatorKey: navigatorKey,
            home: const ProfileScreen(),
          ),
        ),
      );
      await tester.pumpAndSettle();

      // Cuộn để thấy nút Yêu cầu xóa tài khoản
      final deleteAccountTile = find.text('Yêu cầu xóa tài khoản');
      await tester.scrollUntilVisible(deleteAccountTile, 100);
      expect(deleteAccountTile, findsOneWidget);
    });
  });
}
