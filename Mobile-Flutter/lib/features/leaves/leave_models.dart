// Model + tien ich cho man "Nghi phep cua toi" (danh sach + form dang ky/sua) — khop
// Controllers/Api/LeavesApiController.cs va Models/Work/LeaveRequest.cs ben backend.

import '../dashboard/dashboard_models.dart' show parseAspNetDate;

/// Cac gia tri "Loai nghi" — khop LeaveKinds ben backend.
class LeaveKinds {
  LeaveKinds._();

  static const annual = 'PhepNam';
  static const unpaid = 'KhongLuong';
  static const sick = 'Om';
  static const other = 'Khac';

  static const all = [annual, unpaid, sick, other];
}

/// Khop LeaveKinds.Display ben backend — KHONG duoc lech chinh ta so voi ban goc.
String leaveKindLabel(String kind) {
  switch (kind) {
    case LeaveKinds.annual:
      return 'Phép năm';
    case LeaveKinds.unpaid:
      return 'Nghỉ không lương';
    case LeaveKinds.sick:
      return 'Nghỉ ốm';
    case LeaveKinds.other:
      return 'Khác';
    default:
      return kind;
  }
}

/// Cac gia tri "Trang thai duyet" — khop LeaveStates ben backend.
class LeaveStates {
  LeaveStates._();

  static const pending = 'ChoDuyet';
  static const approved = 'DaDuyet';
  static const rejected = 'TuChoi';
  static const cancelled = 'DaHuy';

  static const all = [pending, approved, rejected, cancelled];
}

/// Khop LeaveStates.Display ben backend.
String leaveStateLabel(String state) {
  switch (state) {
    case LeaveStates.pending:
      return 'Chờ duyệt';
    case LeaveStates.approved:
      return 'Đã duyệt';
    case LeaveStates.rejected:
      return 'Từ chối';
    case LeaveStates.cancelled:
      return 'Đã huỷ';
    default:
      return state;
  }
}

/// Don con sua/thu hoi duoc khong — khop LeaveStates.IsEditable ben backend (chi khi Cho duyet).
bool leaveIsEditable(String state) => state == LeaveStates.pending;

/// Hai gia tri "Buoi" khi nghi nua ngay — tu dat (backend chi luu de hien thi, khong co danh muc
/// rieng), khop dung field HalfDaySession gui/nhan qua API.
class HalfDaySessions {
  HalfDaySessions._();

  static const morning = 'Sang';
  static const afternoon = 'Chieu';
}

String halfDaySessionLabel(String? session) {
  switch (session) {
    case HalfDaySessions.morning:
      return 'Buổi sáng';
    case HalfDaySessions.afternoon:
      return 'Buổi chiều';
    default:
      return session ?? '';
  }
}

/// Dinh dang so ngay nghi, cat bot so 0 thua o cuoi (vi du 3.0 -> "3", 0.5 -> "0.5") — giong dinh
/// dang "{0:0.#}" ben backend (xem LeavesApiController.NotifyApprovers).
String formatLeaveDays(double days) {
  var text = days.toStringAsFixed(1);
  if (text.endsWith('.0')) text = text.substring(0, text.length - 2);
  return text;
}

/// Tinh so ngay nghi PREVIEW o client — server van tinh lai va la nguon su that cuoi cung, o day
/// chi de nguoi dung thay ngay khi chon ngay/tick nua ngay. Khop dung LeaveRequest.DaysWithin ben
/// backend: dem ngay lam viec trong khoang [from, to] (CA HAI dau), bo Thu 7 va Chu nhat; neu tick
/// nghi nua ngay va con it nhat 1 ngay lam viec trong khoang thi la 0.5 (khong chia doi so dem
/// duoc).
double calcLeaveDays({
  required DateTime from,
  required DateTime to,
  required bool isHalfDay,
}) {
  final start = DateTime(from.year, from.month, from.day);
  final end = DateTime(to.year, to.month, to.day);
  if (start.isAfter(end)) return 0;

  var count = 0;
  for (var d = start; !d.isAfter(end); d = d.add(const Duration(days: 1))) {
    if (d.weekday != DateTime.saturday && d.weekday != DateTime.sunday) count++;
  }
  if (count == 0) return 0;

  return isHalfDay ? 0.5 : count.toDouble();
}

/// Mot don nghi phep — khop LeaveRequestDto ben backend (Models/Api/ApiDtos.cs).
class LeaveRequestItem {
  const LeaveRequestItem({
    required this.id,
    required this.kind,
    required this.fromDate,
    required this.toDate,
    required this.isHalfDay,
    required this.halfDaySession,
    this.userId = 0,
    this.userFullName,
    required this.days,
    required this.reason,
    required this.state,
    required this.approvedByName,
    required this.approvedAt,
    required this.approverNote,
    required this.createdAt,
  });

