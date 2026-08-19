import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../config/app_theme.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_card.dart';
import '../../core/widgets/app_detail_section.dart';
import '../../core/widgets/app_error_state.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import '../dashboard/dashboard_models.dart'
    show taskStateLabel, taskPriorityLabel;
import 'comment_html.dart';
import 'task_activity_log_screen.dart';
import 'task_assignee_sheet.dart';
import 'task_comments_screen.dart';
import 'task_detail_models.dart';
import 'task_detail_service.dart';
import 'task_detail_widgets.dart';
import 'task_status_sheet.dart';
import 'task_time_log_sheet.dart';
import 'task_todo_sheet.dart';

/// Man "Chi tiet cong viec" — CHI XEM (thong tin chung/tong gio da ghi/danh sach todolist), moi
/// thao tac tach thanh 3 hanh dong rieng de trang chinh nhe (giai quyet phan hieu ung "do" tung
/// bi phan anh khi mot trang dung ca 4 khoi thao tac inline): "Ghi gio" va "Cap nhat danh sach"
/// mo bottom sheet (task_time_log_sheet.dart/task_todo_sheet.dart), "Phan hoi" mo man rieng
/// (task_comments_screen.dart, co rich text/dinh kem/@nhac ten). Man chi doc mot lan qua
/// ChecklistApi/Detail — moi hanh dong tra ve dung SUMMARY moi nhat cua khoi lien quan, cap nhat
/// lai state o day khi dong sheet/man con.
class TaskDetailScreen extends StatefulWidget {
  const TaskDetailScreen({super.key, required this.taskId});

  final String taskId;

  @override
  State<TaskDetailScreen> createState() => _TaskDetailScreenState();
}

class _TaskDetailScreenState extends State<TaskDetailScreen> {
  final _service = TaskDetailService();
  late Future<TaskFullDetail> _future;

  @override
  void initState() {
    super.initState();
    _future = _service.fetch(widget.taskId);
  }

  void _reload() {
    setState(() => _future = _service.fetch(widget.taskId));
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: AppAppBar(
        title: 'Chi tiết công việc',
        actions: [
          AppIconButton(
            icon: PhosphorIconsRegular.clockCounterClockwise,
            tooltip: 'Lịch sử',
            onPressed: () => pushTaskActivityLogScreen(
              context,
              service: _service,
              taskId: int.parse(widget.taskId),
            ),
          ),
        ],
      ),
      body: SafeArea(
        child: FutureBuilder<TaskFullDetail>(
          future: _future,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: AppLoading());
            }

            if (snapshot.hasError) {
              return AppErrorState(
                  message: 'Không tải được thông tin công việc.',
                  onRetry: _reload);
            }

            return _TaskDetailBody(service: _service, initial: snapshot.data!);
          },
        ),
      ),
    );
  }
}

class _TaskDetailBody extends StatefulWidget {
  const _TaskDetailBody({required this.service, required this.initial});

  final TaskDetailService service;
  final TaskFullDetail initial;

  @override
  State<_TaskDetailBody> createState() => _TaskDetailBodyState();
}

class _TaskDetailBodyState extends State<_TaskDetailBody> {
  late TaskDetail _task;
  late TimeLogSummary _timeLog;
  late TodoSummary _todo;
  late CommentsSummary _comments;

  int get _taskId => _task.id;

  @override
  void initState() {
    super.initState();
    _task = widget.initial.task;
    _timeLog = widget.initial.timeLog;
    _todo = widget.initial.todo;
    _comments = widget.initial.comments;
  }

  Future<void> _openTimeLogSheet() async {
    final result = await showTaskTimeLogSheet(
      context,
      service: widget.service,
      taskId: _taskId,
      initial: _timeLog,
    );
    if (result != null) setState(() => _timeLog = result);
  }

  Future<void> _openTodoSheet() async {
    final result = await showTaskTodoSheet(
      context,
      service: widget.service,
      taskId: _taskId,
      initial: _todo,
    );
    if (result != null) setState(() => _todo = result);
  }

  Future<void> _openAssigneeSheet() async {
    final result = await showTaskAssigneeSheet(
      context,
      service: widget.service,
      taskId: _taskId,
      initial: _task,
    );
    if (result != null) {
      setState(() {
        _task = result.task;
        _timeLog = result.timeLog;
        _todo = result.todo;
        _comments = result.comments;
      });
    }
  }

