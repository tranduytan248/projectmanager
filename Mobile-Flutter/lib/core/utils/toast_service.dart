import 'package:flutter/material.dart';

import '../classes/app_keys.dart';

/// Thong bao ngan, tu bien mat — dung SnackBar qua scaffoldMessengerKey toan cuc thay vi them
/// dependency fluttertoast (giu so luong package toi thieu).
class ToastService {
  ToastService._();

  static void show(String message) {
    final messenger = scaffoldMessengerKey.currentState;
    if (messenger == null) return;

    messenger
      ..clearSnackBars()
      ..showSnackBar(SnackBar(content: Text(message), duration: const Duration(seconds: 2)));
  }
}
