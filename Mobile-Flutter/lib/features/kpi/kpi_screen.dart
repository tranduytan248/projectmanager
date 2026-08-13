import 'package:flutter/material.dart';

import '../../shared/widgets/placeholder_body.dart';

class KpiScreen extends StatelessWidget {
  const KpiScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('KPI cua toi')),
      body: const PlaceholderBody(
        message: 'TODO: GET /api/kpi/mine (tuong ung KpiController)\n'
            'Diem KPI theo thang, chi tiet tung tieu chi.',
      ),
    );
  }
}