  Future<void> _openStatusSheet() async {
    final result = await showTaskStatusSheet(
      context,
      service: widget.service,
      taskId: _taskId,
      initial: _task,
    );
    if (result != null) {
      setState(() {
        _task = result.task;
        _timeLog = result.timeLog;
        _todo = result.todo;
        _comments = result.comments;
      });
    }
  }

  Future<void> _openCommentsScreen() async {
    final result = await pushTaskCommentsScreen(
      context,
      service: widget.service,
      taskId: _taskId,
      initial: _comments,
    );
    if (result != null) setState(() => _comments = result);
  }

  @override
  Widget build(BuildContext context) {
    final visibleCommentCount =
        _comments.comments.where((c) => !c.isDeleted).length;

    return ListView(
      padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
      children: [
        _Header(task: _task),
        const SizedBox(height: AppDimens.space16),
        _StatsStrip(task: _task),
        const SizedBox(height: AppDimens.space16),
        AppDetailSection(title: 'Thông tin chung', child: _buildGeneralInfo()),
        const SizedBox(height: AppDimens.space16),
        AppDetailSection(title: 'Giờ công', child: _buildTimeLogView()),
        const SizedBox(height: AppDimens.space16),
        AppDetailSection(
          title: _todo.totalCount > 0
              ? 'Việc cần làm — ${_todo.doneCount}/${_todo.totalCount} xong'
              : 'Việc cần làm',
          child: _buildTodoView(),
        ),
        const SizedBox(height: AppDimens.space16),
        AppDetailSection(
          title: visibleCommentCount > 0
              ? 'Trao đổi — $visibleCommentCount nội dung'
              : 'Trao đổi',
          child: _buildCommentsView(),
        ),
      ],
    );
  }

