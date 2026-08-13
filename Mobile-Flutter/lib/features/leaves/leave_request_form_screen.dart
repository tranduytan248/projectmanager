import 'package:flutter/material.dart';

class LeaveRequestFormScreen extends StatelessWidget {
  const LeaveRequestFormScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Dang ky nghi phep')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('TODO: form ngay bat dau/ket thuc, ly do, gui POST /api/leaves.'),
            const SizedBox(height: 16),
            const TextField(decoration: InputDecoration(labelText: 'Ly do')),
            const SizedBox(height: 24),
            FilledButton(
              onPressed: () => Navigator.of(context).maybePop(),
              child: const Text('Gui yeu cau'),
            ),
          ],
        ),
      ),
    );
  }
}
