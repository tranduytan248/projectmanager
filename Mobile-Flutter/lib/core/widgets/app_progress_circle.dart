import 'package:flutter/material.dart';

/// Vòng tiến độ tròn hiển thị phần trăm hoàn thành hoặc chỉ số KPI.
/// Tuân thủ nghiêm ngặt quy tắc FLUTTER_RULES.md: Màn hình KHÔNG được gọi trực tiếp
/// CircularProgressIndicator, mọi hiển thị tiến độ vòng tròn đi qua AppProgressCircle.
class AppProgressCircle extends StatelessWidget {
  const AppProgressCircle({
    super.key,
    required this.value,
    this.size = 48.0,
    this.strokeWidth = 4.0,
    this.color,
    this.backgroundColor,
    this.child,
  });

  /// Giá trị tiến độ từ 0.0 đến 1.0
  final double value;

  /// Đường kính vòng tròn
  final double size;

  /// Độ dày nét vẽ
  final double strokeWidth;

  /// Màu sắc thanh tiến độ
  final Color? color;

  /// Màu nền của vòng tròn
  final Color? backgroundColor;

  /// Widget hiển thị ở tâm vòng tròn (thường là AppText hiển thị phần trăm hoặc điểm số)
  final Widget? child;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final activeColor = color ?? theme.colorScheme.primary;
    final bgColor = backgroundColor ?? activeColor.withValues(alpha: 0.12);

    return SizedBox(
      width: size,
      height: size,
      child: Stack(
        alignment: Alignment.center,
        children: [
          CircularProgressIndicator(
            value: value.clamp(0.0, 1.0),
            strokeWidth: strokeWidth,
            backgroundColor: bgColor,
            valueColor: AlwaysStoppedAnimation(activeColor),
          ),
          if (child != null) child!,
        ],
      ),
    );
  }
}
