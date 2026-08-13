import 'package:flutter/material.dart';

/// Chon layout theo be rong man hinh (mobile/tablet/desktop...), fallback xuong breakpoint
/// thap hon khi layout tuong ung chua duoc cung cap. App nay hien chi dung [mobile] — cac
/// breakpoint con lai giu san cho tuong lai neu can ho tro tablet/desktop.
///
/// Bocj them [Title] (ten tab tren he dieu hanh) va mot [GestureDetector] tap-de-an-ban-phim.
class Display extends StatelessWidget {
  const Display({
    super.key,
    required this.title,
    required this.mobile,
    this.mobileLandscape,
    this.tablet,
    this.tabletLandscape,
    this.desktop,
    this.desktopLarge,
  });

  final String title;
  final Widget mobile;
  final Widget? mobileLandscape;
  final Widget? tablet;
  final Widget? tabletLandscape;
  final Widget? desktop;
  final Widget? desktopLarge;

  static const _mobileLandscapeBreak = 576.0;
  static const _tabletBreak = 768.0;
  static const _tabletLandscapeBreak = 992.0;
  static const _desktopBreak = 1200.0;
  static const _desktopLargeBreak = 1400.0;

  Widget _pick(double width) {
    if (width >= _desktopLargeBreak)
      return desktopLarge ?? _pick(_desktopLargeBreak - 1);
    if (width >= _desktopBreak) return desktop ?? _pick(_desktopBreak - 1);
    if (width >= _tabletLandscapeBreak)
      return tabletLandscape ?? _pick(_tabletLandscapeBreak - 1);
    if (width >= _tabletBreak) return tablet ?? _pick(_tabletBreak - 1);
    if (width >= _mobileLandscapeBreak) return mobileLandscape ?? mobile;
    return mobile;
  }

  @override
  Widget build(BuildContext context) {
    final width = MediaQuery.sizeOf(context).width;

    return Title(
      title: title,
      color: Theme.of(context).colorScheme.primary,
      child: GestureDetector(
        behavior: HitTestBehavior.translucent,
        onTap: () => FocusManager.instance.primaryFocus?.unfocus(),
        child: _pick(width),
      ),
    );
  }
}
