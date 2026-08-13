import 'package:flutter/material.dart';

class AppTheme {
  AppTheme._();

  // Cung bo token --primary/--primary-dark/--primary-darker/--primary-soft dinh nghia trong
  // TTKDGP.ProjectManager/Content/site.css — mobile va web dung CHUNG mot he mau thuong hieu,
  // sua o site.css thi nho sua lai o day cho khop (chua co co che dung file .css chung).
  //
  // Quy uoc cua site.css (giu nguyen ben mobile): --primary CHI danh cho thao tac (nut, lien
  // ket, muc dang chon) — KHONG dung cho trang thai du lieu (xong han/qua han/canh bao dung
  // mau ngu nghia rieng, xem them ghi chu trong site.css).

  /// = --primary. Trung voi mau nen icon_app.png va splash nguyen sinh cua he dieu hanh (xem
  /// pubspec.yaml muc flutter_native_splash), de moi cho hien logo la mot the thong nhat.
  static const brandBlue = Color(0xFF1A56A8);

  /// = --primary-dark.
  static const brandBlueDark = Color(0xFF143F7D);

  /// = --primary-darker.
  static const brandBlueDarker = Color(0xFF10386F);

  /// = --primary-soft. Nen nhat cho khoi/badge lien quan toi thao tac chinh.
  static const brandBlueSoft = Color(0xFFEAF1FB);

  static const _seedColor = brandBlue;

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
