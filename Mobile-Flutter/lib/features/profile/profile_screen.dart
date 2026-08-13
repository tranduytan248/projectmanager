import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../shared/widgets/app_bottom_nav.dart';
import '../auth/auth_provider.dart';

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();

    return AppBottomNav(
      currentIndex: 3,
      appBar: AppBar(title: const Text('Cai dat')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(auth.displayName ?? '',
                style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 8),
            const Text(
                'TODO: GET /api/auth/me — ho so, doi mat khau, danh sach quyen.'),
            const Spacer(),
            FilledButton.tonal(
              onPressed: () => context.read<AuthProvider>().logout(context),
              child: const Text('Dang xuat'),
            ),
          ],
        ),
      ),
    );
  }
}
