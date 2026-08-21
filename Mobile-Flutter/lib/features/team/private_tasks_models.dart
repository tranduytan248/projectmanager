import 'package:flutter/material.dart';

import '../../core/theme/app_colors.dart';

class AssigneeOption {
  const AssigneeOption({
    required this.userId,
    required this.fullName,
  });

  final int userId;
  final String fullName;

  factory AssigneeOption.fromJson(Map<String, dynamic> json) {
    return AssigneeOption(
      userId: json['UserId'] as int? ?? json['userId'] as int? ?? 0,
      fullName: json['FullName'] as String? ?? json['fullName'] as String? ?? '',
    );
  }
}

class PrivateTaskItem {
  const PrivateTaskItem({
    required this.id,
    required this.title,
    required this.description,
    required this.state,
    required this.priority,
    required this.progress,
    required this.assigneeUserId,
    required this.assigneeName,
    required this.assignedByUserId,
    required this.assignedByName,
    this.startDate,
    this.dueDate,
    this.completedAt,
    required this.isOverdue,
    required this.bonusPercent,
    required this.hasAttachment,
    this.attachmentName,
    required this.attachmentSize,
    required this.canEdit,
    required this.canDelete,
    required this.createdAt,
  });

  final int id;
  final String title;
  final String description;
  final String state;
  final String priority;
  final int progress;
  final int assigneeUserId;
  final String assigneeName;
  final int assignedByUserId;
  final String assignedByName;
  final DateTime? startDate;
  final DateTime? dueDate;
  final DateTime? completedAt;
  final bool isOverdue;
  final double bonusPercent;
  final bool hasAttachment;
  final String? attachmentName;
  final int attachmentSize;
  final bool canEdit;
  final bool canDelete;
  final DateTime createdAt;

  factory PrivateTaskItem.fromJson(Map<String, dynamic> json) {
    DateTime? parseDate(dynamic value) {
      if (value == null) return null;
      if (value is String && value.isNotEmpty) {
        return DateTime.tryParse(value);
      }
      return null;
    }

    return PrivateTaskItem(
      id: json['Id'] as int? ?? json['id'] as int? ?? 0,
      title: json['Title'] as String? ?? json['title'] as String? ?? '',
      description: json['Description'] as String? ?? json['description'] as String? ?? '',
      state: json['State'] as String? ?? json['state'] as String? ?? 'ChuaBatDau',
      priority: json['Priority'] as String? ?? json['priority'] as String? ?? 'TrungBinh',
      progress: json['Progress'] as int? ?? json['progress'] as int? ?? 0,
      assigneeUserId: json['AssigneeUserId'] as int? ?? json['assigneeUserId'] as int? ?? 0,
      assigneeName: json['AssigneeName'] as String? ?? json['assigneeName'] as String? ?? '',
      assignedByUserId: json['AssignedByUserId'] as int? ?? json['assignedByUserId'] as int? ?? 0,
      assignedByName: json['AssignedByName'] as String? ?? json['assignedByName'] as String? ?? '',
      startDate: parseDate(json['StartDate'] ?? json['startDate']),
      dueDate: parseDate(json['DueDate'] ?? json['dueDate']),
      completedAt: parseDate(json['CompletedAt'] ?? json['completedAt']),
      isOverdue: json['IsOverdue'] as bool? ?? json['isOverdue'] as bool? ?? false,
      bonusPercent: (json['BonusPercent'] ?? json['bonusPercent'] as num?)?.toDouble() ?? 0.0,
      hasAttachment: json['HasAttachment'] as bool? ?? json['hasAttachment'] as bool? ?? false,
      attachmentName: json['AttachmentName'] as String? ?? json['attachmentName'] as String?,
      attachmentSize: (json['AttachmentSize'] ?? json['attachmentSize'] as num?)?.toInt() ?? 0,
      canEdit: json['CanEdit'] as bool? ?? json['canEdit'] as bool? ?? false,
      canDelete: json['CanDelete'] as bool? ?? json['canDelete'] as bool? ?? false,
      createdAt: parseDate(json['CreatedAt'] ?? json['createdAt']) ?? DateTime.now(),
    );
  }

  String get stateLabel {
    switch (state) {
      case 'ChuaBatDau':
        return 'Chưa làm';
      case 'DangLam':
        return 'Đang làm';
      case 'TamDung':
        return 'Tạm dừng';
      case 'HoanThanh':
        return 'Hoàn thành';
      case 'Huy':
        return 'Đã hủy';
      default:
        return state;
    }
  }

  Color get stateColor {
    switch (state) {
      case 'ChuaBatDau':
        return AppColors.textSecondary;
      case 'DangLam':
        return AppColors.primary;
      case 'TamDung':
        return AppColors.warning;
      case 'HoanThanh':
        return AppColors.success;
      case 'Huy':
        return AppColors.danger;
      default:
        return AppColors.textSecondary;
    }
  }

  Color get priorityColor {
    switch (priority) {
      case 'Cao':
        return AppColors.danger;
      case 'Thap':
        return AppColors.textSecondary;
      default:
        return AppColors.primary;
    }
  }
}

class PrivateTasksData {
  const PrivateTasksData({
    required this.totalCount,
    required this.overdueCount,
    required this.inProgressCount,
    required this.doneCount,
    required this.items,
    required this.members,
  });

  final int totalCount;
  final int overdueCount;
  final int inProgressCount;
  final int doneCount;
  final List<PrivateTaskItem> items;
  final List<AssigneeOption> members;

  factory PrivateTasksData.fromJson(Map<String, dynamic> json) {
    final rawItems = (json['Items'] ?? json['items']) as List<dynamic>? ?? [];
    final rawMembers = (json['Members'] ?? json['members']) as List<dynamic>? ?? [];

    return PrivateTasksData(
      totalCount: json['TotalCount'] as int? ?? json['totalCount'] as int? ?? 0,
      overdueCount: json['OverdueCount'] as int? ?? json['overdueCount'] as int? ?? 0,
      inProgressCount: json['InProgressCount'] as int? ?? json['inProgressCount'] as int? ?? 0,
      doneCount: json['DoneCount'] as int? ?? json['doneCount'] as int? ?? 0,
      items: rawItems.map((e) => PrivateTaskItem.fromJson(e as Map<String, dynamic>)).toList(),
      members: rawMembers.map((e) => AssigneeOption.fromJson(e as Map<String, dynamic>)).toList(),
    );
  }
}

class PrivateTaskFormOptions {
  const PrivateTaskFormOptions({
    required this.members,
    required this.bonusOptions,
    required this.priorities,
  });

  final List<AssigneeOption> members;
  final List<double> bonusOptions;
  final List<String> priorities;

  factory PrivateTaskFormOptions.fromJson(Map<String, dynamic> json) {
    final rawMembers = (json['Members'] ?? json['members']) as List<dynamic>? ?? [];
    final rawBonus = (json['BonusOptions'] ?? json['bonusOptions']) as List<dynamic>? ?? [];
    final rawPriorities = (json['Priorities'] ?? json['priorities']) as List<dynamic>? ?? [];

    return PrivateTaskFormOptions(
      members: rawMembers.map((e) => AssigneeOption.fromJson(e as Map<String, dynamic>)).toList(),
      bonusOptions: rawBonus.map((e) => (e as num).toDouble()).toList(),
      priorities: rawPriorities.map((e) => e.toString()).toList(),
    );
  }
}
