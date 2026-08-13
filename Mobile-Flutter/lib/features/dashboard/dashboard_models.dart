// Model phang cho JSON tra ve tu DashboardApi/Index — ten truong giu nguyen PascalCase khop
// voi DashboardDto ben backend (Models/Api/ApiDtos.cs) de doi chieu khong nham.

/// ASP.NET MVC (JavaScriptSerializer) tra ngay thang kieu cu "/Date(1734000000000)/" (so mili-giay
/// tu epoch), khong phai ISO 8601 — DateTime.parse chuan khong doc duoc, phai tu tach so ra.
DateTime? parseAspNetDate(String? raw) {
  if (raw == null) return null;
  final match = RegExp(r'/Date\((-?\d+)\)/').firstMatch(raw);
  if (match == null) return null;
  final millis = int.tryParse(match.group(1)!);
  if (millis == null) return null;
  return DateTime.fromMillisecondsSinceEpoch(millis);
}

class ProjectSummary {
  const ProjectSummary({
    required this.id,
    required this.name,
    required this.description,
    required this.progressPercent,
    required this.memberCount,
  });

  final int id;
  final String name;
  final String? description;
  final int progressPercent;
  final int memberCount;

  factory ProjectSummary.fromJson(Map<String, dynamic> json) => ProjectSummary(
        id: json['Id'] as int,
        name: json['Name'] as String? ?? '',
        description: json['Description'] as String?,
        progressPercent: json['ProgressPercent'] as int? ?? 0,
        memberCount: json['MemberCount'] as int? ?? 0,
      );
}

class TaskItem {
  const TaskItem({
    required this.id,
    required this.code,
    required this.title,
    required this.projectName,
    required this.state,
    required this.priority,
    required this.dueDate,
    required this.isOverdue,
    required this.isDueToday,
  });

  final int id;
  final String code;
  final String title;
  final String? projectName;
  final String state;
  final String priority;
  final DateTime? dueDate;
  final bool isOverdue;
  final bool isDueToday;

  factory TaskItem.fromJson(Map<String, dynamic> json) => TaskItem(
        id: json['Id'] as int,
        code: json['Code'] as String? ?? '',
        title: json['Title'] as String? ?? '',
        projectName: json['ProjectName'] as String?,
        state: json['State'] as String? ?? '',
        priority: json['Priority'] as String? ?? '',
        dueDate: parseAspNetDate(json['DueDate'] as String?),
        isOverdue: json['IsOverdue'] as bool? ?? false,
        isDueToday: json['IsDueToday'] as bool? ?? false,
      );
}

class TaskStats {
  const TaskStats({
    required this.openCount,
    required this.inProgressCount,
    required this.notStartedCount,
    required this.overdueCount,
    required this.dueSoonCount,
    required this.doneCount,
  });

  final int openCount;
  final int inProgressCount;
  final int notStartedCount;
  final int overdueCount;
  final int dueSoonCount;
  final int doneCount;

  factory TaskStats.fromJson(Map<String, dynamic> json) => TaskStats(
        openCount: json['OpenCount'] as int? ?? 0,
        inProgressCount: json['InProgressCount'] as int? ?? 0,
        notStartedCount: json['NotStartedCount'] as int? ?? 0,
        overdueCount: json['OverdueCount'] as int? ?? 0,
        dueSoonCount: json['DueSoonCount'] as int? ?? 0,
        doneCount: json['DoneCount'] as int? ?? 0,
      );
}

class KpiSummary {
  const KpiSummary({
    required this.finalPoint,
    required this.rank,
    required this.scoreMax,
    required this.scorePercent,
    required this.workedHours,
    required this.requiredHours,
    required this.hoursPercent,
    required this.hoursShort,
  });

  final double finalPoint;
  final String rank;
  final double scoreMax;
  final int scorePercent;
  final double workedHours;
  final double requiredHours;
  final int hoursPercent;
  final double hoursShort;

  factory KpiSummary.fromJson(Map<String, dynamic> json) => KpiSummary(
        finalPoint: (json['FinalPoint'] as num?)?.toDouble() ?? 0,
        rank: json['Rank'] as String? ?? '',
        scoreMax: (json['ScoreMax'] as num?)?.toDouble() ?? 100,
        scorePercent: json['ScorePercent'] as int? ?? 0,
        workedHours: (json['WorkedHours'] as num?)?.toDouble() ?? 0,
        requiredHours: (json['RequiredHours'] as num?)?.toDouble() ?? 0,
        hoursPercent: json['HoursPercent'] as int? ?? 0,
        hoursShort: (json['HoursShort'] as num?)?.toDouble() ?? 0,
      );
}

class UpcomingLeave {
  const UpcomingLeave({
    required this.fromDate,
    required this.toDate,
    required this.days,
    required this.kind,
  });

  final DateTime? fromDate;
  final DateTime? toDate;
  final double days;
  final String kind;

  factory UpcomingLeave.fromJson(Map<String, dynamic> json) => UpcomingLeave(
        fromDate: parseAspNetDate(json['FromDate'] as String?),
        toDate: parseAspNetDate(json['ToDate'] as String?),
        days: (json['Days'] as num?)?.toDouble() ?? 0,
        kind: json['Kind'] as String? ?? '',
      );
}

class LeaveSummary {
  const LeaveSummary({
    required this.approvedDays,
    required this.pendingCount,
    required this.upcoming,
  });

