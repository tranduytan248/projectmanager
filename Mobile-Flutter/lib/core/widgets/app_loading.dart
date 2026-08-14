import 'package:flutter/material.dart';

import '../../config/app_theme.dart';

/// Vong xoay cho bao dang tai — dung `.claude/rules/FLUTTER_RULES.md`. Man hinh KHONG duoc dung
/// CircularProgressIndicator truc tiep, luon qua AppLoading.
class AppLoading extends StatelessWidget {
  const AppLoading({super.key, this.size = 24, this.color, this.strokeWidth = 2.5});

  final double size;
  final Color? color;
  final double strokeWidth;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: size,
      height: size,
      child: CircularProgressIndicator(
        strokeWidth: strokeWidth,
        color: color ?? AppTheme.brandBlue,
      ),
    );
  }
}
