import 'package:flutter/material.dart';

import '../theme/app_text_styles.dart';

/// Widget chu chuan cua du an — dung `.claude/rules/FLUTTER_RULES.md`. Man hinh KHONG duoc dung
/// `Text` truc tiep, luon qua AppText de co chu lay tu thang AppTextStyles thay vi tuy hung tung
/// noi. Mau mac dinh theo tung variant (xem AppTextStyles) — chi truyen [color] khi ngu canh that
/// su can khac (trang thai thanh cong/canh bao/loi, chu tren nen mau...).
class AppText extends StatelessWidget {
  const AppText(
    this.data, {
    super.key,
    this.variant = AppTextVariant.body,
    this.color,
    this.weight,
    this.fontSize,
    this.align,
    this.maxLines,
    this.overflow,
    this.height,
    this.letterSpacing,
    this.decoration,
  });

  final String data;
  final AppTextVariant variant;
  final Color? color;
  final FontWeight? weight;
  final double? fontSize;
  final TextAlign? align;
  final int? maxLines;
  final TextOverflow? overflow;
  final double? height;
  final double? letterSpacing;
  final TextDecoration? decoration;

  @override
  Widget build(BuildContext context) {
    final base = AppTextStyles.of(variant);

    return Text(
      data,
      textAlign: align,
      maxLines: maxLines,
      overflow: overflow,
      style: base.copyWith(
        color: color,
        fontWeight: weight,
        fontSize: fontSize,
        height: height,
        letterSpacing: letterSpacing,
        decoration: decoration,
      ),
    );
  }
}
