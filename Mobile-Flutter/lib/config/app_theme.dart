import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import '../core/theme/app_colors.dart';
import '../core/theme/app_dimens.dart';

class AppTheme {
  AppTheme._();

  /// Aliases chuyển tiếp sang AppColors để tương thích các nơi cũ
  static const brandBlue = AppColors.primary;
  static const brandBlueDark = AppColors.primaryDark;
  static const brandBlueDarker = AppColors.primaryDarker;
  static const brandBlueSoft = AppColors.primarySoft;

  static const statusSuccess = AppColors.success;
  static const statusSuccessSoft = AppColors.successSoft;

  static const statusWarning = AppColors.warning;
  static const statusWarningSoft = AppColors.warningSoft;

  static const statusDanger = AppColors.danger;
  static const statusDangerSoft = AppColors.dangerSoft;

  static const textMuted = AppColors.textSecondary;
  static const textFaint = AppColors.textFaint;

  static const _minButtonSize = Size.fromHeight(AppDimens.minTapTarget);

  static ThemeData get light => dark; // Luôn áp dụng phong cách VS Code Dark Theme hiện đại

  static ThemeData get dark => ThemeData(
        useMaterial3: true,
        brightness: Brightness.dark,
        fontFamily: GoogleFonts.roboto().fontFamily,
        textTheme: GoogleFonts.robotoTextTheme(ThemeData(brightness: Brightness.dark).textTheme),
        scaffoldBackgroundColor: AppColors.background,
        colorScheme: const ColorScheme.dark(
          primary: AppColors.primary,
          onPrimary: AppColors.textOnPrimary,
          secondary: AppColors.primaryDark,
          surface: AppColors.surface,
          onSurface: AppColors.textPrimary,
          error: AppColors.danger,
          onError: AppColors.textOnPrimary,
        ),
        appBarTheme: AppBarTheme(
          centerTitle: true,
          backgroundColor: AppColors.background,
          elevation: 0,
          scrolledUnderElevation: 0,
          surfaceTintColor: Colors.transparent,
          titleTextStyle: GoogleFonts.roboto(
            color: AppColors.textPrimary,
            fontSize: 17,
            fontWeight: FontWeight.w700,
          ),
          iconTheme: const IconThemeData(color: AppColors.textPrimary),
        ),
        filledButtonTheme: FilledButtonThemeData(
          style: FilledButton.styleFrom(
            backgroundColor: AppColors.primary,
            foregroundColor: AppColors.textOnPrimary,
            minimumSize: _minButtonSize,
            elevation: 0,
          ),
        ),
        outlinedButtonTheme: OutlinedButtonThemeData(
          style: OutlinedButton.styleFrom(
            foregroundColor: AppColors.textPrimary,
            side: const BorderSide(color: AppColors.borderStrong),
            minimumSize: _minButtonSize,
          ),
        ),
        iconButtonTheme: IconButtonThemeData(
          style: IconButton.styleFrom(
            foregroundColor: AppColors.textPrimary,
            minimumSize: const Size.square(AppDimens.minTapTarget),
          ),
        ),
        bottomSheetTheme: const BottomSheetThemeData(
          backgroundColor: AppColors.surface,
          elevation: 0,
          modalElevation: 0,
          surfaceTintColor: Colors.transparent,
        ),
        dialogTheme: const DialogThemeData(
          backgroundColor: AppColors.surface,
          surfaceTintColor: Colors.transparent,
        ),
        dividerTheme: const DividerThemeData(
          color: AppColors.border,
          thickness: 1,
          space: 1,
        ),
      );
}
