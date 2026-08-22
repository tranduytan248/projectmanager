import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:hive_flutter/hive_flutter.dart';
import 'package:provider/provider.dart';

import 'app.dart';
import 'config/app_providers.dart';
import 'core/classes/cache_manager.dart';
import 'core/services/fcm_notification_service.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Hive.initFlutter();
  await Cache.init();

  // Web da tu khoi tao Firebase Analytics rieng bang gtag.js trong web/index.html (khong co
  // file cau hinh nhu google-services.json de doc tren nen web) — chi goi o day cho Android/iOS.
  if (!kIsWeb) {
    await Firebase.initializeApp();
    FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);
    await FcmNotificationService.instance.initialize();
  }

  runApp(MultiProvider(providers: appProviders, child: const TtkdgpApp()));
}
