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
    required this.assigneeName,
    required this.progress,
    required this.kind,
    required this.parentId,
    this.canEdit = false,
    this.createdAt,
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
  final String? assigneeName;
  final int progress;

  /// Gia tri tho cua TaskKinds (vi du "Checklist", "HoTro", "NgoaiDuAn").
  final String kind;

  /// 0 = muc goc, khac 0 = viec con cua muc co Id do.
  final int parentId;

  /// Nguoi dang dang nhap co sua duoc trang thai/tien do cua rieng dau viec nay khong (PM/Quan ly
  /// To HOAC chinh nguoi thuc hien) — CHI duoc backend tra dung cho luong Checklist (xem
  /// ChecklistApiController.Index). Cac luong khac dung TaskItem (Dashboard, Cong viec cua toi...)
  /// KHONG set truong nay nen mac dinh false — cac man do khong doc truong nay nen khong anh huong.
  final bool canEdit;

  /// Ngay tao dau viec — dung de sap xep moi den cu (vi du tung cot Kanban Checklist). Null neu
  /// JSON thieu/loi dinh dang, noi dung sap xep tu coi nhu cu nhat.
  final DateTime? createdAt;

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
        assigneeName: json['AssigneeName'] as String?,
        progress: json['Progress'] as int? ?? 0,
        kind: json['Kind'] as String? ?? '',
        parentId: json['ParentId'] as int? ?? 0,
        canEdit: json['CanEdit'] as bool? ?? false,
        createdAt: parseAspNetDate(json['CreatedAt'] as String?),
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

class DailyLogTime {
  const DailyLogTime({
    required this.date,
    required this.dayOfWeek,
    required this.hours,
    required this.isToday,
  });

  final DateTime? date;
  final String dayOfWeek;
  final double hours;
  final bool isToday;

  factory DailyLogTime.fromJson(Map<String, dynamic> json) => DailyLogTime(
        date: parseAspNetDate(json['Date'] as String?),
        dayOfWeek: json['DayOfWeek'] as String? ?? '',
        hours: (json['Hours'] as num?)?.toDouble() ?? 0.0,
        isToday: json['IsToday'] as bool? ?? false,
      );
}

class WorkTimeDashboard {
  const WorkTimeDashboard({
    required this.totalHoursWeek,
    required this.totalHoursMonth,
    required this.todayHours,
    required this.targetHoursPerDay,
    required this.maxHoursPerDay,
    required this.dailyLogs,
  });

  final double totalHoursWeek;
  final double totalHoursMonth;
  final double todayHours;
  final double targetHoursPerDay;
  final double maxHoursPerDay;
  final List<DailyLogTime> dailyLogs;

  factory WorkTimeDashboard.fromJson(Map<String, dynamic> json) =>
      WorkTimeDashboard(
        totalHoursWeek: (json['TotalHoursWeek'] as num?)?.toDouble() ?? 0.0,
        totalHoursMonth: (json['TotalHoursMonth'] as num?)?.toDouble() ?? 0.0,
        todayHours: (json['TodayHours'] as num?)?.toDouble() ?? 0.0,
        targetHoursPerDay:
            (json['TargetHoursPerDay'] as num?)?.toDouble() ?? 8.0,
        maxHoursPerDay: (json['MaxHoursPerDay'] as num?)?.toDouble() ?? 12.0,
        dailyLogs: (json['DailyLogs'] as List<dynamic>? ?? [])
            .map((e) => DailyLogTime.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  /// Khởi tạo dữ liệu mặc định 7 ngày (T2 - CN) khi server chưa trả WorkTime
  factory WorkTimeDashboard.fallback() {
    final now = DateTime.now();
    final monday = now.subtract(Duration(days: (now.weekday - 1) % 7));
    const dayNames = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];
    final logs = List.generate(7, (i) {
      final date = monday.add(Duration(days: i));
      final isToday = date.year == now.year &&
          date.month == now.month &&
          date.day == now.day;
      return DailyLogTime(
        date: date,
        dayOfWeek: dayNames[i],
        hours: 0.0,
        isToday: isToday,
      );
    });

    return WorkTimeDashboard(
      totalHoursWeek: 0.0,
      totalHoursMonth: 0.0,
      todayHours: 0.0,
      targetHoursPerDay: 8.0,
      maxHoursPerDay: 12.0,
      dailyLogs: logs,
    );
  }
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
    this.workTime,
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
  final WorkTimeDashboard? workTime;
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
        workTime: json['WorkTime'] == null
            ? WorkTimeDashboard.fallback()
            : WorkTimeDashboard.fromJson(
                json['WorkTime'] as Map<String, dynamic>),
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
