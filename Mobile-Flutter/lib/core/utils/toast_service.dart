import 'package:flutter/material.dart';

import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_text.dart';
import '../classes/app_keys.dart';

/// Thong bao ngan, tu bien mat — dung SnackBar qua scaffoldMessengerKey toan cuc thay vi them
/// dependency fluttertoast (giu so luong package toi thieu).
///
/// Day chinh la noi DUY NHAT duoc goi SnackBar cua app — dong vai tro `AppSnackbar.show(...)`
/// theo `.claude/rules/FLUTTER_RULES.md`. Man hinh KHONG duoc tu goi SnackBar, luon qua
/// ToastService.
class ToastService {
  ToastService._();

  static void show(String message) {
    final messenger = scaffoldMessengerKey.currentState;
    if (messenger == null) return;

    messenger
      ..clearSnackBars()
      ..showSnackBar(SnackBar(
          // Nen SnackBar toi mau (Material3 inverseSurface) — chu phai sang mau de doc duoc,
          // AppTextStyles.body mac dinh la mau toi (textPrimary) nen phai ghi de o day.
          content: AppText(message, variant: AppTextVariant.body, color: Colors.white),
          duration: const Duration(seconds: 2)));
  }
}
