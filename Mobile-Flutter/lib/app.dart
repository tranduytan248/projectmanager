import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'config/app_analytics.dart';
import 'config/app_routes.dart';
import 'config/app_theme.dart';
import 'config/theme_provider.dart';
import 'core/classes/app_keys.dart';
import 'features/auth/auth_routes.dart';

class TtkdgpApp extends StatelessWidget {
  const TtkdgpApp({super.key});

  @override
  Widget build(BuildContext context) {
    final themeMode = context.watch<ThemeProvider>().themeMode;

    return MaterialApp(
      navigatorKey: navigatorKey,
      scaffoldMessengerKey: scaffoldMessengerKey,
      title: 'BrewTask - Quản Lý Công Việc',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      darkTheme: AppTheme.dark,
      themeMode: themeMode,
      initialRoute: AuthRoutes.splash,
      routes: Routes().routes,
      navigatorObservers: AppAnalytics.observers,
    );
  }
}