  Widget _buildGeneralInfo() {
    final t = _task;
    final dueColor = t.isOverdue
        ? AppTheme.statusDanger
        : (t.isDueToday ? AppTheme.statusWarning : null);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Flexible(
              child: AppInfoChip(
                icon: taskStateIcon(t.state),
                label: taskStateLabel(t.state),
                color: taskStateColor(t.state),
              ),
            ),
            const SizedBox(width: AppDimens.space8),
            Flexible(
              child: AppInfoChip(
                icon: PhosphorIconsRegular.flag,
                label: 'Ưu tiên ${taskPriorityLabel(t.priority)}',
                color: taskPriorityColor(t.priority),
              ),
            ),
          ],
        ),
        const SizedBox(height: AppDimens.space16),
        TaskAssigneeRow(
          name: t.assigneeName?.isNotEmpty == true
              ? t.assigneeName!
              : '(chưa giao)',
          onEdit: t.canEditAll ? _openAssigneeSheet : null,
        ),
        TaskInfoIconRow(
            icon: PhosphorIconsRegular.buildings,
            label: 'Dự án',
            value: t.projectName?.isNotEmpty == true
                ? t.projectName!
                : 'Việc riêng'),
        TaskInfoIconRow(
            icon: PhosphorIconsRegular.briefcase,
            label: 'Loại công việc',
            value: _taskKindLabel(t.kind)),
        TaskProgressRow(percent: t.progress),
        TaskInfoIconRow(
          icon: PhosphorIconsRegular.calendarBlank,
          label: 'Ngày bắt đầu',
          value: t.startDate == null
              ? '—'
              : DateFormat('dd/MM/yyyy').format(t.startDate!),
        ),
        TaskInfoIconRow(
          icon: PhosphorIconsRegular.calendarCheck,
          label: 'Hạn hoàn thành',
          value: t.dueDate == null
              ? '—'
              : DateFormat('dd/MM/yyyy').format(t.dueDate!),
          iconColor: dueColor,
          valueColor: dueColor,
        ),
        if (t.completedAt != null)
          TaskInfoIconRow(
              icon: PhosphorIconsRegular.checkCircle,
              label: 'Ngày hoàn thành',
              value: DateFormat('dd/MM/yyyy').format(t.completedAt!),
              iconColor: AppTheme.statusSuccess,
              valueColor: AppTheme.statusSuccess),
        if (t.week > 0)
          TaskInfoIconRow(
              icon: PhosphorIconsRegular.calendarDots,
              label: 'Thuộc tuần',
              value: 'Tuần ${t.week}/${t.year}'),
        if (t.parentTitle?.isNotEmpty == true)
          TaskInfoIconRow(
              icon: PhosphorIconsRegular.folderOpen,
              label: 'Mục cha',
              value: t.parentTitle!),
        if (t.bonusPercent > 0)
          TaskInfoIconRow(
              icon: PhosphorIconsRegular.star,
              label: 'Điểm cộng KPI',
              value: '+${formatHours(t.bonusPercent, maxDecimals: 1)}%',
              iconColor: AppTheme.statusSuccess,
              valueColor: AppTheme.statusSuccess),
        if (t.hasAttachment)
          TaskInfoIconRow(
              icon: PhosphorIconsRegular.paperclip,
              label: 'File đính kèm',
              value: t.attachmentName?.isNotEmpty == true
                  ? t.attachmentName!
                  : '(không rõ tên)'),
        if (t.description.trim().isNotEmpty) ...[
          const SizedBox(height: AppDimens.space4),
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(AppDimens.space12),
            decoration: BoxDecoration(
              color: AppColors.primarySoft,
              borderRadius: BorderRadius.circular(AppDimens.radiusSm),
            ),
            child: AppText(t.description,
                variant: AppTextVariant.body, fontSize: 13, height: 1.5),
          ),
        ],
        if (t.canEdit) ...[
          const SizedBox(height: AppDimens.space12),
          AppButton(
              label: 'Cập nhật trạng thái',
              icon: PhosphorIconsRegular.arrowsClockwise,
              onPressed: _openStatusSheet),
        ],
      ],
    );
  }

  Widget _buildTimeLogView() {
    final tl = _timeLog;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Expanded(
              child: MiniStat(
                label: 'Đã ghi',
                value: '${formatHours(tl.taskTotal)} giờ',
                valueColor: tl.isOverCap ? AppTheme.statusDanger : null,
              ),
            ),
            Expanded(
              child: MiniStat(
                label: 'Trần công việc',
                value: tl.taskCap == null
                    ? '—'
                    : '${formatHours(tl.taskCap!)} giờ',
              ),
            ),
          ],
        ),
        const SizedBox(height: AppDimens.space8),
        Row(
          children: [
            Expanded(
              child: MiniStat(
                label: 'Còn lại',
                value: tl.taskRemaining == null
                    ? '—'
                    : '${formatHours(tl.taskRemaining!)} giờ',
              ),
            ),
            Expanded(
              child: MiniStat(
                label: 'Hôm nay đã ghi',
                value:
                    '${formatHours(tl.todayTotal)}/${formatHours(tl.maxPerDay)} giờ',
              ),
            ),
          ],
        ),
        if (tl.taskCap != null) ...[
          const SizedBox(height: AppDimens.space12),
          ClipRRect(
            borderRadius: BorderRadius.circular(AppDimens.radiusSm),
            child: LinearProgressIndicator(
              value: tl.usedPercent / 100,
              minHeight: 6,
              backgroundColor: AppTheme.brandBlue.withValues(alpha: 0.1),
              valueColor: AlwaysStoppedAnimation(tl.isOverCap
                  ? AppTheme.statusDanger
                  : AppTheme.brandBlueDark),
            ),
          ),
        ],
        const SizedBox(height: AppDimens.space12),
        if (tl.logs.isEmpty)
          const AppText('Chưa ghi giờ nào cho công việc này.',
              variant: AppTextVariant.caption, color: AppColors.textSecondary)
        else
          Column(children: [for (final log in tl.logs) TimeLogRow(log: log)]),
        const SizedBox(height: AppDimens.space12),
        if (tl.canLog)
          AppButton(
              label: 'Ghi giờ',
              icon: PhosphorIconsRegular.clockCountdown,
              onPressed: _openTimeLogSheet)
        else if (tl.blockedReason != null && tl.blockedReason!.isNotEmpty)
          AppText(tl.blockedReason!,
              variant: AppTextVariant.caption, color: AppColors.textSecondary),
      ],
    );
  }

  Widget _buildTodoView() {
    final todo = _todo;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (todo.items.isEmpty)
          const AppText('Chưa có việc con nào.',
              variant: AppTextVariant.caption, color: AppColors.textSecondary)
        else
          Column(children: [
            for (final item in todo.items) TodoReadOnlyRow(item: item)
          ]),
        if (todo.canManage) ...[
          const SizedBox(height: AppDimens.space12),
          AppButton(
              label: 'Cập nhật danh sách',
              icon: PhosphorIconsRegular.listChecks,
              onPressed: _openTodoSheet),
        ],
      ],
    );
  }

  Widget _buildCommentsView() {
    final visible = _comments.comments.where((c) => !c.isDeleted).toList();
    final last = visible.isEmpty ? null : visible.last;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (last == null)
          const AppText('Chưa có trao đổi nào.',
              variant: AppTextVariant.caption, color: AppColors.textSecondary)
        else
          Container(
            padding: const EdgeInsets.all(AppDimens.space8),
            decoration: BoxDecoration(
              color: AppColors.primarySoft,
              borderRadius: BorderRadius.circular(AppDimens.radiusSm),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: AppText(
                        last.authorName?.isNotEmpty == true
                            ? last.authorName!
                            : '—',
                        variant: AppTextVariant.caption,
                        fontSize: 12.5,
                        weight: FontWeight.w700,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    AppText(
                      last.createdAt == null
                          ? ''
                          : DateFormat('HH:mm dd/MM/yyyy')
                              .format(last.createdAt!),
                      variant: AppTextVariant.caption,
                      fontSize: 11,
                      color: AppColors.textSecondary,
                    ),
                  ],
                ),
                if ((last.content ?? '').trim().isNotEmpty) ...[
                  const SizedBox(height: AppDimens.space4),
                  renderCommentHtml(last.content!,
                      baseStyle: const TextStyle(fontSize: 13, height: 1.4)),
                ],
              ],
            ),
          ),
        const SizedBox(height: AppDimens.space12),
        AppButton(
            label: 'Phản hồi',
            icon: PhosphorIconsRegular.chatCircleText,
            onPressed: _openCommentsScreen),
      ],
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.task});

  final TaskDetail task;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        AppText(task.title,
            variant: AppTextVariant.title,
            fontSize: 19,
            weight: FontWeight.w800),
        if (task.code.isNotEmpty) ...[
          const SizedBox(height: 2),
          AppText(task.code,
              variant: AppTextVariant.caption,
              fontSize: 12.5,
              color: AppColors.textSecondary),
        ],
      ],
    );
  }
}