  final double approvedDays;
  final int pendingCount;
  final List<UpcomingLeave> upcoming;

  factory LeaveSummary.fromJson(Map<String, dynamic> json) => LeaveSummary(
        approvedDays: (json['ApprovedDays'] as num?)?.toDouble() ?? 0,
        pendingCount: json['PendingCount'] as int? ?? 0,
        upcoming: (json['Upcoming'] as List<dynamic>? ?? [])
            .map((e) => UpcomingLeave.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class ProjectAttention {
  const ProjectAttention({
    required this.id,
    required this.name,
    required this.customer,
    required this.pmName,
    required this.progressPercent,
    required this.overdueCount,
  });

  final int id;
  final String name;
  final String? customer;
  final String? pmName;
  final int progressPercent;
  final int overdueCount;

  factory ProjectAttention.fromJson(Map<String, dynamic> json) => ProjectAttention(
        id: json['Id'] as int,
        name: json['Name'] as String? ?? '',
        customer: json['Customer'] as String?,
        pmName: json['PmName'] as String?,
        progressPercent: json['ProgressPercent'] as int? ?? 0,
        overdueCount: json['OverdueCount'] as int? ?? 0,
      );
}

class TeamSummary {
  const TeamSummary({
    required this.openProjectCount,
    required this.myPmCount,
    required this.openTaskCount,
    required this.overdueTaskCount,
    required this.projects,
  });

  final int openProjectCount;
  final int myPmCount;
  final int openTaskCount;
  final int overdueTaskCount;
  final List<ProjectAttention> projects;

  factory TeamSummary.fromJson(Map<String, dynamic> json) => TeamSummary(
        openProjectCount: json['OpenProjectCount'] as int? ?? 0,
        myPmCount: json['MyPmCount'] as int? ?? 0,
        openTaskCount: json['OpenTaskCount'] as int? ?? 0,
        overdueTaskCount: json['OverdueTaskCount'] as int? ?? 0,
        projects: (json['Projects'] as List<dynamic>? ?? [])
            .map((e) => ProjectAttention.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class DashboardData {
  const DashboardData({
    required this.projectCount,
    required this.todayTaskCount,
    required this.projects,
    required this.tasks,
    required this.taskStats,
    required this.kpi,
    required this.leave,
    required this.canSeeTeam,
    required this.canApproveLeave,
    required this.pendingLeaveApprovalCount,
    required this.team,
  });

  final int projectCount;
  final int todayTaskCount;
  final List<ProjectSummary> projects;
  final List<TaskItem> tasks;
  final TaskStats taskStats;
  final KpiSummary? kpi;
  final LeaveSummary leave;
  final bool canSeeTeam;
  final bool canApproveLeave;
  final int pendingLeaveApprovalCount;
  final TeamSummary? team;

  factory DashboardData.fromJson(Map<String, dynamic> json) => DashboardData(
        projectCount: json['ProjectCount'] as int? ?? 0,
        todayTaskCount: json['TodayTaskCount'] as int? ?? 0,
        projects: (json['Projects'] as List<dynamic>? ?? [])
            .map((e) => ProjectSummary.fromJson(e as Map<String, dynamic>))
            .toList(),
        tasks: (json['Tasks'] as List<dynamic>? ?? [])
            .map((e) => TaskItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        taskStats: TaskStats.fromJson(
            json['TaskStats'] as Map<String, dynamic>? ?? const {}),
        kpi: json['Kpi'] == null
            ? null
            : KpiSummary.fromJson(json['Kpi'] as Map<String, dynamic>),
        leave: LeaveSummary.fromJson(
            json['Leave'] as Map<String, dynamic>? ?? const {}),
        canSeeTeam: json['CanSeeTeam'] as bool? ?? false,
        canApproveLeave: json['CanApproveLeave'] as bool? ?? false,
        pendingLeaveApprovalCount: json['PendingLeaveApprovalCount'] as int? ?? 0,
        team: json['Team'] == null
            ? null
            : TeamSummary.fromJson(json['Team'] as Map<String, dynamic>),
      );
}

/// Nhan hien thi cho gia tri tho cua TaskStates (server tra "DangLam", "HoanThanh"...) — khop Y
/// HET TaskStates.Display ben backend (Models/Work/WorkTask.cs).
String taskStateLabel(String state) {
  switch (state) {
    case 'ChuaBatDau':
      return 'Chưa bắt đầu';
    case 'DangLam':
      return 'Đang làm';
    case 'TamDung':
      return 'Tạm dừng';
    case 'HoanThanh':
      return 'Hoàn thành';
    case 'Huy':
      return 'Huỷ';
    default:
      return state;
  }
}

/// Khop TaskPriorities.Display ben backend.
String taskPriorityLabel(String priority) {
  switch (priority) {
    case 'Cao':
      return 'Cao';
    case 'TrungBinh':
      return 'Trung bình';
    case 'Thap':
      return 'Thấp';
    default:
      return priority;
  }
}

/// Khop LeaveKinds.Display ben backend (Models/Work/LeaveRequest.cs).
String leaveKindLabel(String kind) {
  switch (kind) {
    case 'PhepNam':
      return 'Phép năm';
    case 'KhongLuong':
      return 'Không lương';
    case 'Om':
      return 'Ốm';
    case 'Khac':
      return 'Khác';
    default:
      return kind;
  }
}
