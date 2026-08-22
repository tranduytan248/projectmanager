import 'dart:ui';
import '../../core/theme/app_colors.dart';

class KpiSummaryData {
  final int total;
  final double averagePoint;
  final int passCount;
  final int passPercent;
  final int failCount;
  final int shortHoursCount;
  final String topName;
  final double topPoint;
  final int standardDays;
  final double scaleMax;

  const KpiSummaryData({
    required this.total,
    required this.averagePoint,
    required this.passCount,
    required this.passPercent,
    required this.failCount,
    required this.shortHoursCount,
    required this.topName,
    required this.topPoint,
    required this.standardDays,
    required this.scaleMax,
  });

  factory KpiSummaryData.fromJson(Map<String, dynamic> json) {
    return KpiSummaryData(
      total: json['total'] is num ? (json['total'] as num).toInt() : (json['Total'] as num?)?.toInt() ?? 0,
      averagePoint: json['averagePoint'] is num
          ? (json['averagePoint'] as num).toDouble()
          : (json['AveragePoint'] as num?)?.toDouble() ?? 0.0,
      passCount: json['passCount'] is num ? (json['passCount'] as num).toInt() : (json['PassCount'] as num?)?.toInt() ?? 0,
      passPercent: json['passPercent'] is num ? (json['passPercent'] as num).toInt() : (json['PassPercent'] as num?)?.toInt() ?? 0,
      failCount: json['failCount'] is num ? (json['failCount'] as num).toInt() : (json['FailCount'] as num?)?.toInt() ?? 0,
      shortHoursCount:
          json['shortHoursCount'] is num ? (json['shortHoursCount'] as num).toInt() : (json['ShortHoursCount'] as num?)?.toInt() ?? 0,
      topName: (json['topName'] ?? json['TopName'] ?? '').toString(),
      topPoint: json['topPoint'] is num
          ? (json['topPoint'] as num).toDouble()
          : (json['TopPoint'] as num?)?.toDouble() ?? 0.0,
      standardDays: json['standardDays'] is num ? (json['standardDays'] as num).toInt() : (json['StandardDays'] as num?)?.toInt() ?? 22,
      scaleMax: json['scaleMax'] is num
          ? (json['scaleMax'] as num).toDouble()
          : (json['ScaleMax'] as num?)?.toDouble() ?? 100.0,
    );
  }
}

class KpiMemberRow {
  final int id;
  final int userId;
  final String fullName;
  final int year;
  final int month;
  final double supportPoint;
  final double supportGrossPoint;
  final double supportHours;
  final double supportCapHours;
  final double executePoint;
  final double executeGrossPoint;
  final double executeHours;
  final double executeTargetHours;
  final double assignedPoint;
  final double latePenalty;
  final double qualityPoint;
  final int standardDays;
  final double leaveDays;
  final double requiredHours;
  final double workedHours;
  final int attendanceRate;
  final double finalPoint;
  final String rank;
  final bool isSaved;
  final int rankIndex;

  const KpiMemberRow({
    required this.id,
    required this.userId,
    required this.fullName,
    required this.year,
    required this.month,
    required this.supportPoint,
    required this.supportGrossPoint,
    required this.supportHours,
    required this.supportCapHours,
    required this.executePoint,
    required this.executeGrossPoint,
    required this.executeHours,
    required this.executeTargetHours,
    required this.assignedPoint,
    required this.latePenalty,
    required this.qualityPoint,
    required this.standardDays,
    required this.leaveDays,
    required this.requiredHours,
    required this.workedHours,
    required this.attendanceRate,
    required this.finalPoint,
    required this.rank,
    required this.isSaved,
    required this.rankIndex,
  });

  bool get isShortHours => attendanceRate < 100;
  double get hoursShort => (requiredHours - workedHours) > 0 ? (requiredHours - workedHours) : 0;

  Color get rankColor {
    switch (rank) {
      case 'Xuất sắc':
        return AppColors.success;
      case 'Tốt':
        return AppColors.primary;
      case 'Khá':
        return AppColors.info;
      case 'Đạt':
        return AppColors.warning;
      case 'Chưa đạt':
      default:
        return AppColors.danger;
    }
  }

  Color get rankBgColor {
    switch (rank) {
      case 'Xuất sắc':
        return AppColors.successSoft;
      case 'Tốt':
        return AppColors.primarySoft;
      case 'Khá':
        return AppColors.infoSoft;
      case 'Đạt':
        return AppColors.warningSoft;
      case 'Chưa đạt':
      default:
        return AppColors.dangerSoft;
    }
  }

