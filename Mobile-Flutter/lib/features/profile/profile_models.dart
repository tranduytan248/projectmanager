import '../dashboard/dashboard_models.dart' show parseAspNetDate;

/// Man "Thong tin ca nhan" — khop ProfileDto ben backend (Models/Api/ApiDtos.cs), tuong duong
/// ProfilePageViewModel cua Account/Profile ben web.
class ProfileInfo {
  const ProfileInfo({
    required this.userName,
    required this.fullName,
    required this.roleDisplay,
    required this.isAdmin,
    required this.createdAt,
  });

  final String userName;
  final String fullName;
  final String roleDisplay;
  final bool isAdmin;
  final DateTime? createdAt;

  factory ProfileInfo.fromJson(Map<String, dynamic> json) => ProfileInfo(
        userName: json['UserName'] as String? ?? '',
        fullName: json['FullName'] as String? ?? '',
        roleDisplay: json['RoleDisplay'] as String? ?? '',
        isAdmin: json['IsAdmin'] as bool? ?? false,
        createdAt: parseAspNetDate(json['CreatedAt'] as String?),
      );
}
