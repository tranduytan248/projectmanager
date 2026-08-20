import '../dashboard/dashboard_models.dart' show parseAspNetDate;

/// Man "Thong tin ca nhan" — khop ProfileDto ben backend (Models/Api/ApiDtos.cs), tuong duong
/// ProfilePageViewModel cua Account/Profile ben web.
class ProfileInfo {
  const ProfileInfo({
    required this.userName,
    required this.fullName,
    required this.roleDisplay,
    required this.isTeamManager,
    required this.isAdmin,
    required this.createdAt,
  });

  final String userName;
  final String fullName;
  final String roleDisplay;

  /// Vai Quan ly To — nam NGOAI (cac) nhom quyen o [roleDisplay], hien them mot the rieng
  /// "Quan ly To" giong het cach Views/Users/Index.cshtml ben web dang hien (badge rieng, khong
  /// gop chung vao chuoi ten nhom).
  final bool isTeamManager;
  final bool isAdmin;
  final DateTime? createdAt;

  factory ProfileInfo.fromJson(Map<String, dynamic> json) => ProfileInfo(
        userName: json['UserName'] as String? ?? '',
        fullName: json['FullName'] as String? ?? '',
        roleDisplay: json['RoleDisplay'] as String? ?? '',
        isTeamManager: json['IsTeamManager'] as bool? ?? false,
        isAdmin: json['IsAdmin'] as bool? ?? false,
        createdAt: parseAspNetDate(json['CreatedAt'] as String?),
      );
}
