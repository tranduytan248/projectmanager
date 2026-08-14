import 'package:flutter/material.dart';

import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../shared/widgets/placeholder_body.dart';

class KpiScreen extends StatelessWidget {
  const KpiScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const AppScaffold(
      appBar: AppAppBar(title: 'KPI của tôi'),
      // GET /api/kpi/mine (tương ứng KpiController) - điểm KPI theo tháng, chi tiết từng tiêu chí.
      body: PlaceholderBody(
        message: 'Tính năng đang được phát triển, vui lòng quay lại sau.',
      ),
    );
  }
}