  factory KpiMemberRow.fromJson(Map<String, dynamic> json) {
    num getNum(String k1, String k2) =>
        json[k1] is num ? (json[k1] as num) : ((json[k2] as num?) ?? 0);

    return KpiMemberRow(
      id: getNum('id', 'Id').toInt(),
      userId: getNum('userId', 'UserId').toInt(),
      fullName: (json['fullName'] ?? json['FullName'] ?? '').toString(),
      year: getNum('year', 'Year').toInt(),
      month: getNum('month', 'Month').toInt(),
      supportPoint: getNum('supportPoint', 'SupportPoint').toDouble(),
      supportGrossPoint: getNum('supportGrossPoint', 'SupportGrossPoint').toDouble(),
      supportHours: getNum('supportHours', 'SupportHours').toDouble(),
      supportCapHours: getNum('supportCapHours', 'SupportCapHours').toDouble(),
      executePoint: getNum('executePoint', 'ExecutePoint').toDouble(),
      executeGrossPoint: getNum('executeGrossPoint', 'ExecuteGrossPoint').toDouble(),
      executeHours: getNum('executeHours', 'ExecuteHours').toDouble(),
      executeTargetHours: getNum('executeTargetHours', 'ExecuteTargetHours').toDouble(),
      assignedPoint: getNum('assignedPoint', 'AssignedPoint').toDouble(),
      latePenalty: getNum('latePenalty', 'LatePenalty').toDouble(),
      qualityPoint: getNum('qualityPoint', 'QualityPoint').toDouble(),
      standardDays: getNum('standardDays', 'StandardDays').toInt(),
      leaveDays: getNum('leaveDays', 'LeaveDays').toDouble(),
      requiredHours: getNum('requiredHours', 'RequiredHours').toDouble(),
      workedHours: getNum('workedHours', 'WorkedHours').toDouble(),
      attendanceRate: getNum('attendanceRate', 'AttendanceRate').toInt(),
      finalPoint: getNum('finalPoint', 'FinalPoint').toDouble(),
      rank: (json['rank'] ?? json['Rank'] ?? 'Chưa đạt').toString(),
      isSaved: json['isSaved'] == true || json['IsSaved'] == true,
      rankIndex: getNum('rankIndex', 'RankIndex').toInt(),
    );
  }
}

class KpiUserOption {
  final int userId;
  final String fullName;

  const KpiUserOption({required this.userId, required this.fullName});

  factory KpiUserOption.fromJson(Map<String, dynamic> json) {
    return KpiUserOption(
      userId: json['userId'] is num ? (json['userId'] as num).toInt() : (json['UserId'] as num?)?.toInt() ?? 0,
      fullName: (json['fullName'] ?? json['FullName'] ?? '').toString(),
    );
  }
}

class KpiIndexData {
  final int year;
  final int month;
  final bool isCurrentMonth;
  final int selectedUserId;
  final KpiSummaryData summary;
  final List<KpiMemberRow> rows;
  final List<KpiUserOption> users;
  final bool canGenerate;
  final bool canConfig;
  final int standardDays;
  final double scaleMax;

  const KpiIndexData({
    required this.year,
    required this.month,
    required this.isCurrentMonth,
    required this.selectedUserId,
    required this.summary,
    required this.rows,
    required this.users,
    required this.canGenerate,
    required this.canConfig,
    required this.standardDays,
    required this.scaleMax,
  });

  factory KpiIndexData.fromJson(Map<String, dynamic> json) {
    final rawRows = (json['rows'] ?? json['Rows']) as List<dynamic>? ?? [];
    final rawUsers = (json['users'] ?? json['Users']) as List<dynamic>? ?? [];
    final rawSummary = json['summary'] ?? json['Summary'] ?? {};

    return KpiIndexData(
      year: json['year'] is num ? (json['year'] as num).toInt() : (json['Year'] as num?)?.toInt() ?? DateTime.now().year,
      month: json['month'] is num ? (json['month'] as num).toInt() : (json['Month'] as num?)?.toInt() ?? DateTime.now().month,
      isCurrentMonth: json['isCurrentMonth'] == true || json['IsCurrentMonth'] == true,
      selectedUserId:
          json['selectedUserId'] is num ? (json['selectedUserId'] as num).toInt() : (json['SelectedUserId'] as num?)?.toInt() ?? 0,
      summary: KpiSummaryData.fromJson(rawSummary is Map ? Map<String, dynamic>.from(rawSummary) : {}),
      rows: rawRows.map((e) => KpiMemberRow.fromJson(e is Map ? Map<String, dynamic>.from(e) : {})).toList(),
      users: rawUsers.map((e) => KpiUserOption.fromJson(e is Map ? Map<String, dynamic>.from(e) : {})).toList(),
      canGenerate: json['canGenerate'] == true || json['CanGenerate'] == true,
      canConfig: json['canConfig'] == true || json['CanConfig'] == true,
      standardDays: json['standardDays'] is num ? (json['standardDays'] as num).toInt() : (json['StandardDays'] as num?)?.toInt() ?? 22,
      scaleMax: json['scaleMax'] is num
          ? (json['scaleMax'] as num).toDouble()
          : (json['ScaleMax'] as num?)?.toDouble() ?? 100.0,
    );
  }
}

