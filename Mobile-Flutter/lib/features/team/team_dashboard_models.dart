// Model dữ liệu cho Bảng điều khiển Tổ (Team Dashboard).

class TeamDashboardData {
  const TeamDashboardData({
    required this.today,
    required this.year,
    required this.month,
    required this.isCurrentMonth,
    required this.totalMembers,
    required this.idleCount,
    required this.overdueTodayCount,
    required this.members,
  });

  final DateTime today;
  final int year;
  final int month;
  final bool isCurrentMonth;
  final int totalMembers;
  final int idleCount;
  final int overdueTodayCount;
  final List<TeamMemberRow> members;

  factory TeamDashboardData.fromJson(Map<String, dynamic> json) {
    return TeamDashboardData(
      today: DateTime.tryParse(json['today']?.toString() ?? '') ?? DateTime.now(),
      year: json['year'] as int? ?? DateTime.now().year,
      month: json['month'] as int? ?? DateTime.now().month,
      isCurrentMonth: json['isCurrentMonth'] as bool? ?? false,
      totalMembers: json['totalMembers'] as int? ?? 0,
      idleCount: json['idleCount'] as int? ?? 0,
      overdueTodayCount: json['overdueTodayCount'] as int? ?? 0,
      members: (json['members'] as List<dynamic>? ?? [])
          .map((e) => TeamMemberRow.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}

class TeamMemberRow {
  const TeamMemberRow({
    required this.userId,
    required this.fullName,
    required this.todayTasks,
    required this.todayTaskCount,
    required this.overdueTodayCount,
    required this.kpi,
    required this.totalPenalty,
    required this.totalTasks,
    required this.implement,
    required this.support,
  });

  final int userId;
  final String fullName;
  final List<TeamTodayTask> todayTasks;
  final int todayTaskCount;
  final int overdueTodayCount;
  final TeamKpiSummary kpi;
  final double totalPenalty;
  final int totalTasks;
  final TeamProjectCount implement;
  final TeamProjectCount support;

  factory TeamMemberRow.fromJson(Map<String, dynamic> json) {
    return TeamMemberRow(
      userId: json['userId'] as int? ?? 0,
      fullName: json['fullName'] as String? ?? '',
      todayTasks: (json['todayTasks'] as List<dynamic>? ?? [])
          .map((e) => TeamTodayTask.fromJson(e as Map<String, dynamic>))
          .toList(),
      todayTaskCount: json['todayTaskCount'] as int? ?? 0,
      overdueTodayCount: json['overdueTodayCount'] as int? ?? 0,
      kpi: TeamKpiSummary.fromJson(json['kpi'] as Map<String, dynamic>? ?? {}),
      totalPenalty: (json['totalPenalty'] as num?)?.toDouble() ?? 0.0,
      totalTasks: json['totalTasks'] as int? ?? 0,
      implement: TeamProjectCount.fromJson(json['implement'] as Map<String, dynamic>? ?? {}),
      support: TeamProjectCount.fromJson(json['support'] as Map<String, dynamic>? ?? {}),
    );
  }
}

class TeamTodayTask {
  const TeamTodayTask({
    required this.taskId,
    required this.title,
    required this.projectId,
    required this.projectName,
    required this.state,
    required this.progress,
    required this.isOverdue,
  });

  final int taskId;
  final String title;
  final int projectId;
  final String projectName;
  final String state;
  final int progress;
  final bool isOverdue;

  factory TeamTodayTask.fromJson(Map<String, dynamic> json) {
    return TeamTodayTask(
      taskId: json['taskId'] as int? ?? 0,
      title: json['title'] as String? ?? '',
      projectId: json['projectId'] as int? ?? 0,
      projectName: json['projectName'] as String? ?? '',
      state: json['state'] as String? ?? '',
      progress: json['progress'] as int? ?? 0,
      isOverdue: json['isOverdue'] as bool? ?? false,
    );
  }
}

class TeamKpiSummary {
  const TeamKpiSummary({
    required this.finalPoint,
    required this.rank,
    required this.workedHours,
    required this.requiredHours,
    required this.penaltyPoint,
    required this.bonusPercent,
  });

  final double finalPoint;
  final String rank;
  final double workedHours;
  final double requiredHours;
  final double penaltyPoint;
  final double bonusPercent;

  factory TeamKpiSummary.fromJson(Map<String, dynamic> json) {
    return TeamKpiSummary(
      finalPoint: (json['finalPoint'] as num?)?.toDouble() ?? 0.0,
      rank: json['rank'] as String? ?? '—',
      workedHours: (json['workedHours'] as num?)?.toDouble() ?? 0.0,
      requiredHours: (json['requiredHours'] as num?)?.toDouble() ?? 0.0,
      penaltyPoint: (json['penaltyPoint'] as num?)?.toDouble() ?? 0.0,
      bonusPercent: (json['bonusPercent'] as num?)?.toDouble() ?? 0.0,
    );
  }
}

class TeamProjectCount {
  const TeamProjectCount({
    required this.projects,
    required this.tasks,
  });

  final int projects;
  final int tasks;

  factory TeamProjectCount.fromJson(Map<String, dynamic> json) {
    return TeamProjectCount(
      projects: json['projects'] as int? ?? 0,
      tasks: json['tasks'] as int? ?? 0,
    );
  }
}

class TeamMemberTaskItem {
  const TeamMemberTaskItem({
    required this.id,
    required this.code,
    required this.title,
    required this.projectId,
    required this.projectName,
    required this.state,
    required this.priority,
    required this.progress,
    this.startDate,
    this.dueDate,
    this.completedAt,
    required this.isOverdue,
    required this.loggedHours,
  });

  final int id;
  final String code;
  final String title;
  final int projectId;
  final String projectName;
  final String state;
  final String priority;
  final int progress;
  final DateTime? startDate;
  final DateTime? dueDate;
  final DateTime? completedAt;
  final bool isOverdue;
  final double loggedHours;

  factory TeamMemberTaskItem.fromJson(Map<String, dynamic> json) {
    return TeamMemberTaskItem(
      id: json['id'] as int? ?? 0,
      code: json['code'] as String? ?? '',
      title: json['title'] as String? ?? '',
      projectId: json['projectId'] as int? ?? 0,
      projectName: json['projectName'] as String? ?? '',
      state: json['state'] as String? ?? '',
      priority: json['priority'] as String? ?? '',
      progress: json['progress'] as int? ?? 0,
      startDate: json['startDate'] != null ? DateTime.tryParse(json['startDate'].toString()) : null,
      dueDate: json['dueDate'] != null ? DateTime.tryParse(json['dueDate'].toString()) : null,
      completedAt: json['completedAt'] != null ? DateTime.tryParse(json['completedAt'].toString()) : null,
      isOverdue: json['isOverdue'] as bool? ?? false,
      loggedHours: (json['loggedHours'] as num?)?.toDouble() ?? 0.0,
    );
  }
}

class TeamMemberTasksResult {
  const TeamMemberTasksResult({
    required this.userId,
    required this.memberName,
    required this.year,
    required this.month,
    required this.kind,
    required this.kindLabel,
    required this.totalProjects,
    required this.totalTasks,
    required this.tasks,
  });

  final int userId;
  final String memberName;
  final int year;
  final int month;
  final String kind;
  final String kindLabel;
  final int totalProjects;
  final int totalTasks;
  final List<TeamMemberTaskItem> tasks;

  factory TeamMemberTasksResult.fromJson(Map<String, dynamic> json) {
    return TeamMemberTasksResult(
      userId: json['userId'] as int? ?? 0,
      memberName: json['memberName'] as String? ?? '',
      year: json['year'] as int? ?? DateTime.now().year,
      month: json['month'] as int? ?? DateTime.now().month,
      kind: json['kind'] as String? ?? '',
      kindLabel: json['kindLabel'] as String? ?? '',
      totalProjects: json['totalProjects'] as int? ?? 0,
      totalTasks: json['totalTasks'] as int? ?? 0,
      tasks: (json['tasks'] as List<dynamic>? ?? [])
          .map((e) => TeamMemberTaskItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}
