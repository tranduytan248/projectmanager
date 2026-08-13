import 'package:firebase_analytics/firebase_analytics.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/widgets.dart';

/// Theo doi man hinh cho Android/iOS bang Firebase Analytics goc — doc cau hinh tu
/// android/app/google-services.json va ios/Runner/GoogleService-Info.plist, khoi tao mot lan o
/// main.dart. Vi routing ca app deu la named route (xem RouteManager), gan thang observer nay
/// vao MaterialApp la moi lan chuyen man hinh tu dong ghi mot su kien "screen_view", khong phai
/// tu goi tay o tung man.
///
/// Ban web KHONG dung observer nay: Firebase.initializeApp() bi bo qua tren web (xem main.dart)
/// vi ban web da co gtag.js rieng trong web/index.html — goi FirebaseAnalytics.instance khi
/// chua initializeApp se nem loi.
class AppAnalytics {
  AppAnalytics._();

  static final List<NavigatorObserver> observers = kIsWeb
      ? const []
      : [FirebaseAnalyticsObserver(analytics: FirebaseAnalytics.instance)];
}
