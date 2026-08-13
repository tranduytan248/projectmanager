import 'package:flutter/material.dart';

/// Body dung tam cho cac man hinh chua noi API that.
/// Thay noi dung nay bang danh sach/form thuc te khi da co backend API.
class PlaceholderBody extends StatelessWidget {
  const PlaceholderBody({super.key, required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Text(
          message,
          textAlign: TextAlign.center,
          style: Theme.of(context).textTheme.bodyLarge,
        ),
      ),
    );
  }
}