class _StatsStrip extends StatelessWidget {
  const _StatsStrip({required this.task});

  final TaskDetail task;

  @override
  Widget build(BuildContext context) {
    late final String dueLabel;
    late final Color dueColor;
    if (task.isOverdue) {
      dueLabel = 'Quá hạn';
      dueColor = AppTheme.statusDanger;
    } else if (task.isDueToday) {
      dueLabel = 'Hôm nay';
      dueColor = AppTheme.statusWarning;
    } else if (task.dueDate == null) {
      dueLabel = '—';
      dueColor = AppColors.textSecondary;
    } else {
      dueLabel = DateFormat('dd/MM').format(task.dueDate!);
      dueColor = AppTheme.brandBlue;
    }

    final showDaysLeft =
        task.dueDate != null && task.daysLeft != null && !_isClosed(task.state);

    return Row(
      children: [
        Expanded(
          child: _StatTile(
              value: '${task.progress}%',
              label: 'Tiến độ',
              color: AppTheme.brandBlue),
        ),
        const SizedBox(width: AppDimens.space8),
        Expanded(
            child: _StatTile(
                value: dueLabel, label: 'Hạn hoàn thành', color: dueColor)),
        if (showDaysLeft) ...[
          const SizedBox(width: AppDimens.space8),
          Expanded(
            child: _StatTile(
              value: '${task.daysLeft!.abs()}',
              label: task.daysLeft! >= 0 ? 'Ngày còn lại' : 'Ngày đã trễ',
              color: task.daysLeft! >= 0
                  ? AppTheme.brandBlue
                  : AppTheme.statusDanger,
            ),
          ),
        ],
      ],
    );
  }
}

class _StatTile extends StatelessWidget {
  const _StatTile(
      {required this.value, required this.label, required this.color});

  final String value;
  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 8),
      radius: 12,
      child: Column(
        children: [
          AppText(value,
              variant: AppTextVariant.body,
              fontSize: 16,
              weight: FontWeight.w800,
              color: color),
          const SizedBox(height: 2),
          AppText(label,
              align: TextAlign.center,
              maxLines: 1,
              variant: AppTextVariant.overline,
              fontSize: 10,
              color: AppColors.textSecondary,
              letterSpacing: 0),
        ],
      ),
    );
  }
}

bool _isClosed(String state) => state == 'HoanThanh' || state == 'Huy';

String _taskKindLabel(String kind) {
  switch (kind) {
    case 'Checklist':
      return 'Triển khai';
    case 'HoTro':
      return 'Hỗ trợ';
    case 'NgoaiDuAn':
      return 'Việc riêng';
    default:
      return kind;
  }
}

String formatHours(double value, {int maxDecimals = 2}) {
  var text = value.toStringAsFixed(maxDecimals);
  if (text.contains('.')) {
    text = text.replaceFirst(RegExp(r'0+$'), '');
    text = text.replaceFirst(RegExp(r'\.$'), '');
  }
  return text;
}
