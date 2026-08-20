import 'dart:async';

import 'package:flutter/material.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../theme/app_colors.dart';
import '../theme/app_dimens.dart';
import '../theme/app_text_styles.dart';
import 'app_icon_button.dart';
import 'app_text.dart';

/// Phan loai thong bao AppToastBanner — quyet dinh mau nen/vien/icon cua banner.
enum ToastType { success, warning, error }

/// Banner thong bao mau, TRUOT XUONG tu dinh man hinh va TU AN sau vai giay (hoac bam nut dong) —
/// dung `.claude/rules/FLUTTER_RULES.md`. Day la widget hien thi DUY NHAT cho thong bao toan cuc
/// cua app, dong vai tro `AppSnackbar`; man hinh KHONG duoc dung truc tiep, luon goi qua
/// `ToastService.show(...)` (`core/utils/toast_service.dart`).
///
/// Chon banner o TOP thay vi SnackBar o day man hinh (thiet ke truoc day): SnackBar o day rat de
/// bi ban phim dang mo hoac bottom sheet che mat — dung ngay tinh huong sheet "Cap nhat trang
/// thai" bao loi "Phai ghi gio cong..." gan nhu bi cat mep duoi. Banner nay do ToastService chen
/// thang vao Overlay goc (qua navigatorKey), luon noi TREN CUNG, de tren ca bottom sheet/dialog
/// dang mo.
class AppToastBanner extends StatefulWidget {
  const AppToastBanner({
    super.key,
    required this.message,
    required this.type,
    required this.onDismissed,
  });

  final String message;
  final ToastType type;

  /// Goi sau khi hoat canh an xong — noi goi (ToastService) dung tin hieu nay de go OverlayEntry.
  final VoidCallback onDismissed;

  @override
  State<AppToastBanner> createState() => _AppToastBannerState();
}

class _AppToastBannerState extends State<AppToastBanner>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 220),
    reverseDuration: const Duration(milliseconds: 180),
  );
  late final Animation<Offset> _slide = Tween(
    begin: const Offset(0, -1),
    end: Offset.zero,
  ).animate(CurvedAnimation(parent: _controller, curve: Curves.easeOutCubic));

  Timer? _autoDismissTimer;
  bool _dismissing = false;

  @override
  void initState() {
    super.initState();
    _controller.forward();
    // Loi/canh bao can nhieu thoi gian doc hon thong bao thanh cong ngan gon.
    final giay = widget.type == ToastType.success ? 3 : 4;
    _autoDismissTimer = Timer(Duration(seconds: giay), _dismiss);
  }

  Future<void> _dismiss() async {
    if (_dismissing) return;
    _dismissing = true;
    _autoDismissTimer?.cancel();
    if (mounted) await _controller.reverse();
    widget.onDismissed();
  }

  @override
  void dispose() {
    _autoDismissTimer?.cancel();
    _controller.dispose();
    super.dispose();
  }

  _ToastScheme get _scheme {
    switch (widget.type) {
      case ToastType.success:
        return const _ToastScheme(
          background: AppColors.successSoft,
          foreground: AppColors.successOnSoft,
          icon: PhosphorIconsRegular.checkCircle,
        );
      case ToastType.warning:
        return const _ToastScheme(
          background: AppColors.warningSoft,
          foreground: AppColors.warningOnSoft,
          icon: PhosphorIconsRegular.warningCircle,
        );
      case ToastType.error:
        return const _ToastScheme(
          background: AppColors.dangerSoft,
          foreground: AppColors.danger,
          icon: PhosphorIconsRegular.xCircle,
        );
    }
  }

  @override
  Widget build(BuildContext context) {
    final scheme = _scheme;

    // SafeArea + Padding thay vi ep sat notch/status bar — bottom: false vi banner luon nam o
    // TREN, khong lien quan vung an toan duoi (ban phim che khong anh huong banner o dinh).
    return SafeArea(
      bottom: false,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(
          AppDimens.space16,
          AppDimens.space8,
          AppDimens.space16,
          0,
        ),
        child: SlideTransition(
          position: _slide,
          child: FadeTransition(
            opacity: _controller,
            child: Material(
              // Material trong suot chi de lam nen cho InkWell/Tooltip ben trong (AppIconButton) —
              // mau that lay tu Container ben duoi theo AppColors.
              color: Colors.transparent,
              child: Container(
                decoration: BoxDecoration(
                  color: scheme.background,
                  borderRadius: BorderRadius.circular(AppDimens.radiusMd),
                  border:
                      Border.all(color: scheme.foreground.withValues(alpha: 0.28)),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.14),
                      blurRadius: 18,
                      offset: const Offset(0, 8),
                    ),
                  ],
                ),
                padding: const EdgeInsets.only(
                  left: AppDimens.space16,
                  top: AppDimens.space8,
                  bottom: AppDimens.space8,
                  right: AppDimens.space4,
                ),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                    PhosphorIcon(scheme.icon, color: scheme.foreground, size: 22),
                    const SizedBox(width: AppDimens.space12),
                    Expanded(
                      child: AppText(
                        widget.message,
                        variant: AppTextVariant.body,
                        color: scheme.foreground,
                        weight: FontWeight.w600,
                      ),
                    ),
                    AppIconButton(
                      icon: PhosphorIconsRegular.x,
                      tooltip: 'Đóng',
                      color: scheme.foreground,
                      size: 18,
                      onPressed: _dismiss,
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _ToastScheme {
  const _ToastScheme({
    required this.background,
    required this.foreground,
    required this.icon,
  });

  final Color background;
  final Color foreground;
  final IconData icon;
}
