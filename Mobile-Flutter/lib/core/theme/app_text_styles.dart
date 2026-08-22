import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

import 'app_colors.dart';

/// Các mục đích hiển thị chữ — chuẩn Typography dự án.
enum AppTextVariant {
  /// 20-22 — tiêu đề màn hình / con số nổi bật.
  display,

  /// 17-19 — tiêu đề màn hình nhỏ hơn / tên thực thể chính.
  title,

  /// 15-16 — tiêu đề một khối/mục trong màn hình.
  heading,

  /// 14 — nội dung chính, giá trị dữ liệu.
  body,

  /// 12-13 — chú thích, nhãn phụ, ngày tháng, mô tả ngắn.
  caption,

  /// 10-11, letter-spacing rộng — nhãn cực nhỏ (badge, tag trạng thái).
  overline,
}

/// Định nghĩa TextStyle cho từng AppTextVariant dùng font Roboto thanh thoát, dễ đọc như ảnh mẫu.
class AppTextStyles {
  AppTextStyles._();

  static final display = GoogleFonts.roboto(
    fontSize: 22,
    fontWeight: FontWeight.w800,
    color: AppColors.textPrimary,
    height: 1.3,
  );

  static final title = GoogleFonts.roboto(
    fontSize: 18,
    fontWeight: FontWeight.w800,
    color: AppColors.textPrimary,
    height: 1.3,
  );

  static final heading = GoogleFonts.roboto(
    fontSize: 15,
    fontWeight: FontWeight.w700,
    color: AppColors.textPrimary,
    height: 1.4,
  );

  static final body = GoogleFonts.roboto(
    fontSize: 14,
    fontWeight: FontWeight.w400,
    color: AppColors.textPrimary,
    height: 1.45,
  );

  static final caption = GoogleFonts.roboto(
    fontSize: 12.5,
    fontWeight: FontWeight.w400,
    color: AppColors.textSecondary,
    height: 1.4,
  );

  static final overline = GoogleFonts.roboto(
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
