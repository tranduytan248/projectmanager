import 'package:flutter/material.dart';

import '../../shared/widgets/placeholder_body.dart';

/// Man hinh Kanban rut gon cho mobile: hien thi dang list/swimlane,
/// doi trang thai qua dropdown/swipe thay vi keo-tha (xem goi y package
/// kanban_board trong https://pub.dev/packages/kanban_board de thay the
/// PlaceholderBody nay bang board that).
class ChecklistBoardScreen extends StatelessWidget {
  const ChecklistBoardScreen({super.key, required this.projectId});

  final String projectId;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Checklist dự án #$projectId')),
      body: PlaceholderBody(
        message:
            'TODO: GET /api/checklist?projectId=$projectId (tương ứng ChecklistController)\n'
            'Gợi ý dùng package kanban_board để dựng board kéo-thả.',
      ),
    );
  }
}
