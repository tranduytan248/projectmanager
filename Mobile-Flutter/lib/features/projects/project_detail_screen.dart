import 'package:flutter/material.dart';

import '../../shared/widgets/placeholder_body.dart';

class ProjectDetailScreen extends StatelessWidget {
  const ProjectDetailScreen({super.key, required this.projectId});

  final String projectId;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Dự án #$projectId')),
      body: PlaceholderBody(
        message: 'TODO: GET /api/projects/$projectId\n'
            'Thành viên, tiến độ, file đính kèm. Dẫn sang ChecklistBoardScreen.',
      ),
    );
  }
}
