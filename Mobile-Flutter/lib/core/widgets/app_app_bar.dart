import 'package:flutter/material.dart';

import '../theme/app_text_styles.dart';
import 'app_text.dart';

/// Thanh tieu de man hinh — dung `.claude/rules/FLUTTER_RULES.md`. Man hinh KHONG duoc dung
/// AppBar truc tiep, luon qua AppAppBar. [actions]/[leading] chi nen chua widget App* (vi du
/// AppIconButton) — khong lo kieu du lieu Material qua day, chi don gian giu Widget de linh hoat
/// vi ban than actions/leading da la "vi tri lap ghep", khong phai tham so mo ta kieu.
class AppAppBar extends StatelessWidget implements PreferredSizeWidget {
  const AppAppBar({super.key, required this.title, this.actions, this.leading, this.centerTitle = true});

  final String title;
  final List<Widget>? actions;
  final Widget? leading;
  final bool centerTitle;

  @override
  Widget build(BuildContext context) {
    return AppBar(
      title: AppText(title, variant: AppTextVariant.title),
      actions: actions,
      leading: leading,
      centerTitle: centerTitle,
    );
  }

  @override
  Size get preferredSize => const Size.fromHeight(kToolbarHeight);
}
