import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../auth/auth_controller.dart';

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authControllerProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Ca nhan')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(auth.displayName ?? '', style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 8),
            const Text('TODO: GET /api/auth/me — ho so, doi mat khau, danh sach quyen.'),
            const Spacer(),
            FilledButton.tonal(
              onPressed: () => ref.read(authControllerProvider.notifier).logout(),
              child: const Text('Dang xuat'),
            ),
          ],
        ),
      ),
    );
  }
}