class KpiTaskItem {
  final int id;
  final String title;
  final int projectId;
  final String projectName;
  final String kind;
  final String state;
  final int progress;
  final DateTime? startDate;
  final DateTime? dueDate;
  final DateTime? completedAt;
  final bool isOverdue;
  final double loggedHours;
  final double estimatedHours;
  final bool isLate;

  const KpiTaskItem({
    required this.id,
    required this.title,
    required this.projectId,
    required this.projectName,
    required this.kind,
    required this.state,
    required this.progress,
    this.startDate,
    this.dueDate,
    this.completedAt,
    required this.isOverdue,
    required this.loggedHours,
    required this.estimatedHours,
    required this.isLate,
  });

  factory KpiTaskItem.fromJson(Map<String, dynamic> json) {
    DateTime? parseDate(dynamic v) {
      if (v == null) return null;
      try {
        return DateTime.parse(v.toString());
      } catch (_) {
        return null;
      }
    }

    num getNum(String k1, String k2) =>
        json[k1] is num ? (json[k1] as num) : ((json[k2] as num?) ?? 0);

    return KpiTaskItem(
      id: getNum('id', 'Id').toInt(),
      title: (json['title'] ?? json['Title'] ?? '').toString(),
      projectId: getNum('projectId', 'ProjectId').toInt(),
      projectName: (json['projectName'] ?? json['ProjectName'] ?? '').toString(),
      kind: (json['kind'] ?? json['Kind'] ?? '').toString(),
      state: (json['state'] ?? json['State'] ?? '').toString(),
      progress: getNum('progress', 'Progress').toInt(),
      startDate: parseDate(json['startDate'] ?? json['StartDate']),
      dueDate: parseDate(json['dueDate'] ?? json['DueDate']),
      completedAt: parseDate(json['completedAt'] ?? json['CompletedAt']),
      isOverdue: json['isOverdue'] == true || json['IsOverdue'] == true,
      loggedHours: getNum('loggedHours', 'LoggedHours').toDouble(),
      estimatedHours: getNum('estimatedHours', 'EstimatedHours').toDouble(),
      isLate: json['isLate'] == true || json['IsLate'] == true,
    );
  }
}

class KpiDetailData {
  final KpiMemberRow row;
  final List<KpiTaskItem> supportTasks;
  final List<KpiTaskItem> executeTasks;
  final List<KpiTaskItem> assignedTasks;
  final bool canGenerate;
  final bool canConfig;

  const KpiDetailData({
    required this.row,
    required this.supportTasks,
    required this.executeTasks,
    required this.assignedTasks,
    required this.canGenerate,
    required this.canConfig,
  });

  factory KpiDetailData.fromJson(Map<String, dynamic> json) {
    final rawRow = json['row'] ?? json['Row'] ?? {};
    final rawSupport = (json['supportTasks'] ?? json['SupportTasks']) as List<dynamic>? ?? [];
    final rawExecute = (json['executeTasks'] ?? json['ExecuteTasks']) as List<dynamic>? ?? [];
    final rawAssigned = (json['assignedTasks'] ?? json['AssignedTasks']) as List<dynamic>? ?? [];

    return KpiDetailData(
      row: KpiMemberRow.fromJson(rawRow is Map ? Map<String, dynamic>.from(rawRow) : {}),
      supportTasks: rawSupport.map((e) => KpiTaskItem.fromJson(e is Map ? Map<String, dynamic>.from(e) : {})).toList(),
      executeTasks: rawExecute.map((e) => KpiTaskItem.fromJson(e is Map ? Map<String, dynamic>.from(e) : {})).toList(),
      assignedTasks: rawAssigned.map((e) => KpiTaskItem.fromJson(e is Map ? Map<String, dynamic>.from(e) : {})).toList(),
      canGenerate: json['canGenerate'] == true || json['CanGenerate'] == true,
      canConfig: json['canConfig'] == true || json['CanConfig'] == true,
    );
  }
}
