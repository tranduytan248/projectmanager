import 'package:flutter/material.dart';
import 'package:flutter_slidable/flutter_slidable.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../config/app_theme.dart';
import '../../core/classes/route_manager.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_error_state.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_text.dart';
import '../../shared/widgets/app_bottom_nav.dart';
import '../app_routes.dart';
import '../dashboard/dashboard_models.dart' show TaskItem, taskStateLabel;
import 'my_work_service.dart';

/// Man "Cong viec": mac dinh la viec cua rieng minh (tab duoi), nhung cung dung lai duoc lam
/// dich den cho cac the "Viec chua xong"/"Qua han" toan To tren Dashboard qua tham so scope/
/// filter — xem MyWorkController.
class MyWorkScreen extends StatefulWidget {
  const MyWorkScreen({super.key, this.scope = 'mine', this.filter});

  final String scope;
  final String? filter;

  @override
  State<MyWorkScreen> createState() => _MyWorkScreenState();
}

class _MyWorkScreenState extends State<MyWorkScreen> {
  final _service = MyWorkService();
  late Future<List<TaskItem>> _future;

  @override
  void initState() {
    super.initState();
    _future = _service.fetch(scope: widget.scope, filter: widget.filter);
  }

  void _reload() {
    setState(() =>
        _future = _service.fetch(scope: widget.scope, filter: widget.filter));
  }

  String get _title {
    if (widget.scope != 'team') return 'Việc của tôi';
    if (widget.filter == 'overdue') return 'Quá hạn — Toàn Tổ';
    if (widget.filter == 'open') return 'Việc chưa xong — Toàn Tổ';
    return 'Công việc — Toàn Tổ';
  }

  @override
  Widget build(BuildContext context) {
    return AppBottomNav(
      currentIndex: 2,
      appBar: AppAppBar(title: _title),
      body: FutureBuilder<List<TaskItem>>(
        future: _future,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const Center(child: AppLoading());
          }

          if (snapshot.hasError) {
            return AppErrorState(
                message: 'Không tải được danh sách công việc.',
                onRetry: _reload);
          }

          final tasks = snapshot.data!;
          if (tasks.isEmpty) {
            return Center(
              child: AppText(
                widget.scope == 'team'
                    ? 'Không có việc nào khớp.'
                    : 'Bạn chưa có việc nào.',
                variant: AppTextVariant.body,
                color: AppColors.textSecondary,
              ),
            );
          }

          return ListView.separated(
            itemCount: tasks.length,
            separatorBuilder: (context, index) =>
                const Divider(height: 1, indent: 68),
            itemBuilder: (context, index) => _TaskRow(task: tasks[index]),
          );
        },
      ),
    );
  }
}

/// Mot dong cong viec: vuot sang trai lo hai thao tac nhanh (Giao viec / Xu ly), giong danh
/// sach issue cua Jira mobile — nhung chi dung xanh thuong hieu + trang, khong dung mau khac.
class _TaskRow extends StatelessWidget {
  const _TaskRow({required this.task});

  final TaskItem task;

  @override
  Widget build(BuildContext context) {
    return Slidable(
      key: ValueKey(task.id),
      endActionPane: ActionPane(
        motion: const DrawerMotion(),
        extentRatio: 0.5,
        children: [
          SlidableAction(
            onPressed: (_) => ToastService.show(
                'Giao việc "${task.code}" — đang phát triển.',
                type: ToastType.warning),
            backgroundColor: AppColors.primaryDark,
            foregroundColor: AppColors.textOnPrimary,
            icon: PhosphorIconsRegular.userPlus,
            label: 'Giao việc',
          ),
          SlidableAction(
            onPressed: (_) => ToastService.show(
                'Chuyển trạng thái "${task.code}" — đang phát triển.',
                type: ToastType.warning),
            backgroundColor: AppColors.primary,
            foregroundColor: AppColors.textOnPrimary,
            icon: PhosphorIconsRegular.arrowsClockwise,
            label: 'Xử lý',
          ),
        ],
      ),
      child: InkWell(
        onTap: () => Nav.toNamed(context, AppRoutes.taskDetail,
            arguments: {'taskId': task.id.toString()}),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 36,
                height: 36,
                decoration: BoxDecoration(
                  color: AppColors.primaryDark,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: PhosphorIcon(
                  task.isOverdue
                      ? PhosphorIconsRegular.warningCircle
                      : PhosphorIconsRegular.plusCircle,
                  color: AppColors.textOnPrimary,
                  size: 20,
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    AppText(
                      task.title,
                      variant: AppTextVariant.body,
                      fontSize: 15,
                      weight: FontWeight.w500,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 6),
                    Wrap(
                      spacing: 8,
                      runSpacing: 4,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      children: [
                        if (task.code.isNotEmpty)
                          AppText(
                            task.code,
                            variant: AppTextVariant.caption,
                            fontSize: 12.5,
                            color: AppColors.textSecondary,
                          ),
                        if (task.projectName?.isNotEmpty == true)
                          AppText(
                            task.projectName!,
                            variant: AppTextVariant.caption,
                            fontSize: 12.5,
                            color: AppColors.textSecondary,
                          ),
                        _StateBadge(state: task.state),
                        AppText(
                          _dueLabel(task),
                          variant: AppTextVariant.caption,
                          fontSize: 12.5,
                          color: _dueColor(task),
                          weight: FontWeight.w600,
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  String _dueLabel(TaskItem task) {
    if (task.isOverdue) return 'Quá hạn';
    if (task.isDueToday) return 'Hôm nay';
    if (task.dueDate == null) return '—';
    return DateFormat('dd/MM').format(task.dueDate!);
  }

  // Cung 3 muc mau voi _DueInfo cua dashboard_screen.dart va _StatsStrip cua task_detail_screen.dart
  // (qua han = do, hom nay = vang canh bao, con lai = xanh thuong hieu) — truoc day thieu muc
  // "Hom nay" nen hien nham mau xanh giong ngay thuong.
  Color _dueColor(TaskItem task) {
    if (task.isOverdue) return AppTheme.statusDanger;
    if (task.isDueToday) return AppTheme.statusWarning;
    return AppTheme.brandBlueDark;
  }
}

/// Nhan trang thai kieu vien mo — mau theo dung ngu nghia (xong = xanh la, cho duyet = vang
/// canh bao), thay vi to mot mau brand cho moi trang thai, theo quy uoc site.css (mau thuong
/// hieu CHI danh cho thao tac, khong dung cho du lieu).
class _StateBadge extends StatelessWidget {
  const _StateBadge({required this.state});

  final String state;

  Color get _color {
    switch (state) {
      case 'HoanThanh':
        return AppTheme.statusSuccess;
      case 'TamDung':
        return AppTheme.statusWarning;
      default:
        return AppColors.textSecondary;
    }
  }

  @override
  Widget build(BuildContext context) {
    final color = _color;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(
        border: Border.all(color: color.withValues(alpha: 0.5)),
        borderRadius: BorderRadius.circular(4),
      ),
      child: AppText(
        taskStateLabel(state).toUpperCase(),
        variant: AppTextVariant.overline,
        fontSize: 10.5,
        weight: FontWeight.w700,
        color: color,
        letterSpacing: 0.3,
      ),
    );
  }
}
