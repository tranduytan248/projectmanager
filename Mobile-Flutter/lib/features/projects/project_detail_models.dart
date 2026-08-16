import '../dashboard/dashboard_models.dart' show TaskItem, parseAspNetDate;

/// Mot giai doan tham gia du an cua mot nguoi — khop ProjectMemberDto. [id] la Id cua
/// WorkAssignment, can de goi SetPm/EndMember/RemoveMember (man "Nhan su du an", xem
/// project_members_screen.dart).
class ProjectMember {
  const ProjectMember({
    required this.id,
    required this.userFullName,
    required this.isPm,
    required this.role,
    required this.phase,
    required this.joinedAt,
    required this.leftAt,
    required this.isActive,
    required this.note,
  });

  final int id;
  final String userFullName;
  final bool isPm;
  final String? role;
  final String phase;
  final DateTime? joinedAt;
  final DateTime? leftAt;
  final bool isActive;
  final String? note;

  factory ProjectMember.fromJson(Map<String, dynamic> json) => ProjectMember(
        id: json['Id'] as int? ?? 0,
        userFullName: json['UserFullName'] as String? ?? '',
        isPm: json['IsPm'] as bool? ?? false,
        role: json['Role'] as String?,
        phase: json['Phase'] as String? ?? '',
        joinedAt: parseAspNetDate(json['JoinedAt'] as String?),
        leftAt: parseAspNetDate(json['LeftAt'] as String?),
        isActive: json['IsActive'] as bool? ?? false,
        note: json['Note'] as String?,
      );
}

/// Mot lua chon nguoi dung cho o "Nguoi dung" khi them nhan su — khop AssigneeOptionDto.
class UserOption {
  const UserOption({required this.userId, required this.fullName});

  final int userId;
  final String fullName;

  factory UserOption.fromJson(Map<String, dynamic> json) => UserOption(
        userId: json['UserId'] as int? ?? 0,
        fullName: json['FullName'] as String? ?? '',
      );
}

/// Du lieu can de mo form "Them nhan su vao du an" — khop ProjectMemberFormOptionsDto.
class ProjectMemberFormOptions {
  const ProjectMemberFormOptions({required this.users, required this.roles});

  final List<UserOption> users;
  final List<String> roles;

  factory ProjectMemberFormOptions.fromJson(Map<String, dynamic> json) =>
      ProjectMemberFormOptions(
        users: (json['Users'] as List<dynamic>? ?? [])
            .map((e) => UserOption.fromJson(e as Map<String, dynamic>))
            .toList(),
        roles: (json['Roles'] as List<dynamic>? ?? [])
            .map((e) => e as String)
            .toList(),
      );
}

class WeekReportSummary {
  const WeekReportSummary({
    required this.year,
    required this.week,
    required this.submittedAt,
    required this.isSubmitted,
    required this.isOnTime,
  });

  final int year;
  final int week;
  final DateTime? submittedAt;
  final bool isSubmitted;
  final bool isOnTime;

  factory WeekReportSummary.fromJson(Map<String, dynamic> json) => WeekReportSummary(
        year: json['Year'] as int? ?? 0,
        week: json['Week'] as int? ?? 0,
        submittedAt: parseAspNetDate(json['SubmittedAt'] as String?),
        isSubmitted: json['IsSubmitted'] as bool? ?? false,
        isOnTime: json['IsOnTime'] as bool? ?? false,
      );
}

class ProjectDetail {
  const ProjectDetail({
    required this.id,
    required this.code,
    required this.name,
    required this.customer,
    required this.projectType,
    required this.phase,
    required this.state,
    required this.isOpen,
    required this.pmName,
    required this.startDate,
    required this.endDate,
    required this.description,
    required this.canEdit,
    required this.activeMemberCount,
    required this.pastMemberCount,
    required this.checklistDone,
    required this.checklistTotal,
    required this.checklistOverdue,
    required this.supportThisWeek,
    required this.members,
    required this.overdueTasks,
    required this.currentReport,
    required this.recentReports,
  });

  final int id;
  final String? code;
  final String name;
  final String? customer;
  final String? projectType;
  final String phase;
  final String state;
  final bool isOpen;
  final String? pmName;
  final DateTime? startDate;
  final DateTime? endDate;
  final String description;
  final bool canEdit;
  final int activeMemberCount;
  final int pastMemberCount;
  final int checklistDone;
  final int checklistTotal;
  final int checklistOverdue;
  final int supportThisWeek;
  final List<ProjectMember> members;
  final List<TaskItem> overdueTasks;
  final WeekReportSummary? currentReport;
  final List<WeekReportSummary> recentReports;

  factory ProjectDetail.fromJson(Map<String, dynamic> json) => ProjectDetail(
        id: json['Id'] as int,
        code: json['Code'] as String?,
        name: json['Name'] as String? ?? '',
        customer: json['Customer'] as String?,
        projectType: json['ProjectType'] as String?,
        phase: json['Phase'] as String? ?? '',
        state: json['State'] as String? ?? '',
        isOpen: json['IsOpen'] as bool? ?? true,
        pmName: json['PmName'] as String?,
        startDate: parseAspNetDate(json['StartDate'] as String?),
        endDate: parseAspNetDate(json['EndDate'] as String?),
        description: json['Description'] as String? ?? '',
        canEdit: json['CanEdit'] as bool? ?? false,
        activeMemberCount: json['ActiveMemberCount'] as int? ?? 0,
        pastMemberCount: json['PastMemberCount'] as int? ?? 0,
        checklistDone: json['ChecklistDone'] as int? ?? 0,
        checklistTotal: json['ChecklistTotal'] as int? ?? 0,
        checklistOverdue: json['ChecklistOverdue'] as int? ?? 0,
        supportThisWeek: json['SupportThisWeek'] as int? ?? 0,
        members: (json['Members'] as List<dynamic>? ?? [])
            .map((e) => ProjectMember.fromJson(e as Map<String, dynamic>))
            .toList(),
        overdueTasks: (json['OverdueTasks'] as List<dynamic>? ?? [])
            .map((e) => TaskItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        currentReport: json['CurrentReport'] == null
            ? null
            : WeekReportSummary.fromJson(json['CurrentReport'] as Map<String, dynamic>),
        recentReports: (json['RecentReports'] as List<dynamic>? ?? [])
            .map((e) => WeekReportSummary.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}
