import 'package:flutter/material.dart';

import '../theme/app_colors.dart';
import '../theme/app_dimens.dart';

/// Nut chi co icon — dung `.claude/rules/FLUTTER_RULES.md`. Man hinh KHONG duoc dung IconButton
/// truc tiep, luon qua AppIconButton. Vung cham mac dinh toi thieu 48x48dp du icon nho hon.
class AppIconButton extends StatelessWidget {
  const AppIconButton({
    super.key,
    required this.icon,
    required this.onPressed,
    this.color,
    this.size = 20,
    this.tooltip,
    this.background,
    this.minSize = AppDimens.minTapTarget,
  });

  final IconData icon;
  final VoidCallback? onPressed;
  final Color? color;
  final double size;
  final String? tooltip;

  /// Mau nen tron phia sau icon (vi du nut chuong thong bao tren Dashboard) — tuy chon.
  final Color? background;

  /// Vung cham toi thieu — mac dinh 48dp (AppDimens.minTapTarget). CHI duoc giam khi nut la hanh
  /// dong PHU, nam DOC LAP (khong ep sat nut bam duoc khac) va co khoang trong xung quanh du
  /// tranh cham nham — vi du nut "x" go dieu kien loc tren 1 chip rieng le. Khong duoc dung de
  /// thu nho vung cham cho hanh dong CHINH cua man hinh.
  final double minSize;

  @override
  Widget build(BuildContext context) {
    final button = IconButton(
      onPressed: onPressed,
      tooltip: tooltip,
      icon: Icon(icon, size: size, color: color ?? AppColors.textSecondary),
      style: IconButton.styleFrom(
        minimumSize: Size.square(minSize),
        backgroundColor: background,
        shape: const CircleBorder(),
      ),
    );
    return button;
  }
}
