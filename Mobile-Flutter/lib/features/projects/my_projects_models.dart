import '../dashboard/dashboard_models.dart' show parseAspNetDate;

class MyProjectRow {
  const MyProjectRow({
    required this.id,
    required this.code,
    required this.name,
    required this.customer,
    required this.pmName,
    required this.phase,
    required this.state,
    required this.isOpen,
    required this.isPm,
    required this.isActiveMember,
    required this.role,
    required this.joinedAt,
    required this.leftAt,
    required this.checklistDone,
    required this.checklistTotal,
    required this.checklistPercent,
    required this.myOpenCount,
    required this.myOverdueCount,
    required this.reportStatus,
  });

  final int id;
  final String? code;
  final String name;
  final String? customer;
  final String? pmName;
  final String phase;
  final String state;
  final bool isOpen;
  final bool isPm;
  final bool isActiveMember;
  final String? role;
  final DateTime? joinedAt;
  final DateTime? leftAt;
  final int checklistDone;
  final int checklistTotal;
  final int checklistPercent;
  final int myOpenCount;
  final int myOverdueCount;

  /// "DaNopDungHan" | "DaNopTreHan" | "ConBanNhap" | "ChuaNop" | "KhongApDung"
  final String reportStatus;

  factory MyProjectRow.fromJson(Map<String, dynamic> json) => MyProjectRow(
        id: json['Id'] as int,
        code: json['Code'] as String?,
        name: json['Name'] as String? ?? '',
        customer: json['Customer'] as String?,
        pmName: json['PmName'] as String?,
        phase: json['Phase'] as String? ?? '',
        state: json['State'] as String? ?? '',
        isOpen: json['IsOpen'] as bool? ?? true,
        isPm: json['IsPm'] as bool? ?? false,
        isActiveMember: json['IsActiveMember'] as bool? ?? false,
        role: json['Role'] as String?,
        joinedAt: parseAspNetDate(json['JoinedAt'] as String?),
        leftAt: parseAspNetDate(json['LeftAt'] as String?),
        checklistDone: json['ChecklistDone'] as int? ?? 0,
        checklistTotal: json['ChecklistTotal'] as int? ?? 0,
        checklistPercent: json['ChecklistPercent'] as int? ?? 0,
        myOpenCount: json['MyOpenCount'] as int? ?? 0,
        myOverdueCount: json['MyOverdueCount'] as int? ?? 0,
        reportStatus: json['ReportStatus'] as String? ?? 'KhongApDung',
      );
}

class MyProjectsData {
  const MyProjectsData({
    required this.totalCount,
    required this.pmCount,
    required this.closedCount,
    required this.projects,
  });

  final int totalCount;
  final int pmCount;
  final int closedCount;
  final List<MyProjectRow> projects;

  factory MyProjectsData.fromJson(Map<String, dynamic> json) => MyProjectsData(
        totalCount: json['TotalCount'] as int? ?? 0,
        pmCount: json['PmCount'] as int? ?? 0,
        closedCount: json['ClosedCount'] as int? ?? 0,
        projects: (json['Projects'] as List<dynamic>? ?? [])
            .map((e) => MyProjectRow.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

/// Khop ProjectPhases.Display ben backend.
String projectPhaseLabel(String phase) {
  switch (phase) {
    case 'TrienKhai':
      return 'Triển khai';
    case 'HoTro':
      return 'Hỗ trợ';
    default:
      return phase;
  }
}

/// Khop ProjectStates.Display ben backend (Models/Work/WorkProject.cs).
String projectStateLabel(String state) {
  switch (state) {
    case 'ChuanBi':
      return 'Chuẩn bị';
    case 'DangThucHien':
      return 'Đang thực hiện';
    case 'DangThuNghiem':
      return 'Đang thử nghiệm';
    case 'TamDungTrienKhai':
      return 'Tạm dừng triển khai';
    case 'DaNghiemThu':
      return 'Đã nghiệm thu';
    case 'DangHoTro':
      return 'Đang hỗ trợ';
    case 'TamDungHoTro':
      return 'Tạm dừng hỗ trợ';
    case 'KetThucHoTro':
      return 'Kết thúc hỗ trợ';
    case 'Huy':
      return 'Huỷ';
    default:
      return state;
  }
}
