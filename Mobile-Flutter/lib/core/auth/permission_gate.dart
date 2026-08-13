import 'package:flutter/widgets.dart';

/// An/hien widget con dua theo danh sach quyen dang `module.action` cua user,
/// tuong ung co che RoleGroup o backend (Models/RoleGroup.cs, Models/Permission.cs).
/// Nhom quyen mang "*" la toan quyen, giong quy uoc hien co o web.
class PermissionGate extends StatelessWidget {
  const PermissionGate({
    super.key,
    required this.permissions,
    required this.required,
    required this.child,
    this.fallback = const SizedBox.shrink(),
  });

  final List<String> permissions;
  final String required;
  final Widget child;
  final Widget fallback;

  bool get _allowed => permissions.contains('*') || permissions.contains(required);

  @override
  Widget build(BuildContext context) => _allowed ? child : fallback;
}