  final int id;
  final int userId;
  final String? userFullName;

  /// Gia tri tho cua LeaveKinds (PhepNam/KhongLuong/Om/Khac) — dung [leaveKindLabel] de hien thi.
  final String kind;

  final DateTime fromDate;
  final DateTime toDate;
  final bool isHalfDay;
  final String? halfDaySession;
  final double days;
  final String? reason;

  /// Gia tri tho cua LeaveStates (ChoDuyet/DaDuyet/TuChoi/DaHuy) — dung [leaveStateLabel] de hien
  /// thi, [leaveIsEditable] de biet con sua/thu hoi duoc khong.
  final String state;

  final String? approvedByName;
  final DateTime? approvedAt;
  final String? approverNote;
  final DateTime createdAt;

  factory LeaveRequestItem.fromJson(Map<String, dynamic> json) => LeaveRequestItem(
        id: json['Id'] as int,
        userId: json['UserId'] as int? ?? 0,
        userFullName: json['UserFullName'] as String?,
        kind: json['Kind'] as String? ?? LeaveKinds.annual,
        fromDate: parseAspNetDate(json['FromDate'] as String?) ?? DateTime.now(),
        toDate: parseAspNetDate(json['ToDate'] as String?) ?? DateTime.now(),
        isHalfDay: json['IsHalfDay'] as bool? ?? false,
        halfDaySession: json['HalfDaySession'] as String?,
        days: (json['Days'] as num?)?.toDouble() ?? 0,
        reason: json['Reason'] as String?,
        state: json['State'] as String? ?? LeaveStates.pending,
        approvedByName: json['ApprovedByName'] as String?,
        approvedAt: parseAspNetDate(json['ApprovedAt'] as String?),
        approverNote: json['ApproverNote'] as String?,
        createdAt: parseAspNetDate(json['CreatedAt'] as String?) ?? DateTime.now(),
      );
}

/// Danh sach "Nghi phep cua toi" — khop LeavesListDto ben backend.
class LeavesData {
  const LeavesData({
    required this.statYear,
    required this.approvedDaysInYear,
    required this.pendingCount,
    required this.years,
    required this.items,
  });

  final int statYear;
  final double approvedDaysInYear;
  final int pendingCount;

  /// Cac nam co don, moi cho dropdown loc — giam dan.
  final List<int> years;

  final List<LeaveRequestItem> items;

  factory LeavesData.fromJson(Map<String, dynamic> json) => LeavesData(
        statYear: json['StatYear'] as int? ?? DateTime.now().year,
        approvedDaysInYear: (json['ApprovedDaysInYear'] as num?)?.toDouble() ?? 0,
        pendingCount: json['PendingCount'] as int? ?? 0,
        years: (json['Years'] as List<dynamic>? ?? []).map((e) => e as int).toList(),
        items: (json['Items'] as List<dynamic>? ?? [])
            .map((e) => LeaveRequestItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

/// Tuy chon nhan su cho bo loc Duyet nghi phep.
class LeaveMemberOption {
  const LeaveMemberOption({
    required this.userId,
    required this.fullName,
  });

  final int userId;
  final String fullName;

  factory LeaveMemberOption.fromJson(Map<String, dynamic> json) => LeaveMemberOption(
        userId: json['UserId'] as int? ?? 0,
        fullName: json['FullName'] as String? ?? '',
      );
}

/// Danh sach "Duyet nghi phep" toan To — khop LeaveApprovalsDataDto ben backend.
class LeaveApprovalsData {
  const LeaveApprovalsData({
    required this.totalCount,
    required this.pendingCount,
    required this.approvedCount,
    required this.rejectedCount,
    required this.years,
    required this.members,
    required this.items,
  });

  final int totalCount;
  final int pendingCount;
  final int approvedCount;
  final int rejectedCount;
  final List<int> years;
  final List<LeaveMemberOption> members;
  final List<LeaveRequestItem> items;

  factory LeaveApprovalsData.fromJson(Map<String, dynamic> json) => LeaveApprovalsData(
        totalCount: json['TotalCount'] as int? ?? 0,
        pendingCount: json['PendingCount'] as int? ?? 0,
        approvedCount: json['ApprovedCount'] as int? ?? 0,
        rejectedCount: json['RejectedCount'] as int? ?? 0,
        years: (json['Years'] as List<dynamic>? ?? []).map((e) => e as int).toList(),
        members: (json['Members'] as List<dynamic>? ?? [])
            .map((e) => LeaveMemberOption.fromJson(e as Map<String, dynamic>))
            .toList(),
        items: (json['Items'] as List<dynamic>? ?? [])
            .map((e) => LeaveRequestItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}
