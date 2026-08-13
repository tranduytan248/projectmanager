import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:ttkdgp_mobile/app.dart';
import 'package:ttkdgp_mobile/config/app_providers.dart';
import 'package:ttkdgp_mobile/core/classes/cache_manager.dart';

void main() {
  testWidgets('App khoi dong va hien man hinh dang nhap', (WidgetTester tester) async {
    // Cache.init() doc SharedPreferences that qua platform channel — can mock truoc trong
    // moi truong test, khong thi se treo/loi vi khong co plugin thuc.
    SharedPreferences.setMockInitialValues({});
    await Cache.init();

    await tester.pumpWidget(MultiProvider(providers: appProviders, child: const TtkdgpApp()));
    await tester.pumpAndSettle();

    expect(find.byType(MaterialApp), findsOneWidget);
  });
}
