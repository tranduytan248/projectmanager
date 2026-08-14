import 'package:flutter/material.dart';

import 'app_colors.dart';

/// Cac muc dich hien thi chu — dung `.claude/rules/FLUTTER_RULES.md` ("thang co chu ro rang").
/// AppText chon TextStyle theo variant nay, khong de tung man hinh tu dat fontSize/fontWeight.
enum AppTextVariant {
  /// 20-22 — tieu de man hinh / con so noi bat (vi du "Ban co N du an").
  display,

  /// 17-19 — tieu de man hinh nho hon / ten thuc the chinh (ten du an, ten cong viec).
  title,

  /// 15-16 — tieu de mot khoi/muc trong man hinh (vi du "Thong tin chung").
  heading,

  /// 14 — noi dung chinh, gia tri du lieu.
  body,

  /// 12-13 — chu thich, nhan phu, ngay thang, mo ta ngan.
  caption,

  /// 10-11, letter-spacing rong — nhan cuc nho (badge, tag trang thai).
  overline,
}

/// Dinh nghia TextStyle cho tung AppTextVariant. Mau mac dinh lay theo AppColors.textPrimary,
/// AppText cho phep doi mau khi ngu canh can (vi du chu tren nen mau, chu trang thai) — xem
/// ghi chu trong app_text.dart.
class AppTextStyles {
  AppTextStyles._();

  static const display = TextStyle(
    fontSize: 22,
    fontWeight: FontWeight.w800,
    color: AppColors.textPrimary,
    height: 1.3,
  );

  static const title = TextStyle(
    fontSize: 18,
    fontWeight: FontWeight.w800,
    color: AppColors.textPrimary,
    height: 1.3,
  );

  static const heading = TextStyle(
    fontSize: 15,
    fontWeight: FontWeight.w700,
    color: AppColors.textPrimary,
    height: 1.4,
  );

  static const body = TextStyle(
    fontSize: 14,
    fontWeight: FontWeight.w500,
    color: AppColors.textPrimary,
    height: 1.45,
  );

  static const caption = TextStyle(
    fontSize: 12.5,
    fontWeight: FontWeight.w500,
    color: AppColors.textSecondary,
    height: 1.4,
  );

  static const overline = TextStyle(
    fontSize: 10.5,
    fontWeight: FontWeight.w700,
    color: AppColors.textSecondary,
    letterSpacing: 0.3,
    height: 1.3,
  );

  static TextStyle of(AppTextVariant variant) {
    switch (variant) {
      case AppTextVariant.display:
        return display;
      case AppTextVariant.title:
        return title;
      case AppTextVariant.heading:
        return heading;
      case AppTextVariant.body:
        return body;
      case AppTextVariant.caption:
        return caption;
      case AppTextVariant.overline:
        return overline;
    }
  }
}
