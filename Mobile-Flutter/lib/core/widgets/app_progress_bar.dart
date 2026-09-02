import 'package:flutter/material.dart';

import '../theme/app_dimens.dart';

/// Thanh tiến độ dạng vạch ngang chuẩn của hệ thống BrewTask.
/// Tuân thủ nghiêm ngặt FLUTTER_RULES.md: Màn hình không gọi trực tiếp
/// LinearProgressIndicator, mọi hiển thị thanh tiến độ đi qua AppProgressBar.
class AppProgressBar extends StatelessWidget {
  const AppProgressBar({
    super.key,
    required this.value,
    this.height = 4.0,
    this.color,
    this.backgroundColor,
    this.borderRadius = AppDimens.radiusSm,
  });

  /// Giá trị tiến độ từ 0.0 đến 1.0
  final double value;

  /// Chiều cao của thanh tiến độ
  final double height;

  /// Màu sắc của dải tiến độ
  final Color? color;

  /// Màu nền của thanh tiến độ
  final Color? backgroundColor;

  /// Bo góc hai đầu thanh
  final double borderRadius;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final activeColor = color ?? theme.colorScheme.primary;
    final bgColor = backgroundColor ?? activeColor.withValues(alpha: 0.12);

    return ClipRRect(
      borderRadius: BorderRadius.circular(borderRadius),
      child: LinearProgressIndicator(
        value: value.clamp(0.0, 1.0),
        minHeight: height,
        backgroundColor: bgColor,
        valueColor: AlwaysStoppedAnimation<Color>(activeColor),
      ),
    );
  }
}
