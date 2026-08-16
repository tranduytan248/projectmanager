import '../dashboard/dashboard_models.dart' show parseAspNetDate;

/// Chi tiet mot dau viec — khop TaskDetailDto ben backend (Models/Api/ApiDtos.cs), ke thua het
/// truong cua TaskDto (xem TaskItem trong dashboard_models.dart) cong them cac truong rieng cua
/// man chi tiet day du (ProjectId/Week/Year/DaysLeft/IsOnTime/ParentTitle/BonusPercent/
/// HasAttachment/AttachmentName/CanEdit/CanEditAll).
class TaskDetail {
  const TaskDetail({
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
    required this.assigneeUserId,
    required this.progress,
    required this.kind,
    required this.parentId,
    required this.description,
    required this.startDate,
    required this.completedAt,
    required this.projectId,
    required this.week,
    required this.year,
    required this.daysLeft,
    required this.isOnTime,
    required this.parentTitle,
    required this.bonusPercent,
    required this.hasAttachment,
    required this.attachmentName,
    required this.canEdit,
    required this.canEditAll,
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
  final int assigneeUserId;
  final int progress;
  final String kind;
  final int parentId;
  final String description;
  final DateTime? startDate;
  final DateTime? completedAt;

  final int projectId;
  final int week;
  final int year;

  /// So ngay con lai toi han; am = da tre. Null khi khong co han.
  final int? daysLeft;
  final bool isOnTime;
  final String? parentTitle;
  final double bonusPercent;
  final bool hasAttachment;
  final String? attachmentName;
  final bool canEdit;
  final bool canEditAll;

  factory TaskDetail.fromJson(Map<String, dynamic> json) => TaskDetail(
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
        assigneeUserId: json['AssigneeUserId'] as int? ?? 0,
        progress: json['Progress'] as int? ?? 0,
        kind: json['Kind'] as String? ?? '',
        parentId: json['ParentId'] as int? ?? 0,
        description: json['Description'] as String? ?? '',
        startDate: parseAspNetDate(json['StartDate'] as String?),
        completedAt: parseAspNetDate(json['CompletedAt'] as String?),
        projectId: json['ProjectId'] as int? ?? 0,
        week: json['Week'] as int? ?? 0,
        year: json['Year'] as int? ?? 0,
        daysLeft: json['DaysLeft'] as int?,
        isOnTime: json['IsOnTime'] as bool? ?? false,
        parentTitle: json['ParentTitle'] as String?,
        bonusPercent: (json['BonusPercent'] as num?)?.toDouble() ?? 0,
        hasAttachment: json['HasAttachment'] as bool? ?? false,
        attachmentName: json['AttachmentName'] as String?,
        canEdit: json['CanEdit'] as bool? ?? false,
        canEditAll: json['CanEditAll'] as bool? ?? false,
      );
}

/// Mot luot ghi gio cong — khop TimeLogEntryDto. CanDelete da tinh san o server (dung cua chinh
/// minh VA viec chua dong), man hinh khong tu doan quyen.
class TimeLogEntry {
  const TimeLogEntry({
    required this.id,
    required this.workDate,
    required this.hours,
    required this.note,
    required this.userId,
    required this.userName,
    required this.canDelete,
  });

  final int id;
  final DateTime? workDate;
  final double hours;
  final String? note;
  final int userId;
  final String? userName;
  final bool canDelete;

  factory TimeLogEntry.fromJson(Map<String, dynamic> json) => TimeLogEntry(
        id: json['Id'] as int,
        workDate: parseAspNetDate(json['WorkDate'] as String?),
        hours: (json['Hours'] as num?)?.toDouble() ?? 0,
        note: json['Note'] as String?,
        userId: json['UserId'] as int? ?? 0,
        userName: json['UserName'] as String?,
        canDelete: json['CanDelete'] as bool? ?? false,
      );
}

/// Khoi "Gio cong" — khop TimeLogSummaryDto. TaskCap/TaskRemaining null nghia la viec chua co
/// du ngay bat dau/han nen chua tinh duoc tran (giong het cach _TimeLogs.cshtml ben web dang
/// hien dau gach ngang thay vi so).
class TimeLogSummary {
  const TimeLogSummary({
    required this.taskCap,
    required this.taskTotal,
    required this.taskRemaining,
    required this.todayTotal,
    required this.todayRemaining,
    required this.maxPerDay,
    required this.canLog,
    required this.blockedReason,
    required this.logs,
  });

  final double? taskCap;
  final double taskTotal;
  final double? taskRemaining;
  final double todayTotal;
  final double todayRemaining;
  final double maxPerDay;
  final bool canLog;
  final String? blockedReason;
  final List<TimeLogEntry> logs;

  /// Phan tram gio da dung so voi tran cua viec, de ve thanh tien trinh — cung cong thuc
  /// TaskTimeLogViewModel.UsedPercent ben web.
  int get usedPercent {
    if (taskCap == null || taskCap! <= 0) return 0;
    final percent = (taskTotal * 100 / taskCap!).round();
    return percent > 100 ? 100 : percent;
  }

  bool get isOverCap => taskCap != null && taskTotal > taskCap!;

  factory TimeLogSummary.fromJson(Map<String, dynamic> json) => TimeLogSummary(
        taskCap: (json['TaskCap'] as num?)?.toDouble(),
        taskTotal: (json['TaskTotal'] as num?)?.toDouble() ?? 0,
        taskRemaining: (json['TaskRemaining'] as num?)?.toDouble(),
        todayTotal: (json['TodayTotal'] as num?)?.toDouble() ?? 0,
        todayRemaining: (json['TodayRemaining'] as num?)?.toDouble() ?? 0,
        maxPerDay: (json['MaxPerDay'] as num?)?.toDouble() ?? 0,
        canLog: json['CanLog'] as bool? ?? false,
        blockedReason: json['BlockedReason'] as String?,
        logs: (json['Logs'] as List<dynamic>? ?? [])
            .map((e) => TimeLogEntry.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

/// Mot dong "Viec can lam" — khop TodoItemDto.
class TodoItem {
  const TodoItem({
    required this.id,
    required this.content,
    required this.isDone,
    required this.createdByName,
    required this.createdAt,
  });

  final int id;
  final String content;
  final bool isDone;
  final String? createdByName;
  final DateTime? createdAt;

  factory TodoItem.fromJson(Map<String, dynamic> json) => TodoItem(
        id: json['Id'] as int,
        content: json['Content'] as String? ?? '',
        isDone: json['IsDone'] as bool? ?? false,
        createdByName: json['CreatedByName'] as String?,
        createdAt: parseAspNetDate(json['CreatedAt'] as String?),
      );
}

/// Khoi "Viec can lam" — khop TodoSummaryDto. CanManage tinh san o server.
class TodoSummary {
  const TodoSummary({
    required this.canManage,
    required this.doneCount,
    required this.totalCount,
    required this.items,
  });

  final bool canManage;
  final int doneCount;
  final int totalCount;
  final List<TodoItem> items;

  factory TodoSummary.fromJson(Map<String, dynamic> json) => TodoSummary(
        canManage: json['CanManage'] as bool? ?? false,
        doneCount: json['DoneCount'] as int? ?? 0,
        totalCount: json['TotalCount'] as int? ?? 0,
        items: (json['Items'] as List<dynamic>? ?? [])
            .map((e) => TodoItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

/// Nguoi co the bi @nhac trong Trao doi — khop TaskMentionOptionDto, nguon goi y khi go @ trong
/// AppRichEditor (xem mention_picker.dart).
class TaskMentionOption {
  const TaskMentionOption({
    required this.userId,
    required this.fullName,
    required this.userName,
  });

  final int userId;
  final String fullName;
  final String? userName;

  factory TaskMentionOption.fromJson(Map<String, dynamic> json) =>
      TaskMentionOption(
        userId: json['UserId'] as int? ?? 0,
        fullName: json['FullName'] as String? ?? '',
        userName: json['UserName'] as String?,
      );
}

/// Mot luot "Trao doi" — khop TaskCommentDto. Content la HTML da qua HtmlSanitizer.Clean ben
/// server (dam/nghieng/gach chan/danh sach/link) — hien qua bo doc rieng (comment_html.dart),
/// KHONG phai chu thuong nua. Noi dung thu hoi thi Content la null — man hinh tu hien "Noi dung
/// da thu hoi." giong _Comments.cshtml ben web.
class TaskComment {
  const TaskComment({
    required this.id,
    required this.authorName,
    required this.content,
    required this.createdAt,
    required this.isDeleted,
    required this.hasAttachment,
    required this.attachmentName,
    required this.canRecall,
  });

  final int id;
  final String? authorName;
  final String? content;
  final DateTime? createdAt;
  final bool isDeleted;
  final bool hasAttachment;
  final String? attachmentName;
  final bool canRecall;

  factory TaskComment.fromJson(Map<String, dynamic> json) => TaskComment(
        id: json['Id'] as int,
        authorName: json['AuthorName'] as String?,
        content: json['Content'] as String?,
        createdAt: parseAspNetDate(json['CreatedAt'] as String?),
        isDeleted: json['IsDeleted'] as bool? ?? false,
        hasAttachment: json['HasAttachment'] as bool? ?? false,
        attachmentName: json['AttachmentName'] as String?,
        canRecall: json['CanRecall'] as bool? ?? false,
      );
}

/// Khoi "Trao doi" — khop CommentsSummaryDto.
class CommentsSummary {
  const CommentsSummary({required this.comments, required this.participants});

  final List<TaskComment> comments;
  final List<TaskMentionOption> participants;

  factory CommentsSummary.fromJson(Map<String, dynamic> json) =>
      CommentsSummary(
        comments: (json['Comments'] as List<dynamic>? ?? [])
            .map((e) => TaskComment.fromJson(e as Map<String, dynamic>))
            .toList(),
        participants: (json['Participants'] as List<dynamic>? ?? [])
            .map((e) => TaskMentionOption.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

/// Mot dong trong "Lich su thao tac" cua mot dau viec — khop TaskActivityLogDto. Description da
/// dung san cau tieng Viet o server, man hinh chi viec hien thang.
class TaskActivityLogEntry {
  const TaskActivityLogEntry({
    required this.id,
    required this.actorName,
    required this.action,
    required this.description,
    required this.createdAt,
  });

  final int id;
  final String actorName;
  final String action;
  final String description;
  final DateTime? createdAt;

  factory TaskActivityLogEntry.fromJson(Map<String, dynamic> json) =>
      TaskActivityLogEntry(
        id: json['Id'] as int,
        actorName: json['ActorName'] as String? ?? '(không rõ)',
        action: json['Action'] as String? ?? '',
        description: json['Description'] as String? ?? '',
        createdAt: parseAspNetDate(json['CreatedAt'] as String?),
      );
}

/// Boc ca 4 khoi cua man "Chi tiet cong viec" day du — khop TaskFullDetailDto, tra ve trong MOT
/// lan goi ChecklistApi/Detail duy nhat.
class TaskFullDetail {
  const TaskFullDetail({
    required this.task,
    required this.timeLog,
    required this.todo,
    required this.comments,
  });

  final TaskDetail task;
  final TimeLogSummary timeLog;
  final TodoSummary todo;
  final CommentsSummary comments;

  factory TaskFullDetail.fromJson(Map<String, dynamic> json) => TaskFullDetail(
        task: TaskDetail.fromJson(json['Task'] as Map<String, dynamic>),
        timeLog:
            TimeLogSummary.fromJson(json['TimeLog'] as Map<String, dynamic>),
        todo: TodoSummary.fromJson(json['Todo'] as Map<String, dynamic>),
        comments:
            CommentsSummary.fromJson(json['Comments'] as Map<String, dynamic>),
      );
}
