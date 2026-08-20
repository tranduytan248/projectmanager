import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../config/app_theme.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/dialog_service.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_date_field.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import 'task_detail_models.dart';
import 'task_detail_service.dart';
import 'task_detail_widgets.dart';

/// Bottom sheet "Ghi giờ" — tach ra khoi man Chi tiet cong viec (truoc day gom thang trong trang
/// chinh) de trang chinh chi con VIEW-ONLY. Mo qua showModalBottomSheet<TimeLogSummary>, tra ve
/// TimeLogSummary moi nhat khi dong (null neu khong doi gi — man chinh giu nguyen du lieu cu).
Future<TimeLogSummary?> showTaskTimeLogSheet(
  BuildContext context, {
  required TaskDetailService service,
  required int taskId,
  required TimeLogSummary initial,
}) {
  return showModalBottomSheet<TimeLogSummary>(
    context: context,
    isScrollControlled: true,
    // Tat vuot/bam-ngoai-de-dong: nguoi dung phai bam nut Dong tuong minh, dam bao sheet LUON
    // tra ve _summary moi nhat (co the da doi du con _changed=false) cho trang chinh cap nhat —
    // mac dinh cua showModalBottomSheet tra null khi vuot dong, lam trang chinh giu du lieu cu.
    isDismissible: false,
    enableDrag: false,
    elevation: 0,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
    ),
    builder: (context) =>
        _TaskTimeLogSheet(service: service, taskId: taskId, initial: initial),
  );
}

class _TaskTimeLogSheet extends StatefulWidget {
  const _TaskTimeLogSheet(
      {required this.service, required this.taskId, required this.initial});

  final TaskDetailService service;
  final int taskId;
  final TimeLogSummary initial;

  @override
  State<_TaskTimeLogSheet> createState() => _TaskTimeLogSheetState();
}

class _TaskTimeLogSheetState extends State<_TaskTimeLogSheet> {
  late TimeLogSummary _summary;
  bool _changed = false;

  DateTime _workDate = DateTime.now();
  final _hoursController = TextEditingController();
  final _noteController = TextEditingController();
  bool _loggingTime = false;
  int? _deletingLogId;
  String? _hoursFieldError;

  @override
  void initState() {
    super.initState();
    _summary = widget.initial;
  }

