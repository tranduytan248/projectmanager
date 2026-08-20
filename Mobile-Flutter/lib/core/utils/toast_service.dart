import 'package:flutter/material.dart';

import '../classes/app_keys.dart';
import '../widgets/app_toast_banner.dart';

export '../widgets/app_toast_banner.dart' show ToastType;

/// Thong bao dang banner mau o DINH man hinh, tu bien mat sau vai giay hoac bam nut dong — chen
/// thang vao Overlay goc qua navigatorKey (giong cach DialogService dung navigatorKey), khong can
/// BuildContext cua man hinh hien tai nen goi duoc tu bat ky dau (kể ca tu Provider/HttpManager).
///
/// Truoc day dung SnackBar o DAY man hinh nhung de bi ban phim/bottom sheet dang mo che mat (vi
/// du sheet "Cap nhat trang thai" — thong bao loi bi cat mep duoi). Doi sang banner o TOP, chen
/// vao Overlay nen luon hien TREN CUNG, de tren ca bottom sheet/dialog dang mo.
///
/// Day chinh la noi DUY NHAT duoc chen widget thong bao toan cuc cua app — dong vai tro
/// `AppSnackbar.show(...)` theo `.claude/rules/FLUTTER_RULES.md`. Man hinh KHONG duoc tu hien thi
/// SnackBar/banner, luon qua ToastService.
class ToastService {
  ToastService._();

  static OverlayEntry? _entry;

  static void show(String message, {required ToastType type}) {
    final overlay = navigatorKey.currentState?.overlay;
    if (overlay == null) return;

    // Thay ngay banner dang hien (neu co) bang banner moi — giong hanh vi clearSnackBars() truoc
    // day, tranh chong nhieu banner khi co nhieu loi/thanh cong xay ra lien tiep nhanh.
    _entry?.remove();
    _entry = null;

    late final OverlayEntry entry;
    entry = OverlayEntry(
      builder: (context) => Positioned(
        top: 0,
        left: 0,
        right: 0,
        child: AppToastBanner(
          message: message,
          type: type,
          onDismissed: () {
            entry.remove();
            if (identical(_entry, entry)) _entry = null;
          },
        ),
      ),
    );
    _entry = entry;
    overlay.insert(entry);
  }
}
