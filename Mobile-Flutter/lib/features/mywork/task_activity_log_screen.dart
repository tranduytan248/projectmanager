import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_error_state.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import 'task_detail_models.dart';
import 'task_detail_service.dart';

/// Man "Lịch sử" — nhật ký thao tác của một đầu việc (ai làm gì, lúc nào), mới nhất trên đầu.
/// Push full-màn hình từ icon trên AppBar của Chi tiết công việc (không phải bottom sheet — danh
/// sách có thể dài, cần toàn bộ chiều cao màn hình). Chỉ ĐỌC, không có hành động nào ở đây.
Future<void> pushTaskActivityLogScreen(
  BuildContext context, {
  required TaskDetailService service,
  required int taskId,
}) {
  return Navigator.of(context).push(
    MaterialPageRoute(
      builder: (context) =>
          TaskActivityLogScreen(service: service, taskId: taskId),
    ),
  );
}

class TaskActivityLogScreen extends StatefulWidget {
  const TaskActivityLogScreen(
      {super.key, required this.service, required this.taskId});

  final TaskDetailService service;
  final int taskId;

  @override
  State<TaskActivityLogScreen> createState() => _TaskActivityLogScreenState();
}

class _TaskActivityLogScreenState extends State<TaskActivityLogScreen> {
  late Future<List<TaskActivityLogEntry>> _future;

  @override
  void initState() {
    super.initState();
    _future = widget.service.fetchActivityLog(widget.taskId);
  }

  void _reload() {
    setState(() => _future = widget.service.fetchActivityLog(widget.taskId));
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: AppAppBar(
        title: 'Lịch sử',
        leading: AppIconButton(
          icon: PhosphorIconsRegular.arrowLeft,
          tooltip: 'Quay lại',
          onPressed: () => Navigator.of(context).pop(),
        ),
      ),
      body: SafeArea(
        child: FutureBuilder<List<TaskActivityLogEntry>>(
          future: _future,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: AppLoading());
            }

            if (snapshot.hasError) {
              return AppErrorState(
                  message: 'Không tải được lịch sử thao tác.',
                  onRetry: _reload);
            }

            final items = snapshot.data!;
            if (items.isEmpty) {
              return const Center(
                child: Padding(
                  padding: EdgeInsets.all(AppDimens.space24),
                  child: AppText(
                    'Chưa có thao tác nào được ghi nhận trên công việc này.',
                    variant: AppTextVariant.caption,
                    color: AppColors.textSecondary,
                    align: TextAlign.center,
                  ),
                ),
              );
            }

            return ListView.separated(
              padding: const EdgeInsets.all(AppDimens.space16),
              itemCount: items.length,
              separatorBuilder: (_, __) =>
                  const SizedBox(height: AppDimens.space8),
              itemBuilder: (context, index) => _ActivityRow(item: items[index]),
            );
          },
        ),
      ),
    );
  }
}

class _ActivityRow extends StatelessWidget {
  const _ActivityRow({required this.item});

  final TaskActivityLogEntry item;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(AppDimens.space12),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(AppDimens.radiusMd),
        border: Border.all(color: AppColors.border),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Padding(
            padding: EdgeInsets.only(top: 2),
            child: PhosphorIcon(PhosphorIconsRegular.clockCounterClockwise,
                size: 16, color: AppColors.primary),
          ),
          const SizedBox(width: AppDimens.space8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: AppText(
                        item.actorName,
                        variant: AppTextVariant.caption,
                        fontSize: 12.5,
                        weight: FontWeight.w700,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    AppText(
                      item.createdAt == null
                          ? ''
                          : DateFormat('HH:mm dd/MM/yyyy')
                              .format(item.createdAt!),
                      variant: AppTextVariant.caption,
                      fontSize: 11,
                      color: AppColors.textSecondary,
                    ),
                  ],
                ),
                const SizedBox(height: AppDimens.space4),
                AppText(
                  item.description,
                  variant: AppTextVariant.body,
                  fontSize: 13,
                  height: 1.4,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
