import 'package:flutter/material.dart';

import '../core/theme/app_dimens.dart';

class AppTheme {
  AppTheme._();

  /// = Visual Studio Code Accent (#007ACC).
  static const brandBlue = Color(0xFF007ACC);
  static const brandBlueDark = Color(0xFF0E639C);
  static const brandBlueDarker = Color(0xFF114A71);
  static const brandBlueSoft = Color(0xFF1B2B3E);

  /// Trạng thái Hoàn thành / Đúng hạn (#4EC9B0).
  static const statusSuccess = Color(0xFF4EC9B0);
  static const statusSuccessSoft = Color(0xFF17332B);

  /// Trạng thái Cảnh báo / Đang làm (#DCDCAA).
  static const statusWarning = Color(0xFFDCDCAA);
  static const statusWarningSoft = Color(0xFF33301B);

  /// Trạng thái Quá hạn / Lỗi (#F14C4C).
  static const statusDanger = Color(0xFFF14C4C);
  static const statusDangerSoft = Color(0xFF3A1E1E);

  /// Màu chữ phụ (#9DA5B4).
  static const textMuted = Color(0xFF9DA5B4);
  static const textFaint = Color(0xFF6A737D);

  /// Font chữ mặc định toàn app.
  static const fontFamily = 'Be Vietnam Pro';

  static const _minButtonSize = Size.fromHeight(AppDimens.minTapTarget);

  static ThemeData get light => dark; // Luôn áp dụng phong cách VS Code Dark Theme hiện đại

  static ThemeData get dark => ThemeData(
        useMaterial3: true,
        brightness: Brightness.dark,
        fontFamily: fontFamily,
        scaffoldBackgroundColor: const Color(0xFF1E1E1E),
        colorScheme: const ColorScheme.dark(
          primary: Color(0xFF007ACC),
          onPrimary: Colors.white,
          secondary: Color(0xFF0E639C),
          surface: Color(0xFF252526),
          onSurface: Color(0xFFD4D4D4),
          error: Color(0xFFF14C4C),
          onError: Colors.white,
        ),
        appBarTheme: const AppBarTheme(
          centerTitle: true,
          backgroundColor: Color(0xFF1E1E1E),
          elevation: 0,
          scrolledUnderElevation: 0,
          surfaceTintColor: Colors.transparent,
          titleTextStyle: TextStyle(
            fontFamily: fontFamily,
            color: Color(0xFFD4D4D4),
            fontSize: 17,
            fontWeight: FontWeight.w700,
          ),
          iconTheme: IconThemeData(color: Color(0xFFD4D4D4)),
        ),
        filledButtonTheme: FilledButtonThemeData(
          style: FilledButton.styleFrom(
            backgroundColor: const Color(0xFF007ACC),
            foregroundColor: Colors.white,
            minimumSize: _minButtonSize,
            elevation: 0,
          ),
        ),
        outlinedButtonTheme: OutlinedButtonThemeData(
          style: OutlinedButton.styleFrom(
            foregroundColor: const Color(0xFFD4D4D4),
            side: const BorderSide(color: Color(0xFF3C3C3C)),
            minimumSize: _minButtonSize,
          ),
        ),
        iconButtonTheme: IconButtonThemeData(
          style: IconButton.styleFrom(
            foregroundColor: const Color(0xFFD4D4D4),
            minimumSize: const Size.square(AppDimens.minTapTarget),
          ),
        ),
        bottomSheetTheme: const BottomSheetThemeData(
          backgroundColor: Color(0xFF252526),
          elevation: 0,
          modalElevation: 0,
          surfaceTintColor: Colors.transparent,
        ),
        dialogTheme: const DialogThemeData(
          backgroundColor: Color(0xFF252526),
          surfaceTintColor: Colors.transparent,
        ),
        dividerTheme: const DividerThemeData(
          color: Color(0xFF333333),
          thickness: 1,
          space: 1,
        ),
      );
}
