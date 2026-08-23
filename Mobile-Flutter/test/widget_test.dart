import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:ttkdgp_mobile/app.dart';
import 'package:ttkdgp_mobile/config/app_providers.dart';
import 'package:ttkdgp_mobile/core/classes/cache_manager.dart';

void main() {
  testWidgets('App khoi dong va hien man hinh', (WidgetTester tester) async {
    SharedPreferences.setMockInitialValues({});
    FlutterSecureStorage.setMockInitialValues({});
    await Cache.init();

    await tester.pumpWidget(MultiProvider(providers: appProviders, child: const TtkdgpApp()));
    await tester.pump(const Duration(seconds: 4));

    expect(find.byType(MaterialApp), findsOneWidget);
  });
}
