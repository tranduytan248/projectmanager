import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
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
      builder: (context, child) {
        return GestureDetector(
          behavior: HitTestBehavior.translucent,
          onTap: () => FocusManager.instance.primaryFocus?.unfocus(),
          child: child ?? const SizedBox.shrink(),
        );
      },
      // Dich cac widget he thong (date picker, time picker...) sang tieng Viet — man hinh tu viet
      // vẫn tự dịch qua AppStrings/AppText nhu binh thuong, day chi anh huong widget cua Flutter.
      localizationsDelegates: const [
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      supportedLocales: const [Locale('vi', 'VN')],
      locale: const Locale('vi', 'VN'),
      initialRoute: AuthRoutes.splash,
      routes: Routes().routes,
      navigatorObservers: AppAnalytics.observers,
    );
  }
}
