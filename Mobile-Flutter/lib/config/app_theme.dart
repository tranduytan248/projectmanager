import 'package:flutter/material.dart';

import '../core/theme/app_dimens.dart';

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

  /// = --success/--st-ok. Trang thai "hoan thanh/dung han" — KHONG dung brandBlue cho y nghia
  /// nay, theo dung quy uoc site.css (mau thuong hieu chi danh cho thao tac).
  static const statusSuccess = Color(0xFF1A7F52);
  static const statusSuccessSoft = Color(0xFFE6F5EE);

  /// = --warn/--st-warn. Trang thai "sap den han/canh bao".
  static const statusWarning = Color(0xFF9A6700);
  static const statusWarningSoft = Color(0xFFFDF5E2);

  /// = --danger/--st-late. Trang thai "qua han/loi".
  static const statusDanger = Color(0xFFB3261E);
  static const statusDangerSoft = Color(0xFFFBEAE9);

  /// = --text-muted. Mau chu phu (nhan, ghi chu) — dung thay Colors.black45/black54: hai mau do
  /// la den trong suot, tren nen trang doc mo/kho chiu; day la mot mau dac (khong trong suot)
  /// da duoc web dung san, tuong phan tot hon nhieu du cung "xam".
  static const textMuted = Color(0xFF667289);

  /// = --text-faint. Nhat hon textMuted mot chut, dung cho chu tren nen da mau (badge, chip).
  static const textFaint = Color(0xFF8B96A9);

  static const _seedColor = brandBlue;

  /// Font chu mac dinh toan app — khai bao trong pubspec.yaml (assets/fonts/BeVietnamPro-*.ttf).
  static const fontFamily = 'Be Vietnam Pro';

  // Vung cham toi thieu 48dp cho moi nut — Material3 mac dinh chi 40dp cho FilledButton/
  // OutlinedButton, duoi chuan AppDimens.minTapTarget. Khai bao 1 lan o day de moi nut trong app
  // (ke ca truoc khi kip doi het sang AppButton) deu tu dong dat chuan, khong phai sua tung noi.
  static const _minButtonSize = Size.fromHeight(AppDimens.minTapTarget);

  static ThemeData get light => ThemeData(
        useMaterial3: true,
        fontFamily: fontFamily,
        colorScheme: ColorScheme.fromSeed(seedColor: _seedColor),
        // Mac dinh Material3 lay mau "surface" tu seed (hoi ngar xanh), khong phai trang tuyet
        // doi — ep trang de khop dung voi cac the trong app deu dung Colors.white, va de status
        // bar trong suot (xem app_bottom_nav.dart) hoa lien mach vao nen thay vi lo mot vet mau.
        scaffoldBackgroundColor: Colors.white,
        // Flat design: khong do bong AppBar khi noi dung cuon qua (Material3 mac dinh
        // scrolledUnderElevation:3 tu ve mot dai bong/tint duoi AppBar — trai tinh than flat).
        appBarTheme: const AppBarTheme(
          centerTitle: true,
          backgroundColor: Colors.white,
          elevation: 0,
          scrolledUnderElevation: 0,
          surfaceTintColor: Colors.transparent,
        ),
        filledButtonTheme: FilledButtonThemeData(
          style: FilledButton.styleFrom(minimumSize: _minButtonSize, elevation: 0),
        ),
        outlinedButtonTheme: OutlinedButtonThemeData(
          style: OutlinedButton.styleFrom(minimumSize: _minButtonSize),
        ),
        iconButtonTheme: IconButtonThemeData(
          style: IconButton.styleFrom(minimumSize: const Size.square(AppDimens.minTapTarget)),
        ),
        bottomSheetTheme: const BottomSheetThemeData(
          elevation: 0,
          modalElevation: 0,
          surfaceTintColor: Colors.transparent,
        ),
      );

  static ThemeData get dark => ThemeData(
        useMaterial3: true,
        fontFamily: fontFamily,
        colorScheme: ColorScheme.fromSeed(
          seedColor: _seedColor,
          brightness: Brightness.dark,
        ),
        appBarTheme: const AppBarTheme(
          centerTitle: true,
          elevation: 0,
          scrolledUnderElevation: 0,
          surfaceTintColor: Colors.transparent,
        ),
        filledButtonTheme: FilledButtonThemeData(
          style: FilledButton.styleFrom(minimumSize: _minButtonSize, elevation: 0),
        ),
        outlinedButtonTheme: OutlinedButtonThemeData(
          style: OutlinedButton.styleFrom(minimumSize: _minButtonSize),
        ),
        iconButtonTheme: IconButtonThemeData(
          style: IconButton.styleFrom(minimumSize: const Size.square(AppDimens.minTapTarget)),
        ),
        bottomSheetTheme: const BottomSheetThemeData(
          elevation: 0,
          modalElevation: 0,
          surfaceTintColor: Colors.transparent,
        ),
      );
}
