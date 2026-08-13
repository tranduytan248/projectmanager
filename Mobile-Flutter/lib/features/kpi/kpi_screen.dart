import 'package:flutter/material.dart';

import '../../shared/widgets/placeholder_body.dart';

class KpiScreen extends StatelessWidget {
  const KpiScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('KPI của tôi')),
      body: const PlaceholderBody(
        message: 'TODO: GET /api/kpi/mine (tương ứng KpiController)\n'
            'Điểm KPI theo tháng, chi tiết từng tiêu chí.',
      ),
    );
  }
}
