import 'package:firebase_analytics/firebase_analytics.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/widgets.dart';

/// Theo doi man hinh cho Android/iOS bang Firebase Analytics goc — doc cau hinh tu
/// android/app/google-services.json va ios/Runner/GoogleService-Info.plist, khoi tao mot lan o
/// main.dart. Vi routing ca app deu la named route (xem RouteManager), gan thang observer nay
/// vao MaterialApp la moi lan chuyen man hinh tu dong ghi mot su kien "screen_view", khong phai
/// tu goi tay o tung man.
///
/// Ban web hoac trong test environment KHONG dung observer nay vi chua goi Firebase.initializeApp().
class AppAnalytics {
  AppAnalytics._();

  static List<NavigatorObserver> get observers {
    if (kIsWeb || Firebase.apps.isEmpty) {
      return const [];
    }
    try {
      return [FirebaseAnalyticsObserver(analytics: FirebaseAnalytics.instance)];
    } catch (_) {
      return const [];
    }
  }
}
