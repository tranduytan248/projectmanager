import '../dashboard/dashboard_models.dart' show TaskItem;

class AssigneeOption {
  const AssigneeOption({required this.userId, required this.fullName});

  final int userId;
  final String fullName;

  factory AssigneeOption.fromJson(Map<String, dynamic> json) => AssigneeOption(
        userId: json['UserId'] as int,
        fullName: json['FullName'] as String? ?? '',
      );
}

class ChecklistData {
  const ChecklistData({
    required this.projectId,
    required this.projectName,
    required this.pmName,
    required this.canEdit,
    this.canCreateTask = false,
    required this.totalCount,
    required this.doneCount,
    required this.overdueCount,
    required this.donePercent,
    required this.tasks,
    required this.assignees,
  });

  final int projectId;
  final String projectName;
  final String? pmName;
  final bool canEdit;
  final bool canCreateTask;
  final int totalCount;
  final int doneCount;
  final int overdueCount;
  final int donePercent;
  final List<TaskItem> tasks;
  final List<AssigneeOption> assignees;

  factory ChecklistData.fromJson(Map<String, dynamic> json) {
    final canEdit = json['CanEdit'] as bool? ?? false;
    final canCreate = json['CanCreateTask'] as bool? ?? canEdit;
    return ChecklistData(
      projectId: json['ProjectId'] as int,
      projectName: json['ProjectName'] as String? ?? '',
      pmName: json['PmName'] as String?,
      canEdit: canEdit,
      canCreateTask: canCreate,
      totalCount: json['TotalCount'] as int? ?? 0,
      doneCount: json['DoneCount'] as int? ?? 0,
      overdueCount: json['OverdueCount'] as int? ?? 0,
      donePercent: json['DonePercent'] as int? ?? 0,
      tasks: (json['Tasks'] as List<dynamic>? ?? [])
          .map((e) => TaskItem.fromJson(e as Map<String, dynamic>))
          .toList(),
      assignees: (json['Assignees'] as List<dynamic>? ?? [])
          .map((e) => AssigneeOption.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}