  @override
  void dispose() {
    _hoursController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  Future<void> _submitLogTime() async {
    final hours =
        double.tryParse(_hoursController.text.trim().replaceAll(',', '.'));
    if (hours == null || hours <= 0) {
      setState(
          () => _hoursFieldError = 'Nhập số giờ hợp lệ, ví dụ 2 hoặc 2,5.');
      return;
    }
    setState(() => _hoursFieldError = null);

    setState(() => _loggingTime = true);
    final result = await widget.service.logTime(
      taskId: widget.taskId,
      workDate: _workDate,
      hours: hours,
      note: _noteController.text.trim().isEmpty
          ? null
          : _noteController.text.trim(),
    );
    if (!mounted) return;
    setState(() => _loggingTime = false);

    if (result.isSuccess) {
      setState(() {
        _summary = result.data!;
        _changed = true;
        _hoursController.clear();
        _noteController.clear();
      });
      ToastService.show('Đã ghi giờ công.', type: ToastType.success);
    } else {
      ToastService.show(result.error!, type: ToastType.error);
    }
  }

  Future<void> _confirmDeleteTimeLog(TimeLogEntry log) async {
    final dateLabel = log.workDate == null
        ? ''
        : ' ngày ${DateFormat('dd/MM/yyyy').format(log.workDate!)}';
    final ok = await DialogService.showConfirm(
      'Xoá lượt ghi ${formatHours(log.hours)} giờ$dateLabel? Hành động này không thể hoàn tác.',
      title: 'Xoá lượt ghi giờ',
    );
    if (!ok) return;

    setState(() => _deletingLogId = log.id);
    final result = await widget.service
        .deleteTimeLog(taskId: widget.taskId, logId: log.id);
    if (!mounted) return;
    setState(() => _deletingLogId = null);

    if (result.isSuccess) {
      setState(() {
        _summary = result.data!;
        _changed = true;
      });
      ToastService.show('Đã xoá lượt ghi giờ.', type: ToastType.success);
    } else {
      ToastService.show(result.error!, type: ToastType.error);
    }
  }

  @override
  Widget build(BuildContext context) {
    final tl = _summary;

    return SafeArea(
      child: Padding(
        padding: EdgeInsets.fromLTRB(
            20, 20, 20, MediaQuery.of(context).viewInsets.bottom + 20),
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Expanded(
                    child: AppText('Ghi giờ',
                        variant: AppTextVariant.body,
                        fontSize: 16,
                        weight: FontWeight.w700),
                  ),
                  AppIconButton(
                    icon: PhosphorIconsRegular.x,
                    tooltip: 'Đóng',
                    onPressed: () =>
                        Navigator.of(context).pop(_changed ? _summary : null),
                  ),
                ],
              ),
              const SizedBox(height: AppDimens.space12),
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
              if (tl.isOverCap) ...[
                const SizedBox(height: AppDimens.space8),
                Container(
                  padding: const EdgeInsets.all(AppDimens.space8),
                  decoration: BoxDecoration(
                    color: AppColors.dangerSoft,
                    borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                  ),
                  child: const AppText(
                    'Tổng giờ đã ghi vượt trần của công việc. Hãy xoá bớt một lượt hoặc nhờ người '
                    'giao việc nới hạn hoàn thành.',
                    variant: AppTextVariant.caption,
                    fontSize: 12.5,
                    color: AppColors.danger,
                  ),
                ),
              ],
              const SizedBox(height: AppDimens.space12),
              if (tl.logs.isEmpty)
                const AppText('Chưa ghi giờ nào cho công việc này.',
                    variant: AppTextVariant.caption,
                    color: AppColors.textSecondary)
              else
                Column(
                  children: [
                    for (final log in tl.logs)
                      TimeLogRow(
                        log: log,
                        busy: _deletingLogId == log.id,
                        onDelete: log.canDelete
                            ? () => _confirmDeleteTimeLog(log)
                            : null,
                      ),
                  ],
                ),
              const SizedBox(height: AppDimens.space12),
              if (tl.canLog)
                _buildForm(tl)
              else
                AppText(
                  tl.blockedReason ??
                      'Bạn không ghi được giờ cho công việc này.',
                  variant: AppTextVariant.caption,
                  color: AppColors.textSecondary,
                ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildForm(TimeLogSummary tl) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        AppDateField(
          label: 'Ngày làm',
          value: _workDate,
          lastDate: DateTime.now(),
          onChanged: (d) => setState(() => _workDate = d),
        ),
        const SizedBox(height: AppDimens.space12),
        AppTextField(
          label: 'Số giờ',
          controller: _hoursController,
          hint: '2,5',
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          inputFormatters: [
            FilteringTextInputFormatter.allow(RegExp(r'[0-9.,]'))
          ],
          onChanged: (_) {
            if (_hoursFieldError != null) {
              setState(() => _hoursFieldError = null);
            }
          },
        ),
        if (_hoursFieldError != null)
          Padding(
            padding: const EdgeInsets.only(top: AppDimens.space4),
            child: AppText(_hoursFieldError!,
                variant: AppTextVariant.caption,
                fontSize: 12,
                color: AppColors.danger),
          ),
        const SizedBox(height: AppDimens.space12),
        AppTextField(
            label: 'Nội dung đã làm',
            controller: _noteController,
            hint: 'Không bắt buộc'),
        const SizedBox(height: AppDimens.space12),
        AppButton(
          label: 'Ghi giờ',
          fullWidth: true,
          isLoading: _loggingTime,
          onPressed: _loggingTime ? null : _submitLogTime,
        ),
        const SizedBox(height: AppDimens.space8),
        AppText(
          'Tối đa ${formatHours(tl.maxPerDay)} giờ mỗi ngày tính trên tất cả công việc — hôm '
          'nay còn ${formatHours(tl.todayRemaining)} giờ.',
          variant: AppTextVariant.caption,
          fontSize: 12,
          color: AppColors.textSecondary,
        ),
      ],
    );
  }
}
