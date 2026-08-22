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
    final rawToday = json['today'] ?? json['Today'];
    final rawMembers = json['members'] ?? json['Members'];
    return TeamDashboardData(
      today: DateTime.tryParse(rawToday?.toString() ?? '') ?? DateTime.now(),
      year: (json['year'] ?? json['Year']) as int? ?? DateTime.now().year,
      month: (json['month'] ?? json['Month']) as int? ?? DateTime.now().month,
      isCurrentMonth: (json['isCurrentMonth'] ?? json['IsCurrentMonth']) as bool? ?? false,
      totalMembers: (json['totalMembers'] ?? json['TotalMembers']) as int? ?? 0,
      idleCount: (json['idleCount'] ?? json['IdleCount']) as int? ?? 0,
      overdueTodayCount: (json['overdueTodayCount'] ?? json['OverdueTodayCount']) as int? ?? 0,
      members: (rawMembers as List<dynamic>? ?? [])
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
    final rawTasks = json['todayTasks'] ?? json['TodayTasks'];
    final rawKpi = json['kpi'] ?? json['Kpi'];
    final rawImp = json['implement'] ?? json['Implement'];
    final rawSup = json['support'] ?? json['Support'];
    final rawPenalty = json['totalPenalty'] ?? json['TotalPenalty'];

    return TeamMemberRow(
      userId: (json['userId'] ?? json['UserId']) as int? ?? 0,
      fullName: (json['fullName'] ?? json['FullName'] ?? '').toString(),
      todayTasks: (rawTasks as List<dynamic>? ?? [])
          .map((e) => TeamTodayTask.fromJson(e as Map<String, dynamic>))
          .toList(),
      todayTaskCount: (json['todayTaskCount'] ?? json['TodayTaskCount']) as int? ?? 0,
      overdueTodayCount: (json['overdueTodayCount'] ?? json['OverdueTodayCount']) as int? ?? 0,
      kpi: TeamKpiSummary.fromJson(rawKpi as Map<String, dynamic>? ?? {}),
      totalPenalty: (rawPenalty as num?)?.toDouble() ?? 0.0,
      totalTasks: (json['totalTasks'] ?? json['TotalTasks']) as int? ?? 0,
      implement: TeamProjectCount.fromJson(rawImp as Map<String, dynamic>? ?? {}),
      support: TeamProjectCount.fromJson(rawSup as Map<String, dynamic>? ?? {}),
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
      taskId: (json['taskId'] ?? json['TaskId']) as int? ?? 0,
      title: (json['title'] ?? json['Title'] ?? '').toString(),
      projectId: (json['projectId'] ?? json['ProjectId']) as int? ?? 0,
      projectName: (json['projectName'] ?? json['ProjectName'] ?? '').toString(),
      state: (json['state'] ?? json['State'] ?? '').toString(),
      progress: (json['progress'] ?? json['Progress']) as int? ?? 0,
      isOverdue: (json['isOverdue'] ?? json['IsOverdue']) as bool? ?? false,
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
    final rawFinal = json['finalPoint'] ?? json['FinalPoint'];
    final rawRank = json['rank'] ?? json['Rank'];
    final rawWorked = json['workedHours'] ?? json['WorkedHours'];
    final rawReq = json['requiredHours'] ?? json['RequiredHours'];
    final rawPenalty = json['penaltyPoint'] ?? json['PenaltyPoint'] ?? json['TotalPenalty'];
    final rawBonus = json['bonusPercent'] ?? json['BonusPercent'];

    return TeamKpiSummary(
      finalPoint: (rawFinal as num?)?.toDouble() ?? 0.0,
      rank: (rawRank ?? '—').toString(),
      workedHours: (rawWorked as num?)?.toDouble() ?? 0.0,
      requiredHours: (rawReq as num?)?.toDouble() ?? 0.0,
      penaltyPoint: (rawPenalty as num?)?.toDouble() ?? 0.0,
      bonusPercent: (rawBonus as num?)?.toDouble() ?? 0.0,
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
      projects: (json['projects'] ?? json['Projects']) as int? ?? 0,
      tasks: (json['tasks'] ?? json['Tasks']) as int? ?? 0,
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
    final rawStart = json['startDate'] ?? json['StartDate'];
    final rawDue = json['dueDate'] ?? json['DueDate'];
    final rawComp = json['completedAt'] ?? json['CompletedAt'];
    final rawHours = json['loggedHours'] ?? json['LoggedHours'];

    return TeamMemberTaskItem(
      id: (json['id'] ?? json['Id']) as int? ?? 0,
      code: (json['code'] ?? json['Code'] ?? '').toString(),
      title: (json['title'] ?? json['Title'] ?? '').toString(),
      projectId: (json['projectId'] ?? json['ProjectId']) as int? ?? 0,
      projectName: (json['projectName'] ?? json['ProjectName'] ?? '').toString(),
      state: (json['state'] ?? json['State'] ?? '').toString(),
      priority: (json['priority'] ?? json['Priority'] ?? '').toString(),
      progress: (json['progress'] ?? json['Progress']) as int? ?? 0,
      startDate: rawStart != null ? DateTime.tryParse(rawStart.toString()) : null,
      dueDate: rawDue != null ? DateTime.tryParse(rawDue.toString()) : null,
      completedAt: rawComp != null ? DateTime.tryParse(rawComp.toString()) : null,
      isOverdue: (json['isOverdue'] ?? json['IsOverdue']) as bool? ?? false,
      loggedHours: (rawHours as num?)?.toDouble() ?? 0.0,
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
    final rawTasks = json['tasks'] ?? json['Tasks'];

    return TeamMemberTasksResult(
      userId: (json['userId'] ?? json['UserId']) as int? ?? 0,
      memberName: (json['memberName'] ?? json['MemberName'] ?? '').toString(),
      year: (json['year'] ?? json['Year']) as int? ?? DateTime.now().year,
      month: (json['month'] ?? json['Month']) as int? ?? DateTime.now().month,
      kind: (json['kind'] ?? json['Kind'] ?? '').toString(),
      kindLabel: (json['kindLabel'] ?? json['KindLabel'] ?? '').toString(),
      totalProjects: (json['totalProjects'] ?? json['TotalProjects']) as int? ?? 0,
      totalTasks: (json['totalTasks'] ?? json['TotalTasks']) as int? ?? 0,
      tasks: (rawTasks as List<dynamic>? ?? [])
          .map((e) => TeamMemberTaskItem.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}
