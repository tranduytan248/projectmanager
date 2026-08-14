import 'package:flutter/material.dart';

/// Khung man hinh — dung `.claude/rules/FLUTTER_RULES.md`. Man hinh KHONG duoc dung Scaffold
/// truc tiep, luon qua AppScaffold. Danh cho man hinh KHONG co thanh dieu huong duoi (man con,
/// push tu man cap-tab) — man cap-tab dung `AppBottomNav` (shared/widgets/app_bottom_nav.dart),
/// ban than no da boc Scaffold rieng, khong can qua AppScaffold nua.
class AppScaffold extends StatelessWidget {
  const AppScaffold({
    super.key,
    required this.body,
    this.appBar,
    this.floatingActionButton,
    this.resizeToAvoidBottomInset,
  });

  final Widget body;
  final PreferredSizeWidget? appBar;
  final Widget? floatingActionButton;
  final bool? resizeToAvoidBottomInset;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: appBar,
      body: body,
      floatingActionButton: floatingActionButton,
      resizeToAvoidBottomInset: resizeToAvoidBottomInset,
    );
  }
}
