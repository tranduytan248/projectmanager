import 'package:flutter/material.dart';

class AppTheme {
  AppTheme._();

  /// Mau xanh thuong hieu — trung voi mau nen icon_app.png va splash nguyen sinh cua he dieu
  /// hanh (xem pubspec.yaml muc flutter_native_splash), de moi cho hien logo la mot the thong
  /// nhat: icon ngoai man hinh chinh, splash luc mo app, va man Dang nhap.
  static const brandBlue = Color(0xFF1A56A8);

  /// Ban sang hon cua brandBlue (cung tong mau, tang do sang ~L+12% theo HSL) — dung cho nut
  /// bam/nhan can noi bat tren nen brandBlue.
  static const brandBlueLight = Color(0xFF2271DD);

  static const _seedColor = brandBlueLight;

  static ThemeData get light => ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(seedColor: _seedColor),
        appBarTheme: const AppBarTheme(centerTitle: true),
      );

  static ThemeData get dark => ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: _seedColor,
          brightness: Brightness.dark,
        ),
        appBarTheme: const AppBarTheme(centerTitle: true),
      );
}
