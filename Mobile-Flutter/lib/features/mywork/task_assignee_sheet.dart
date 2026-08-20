import 'package:flutter/material.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_dropdown.dart';
import '../../core/widgets/app_error_state.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_text.dart';
import '../checklist/checklist_models.dart' show AssigneeOption;
import 'task_detail_models.dart';
import 'task_detail_service.dart';

/// Bottom sheet "Đổi người thực hiện" — chỉ mở được khi TaskDetail.canEditAll = true (PM dự án
/// hoặc Quản lý Tổ, server tự kiểm lại). Tải danh sách thành viên đang hoạt động của dự án qua
/// AssigneeOptions rồi mới cho chọn, khác với các sheet còn lại vì cần thêm một lượt gọi API nữa
/// trước khi hiện được form.
Future<TaskFullDetail?> showTaskAssigneeSheet(
  BuildContext context, {
  required TaskDetailService service,
  required int taskId,
  required TaskDetail initial,
}) {
  return showModalBottomSheet<TaskFullDetail>(
    context: context,
    isScrollControlled: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
    ),
    builder: (context) =>
        _TaskAssigneeSheet(service: service, taskId: taskId, initial: initial),
  );
}

class _TaskAssigneeSheet extends StatefulWidget {
  const _TaskAssigneeSheet(
      {required this.service, required this.taskId, required this.initial});

  final TaskDetailService service;
  final int taskId;
  final TaskDetail initial;

  @override
  State<_TaskAssigneeSheet> createState() => _TaskAssigneeSheetState();
}

class _TaskAssigneeSheetState extends State<_TaskAssigneeSheet> {
  late Future<List<AssigneeOption>> _optionsFuture;
  late int _assigneeUserId;
  bool _submitting = false;
  TaskFullDetail? _result;

  @override
  void initState() {
    super.initState();
    _assigneeUserId = widget.initial.assigneeUserId;
    _optionsFuture = widget.service.fetchAssigneeOptions(widget.taskId);
  }

  Future<void> _submit() async {
    setState(() => _submitting = true);
    final result = await widget.service.updateAssignee(
      taskId: widget.taskId,
      assigneeUserId: _assigneeUserId,
    );
    if (!mounted) return;
    setState(() => _submitting = false);

    if (result.isSuccess) {
      setState(() => _result = result.data!);
      ToastService.show('Đã đổi người thực hiện.', type: ToastType.success);
      Navigator.of(context).pop(_result);
    } else {
      ToastService.show(result.error!, type: ToastType.error);
    }
  }

  @override
  Widget build(BuildContext context) {
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
                    child: AppText('Đổi người thực hiện',
                        variant: AppTextVariant.body,
                        fontSize: 16,
                        weight: FontWeight.w700),
                  ),
                  AppIconButton(
                    icon: PhosphorIconsRegular.x,
                    tooltip: 'Đóng',
                    onPressed: () => Navigator.of(context).pop(_result),
                  ),
                ],
              ),
              const SizedBox(height: AppDimens.space16),
              FutureBuilder<List<AssigneeOption>>(
                future: _optionsFuture,
                builder: (context, snapshot) {
                  if (snapshot.connectionState != ConnectionState.done) {
                    return const Padding(
                      padding: EdgeInsets.symmetric(vertical: 24),
                      child: Center(child: AppLoading()),
                    );
                  }
                  if (snapshot.hasError) {
                    return AppErrorState(
                      message: 'Không tải được danh sách thành viên dự án.',
                      onRetry: () => setState(() => _optionsFuture =
                          widget.service.fetchAssigneeOptions(widget.taskId)),
                    );
                  }

                  final options = snapshot.data!;
                  final validIds = {0, for (final o in options) o.userId};
                  if (!validIds.contains(_assigneeUserId)) {
                    _assigneeUserId = 0;
                  }

                  return Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      AppDropdown<int>(
                        label: 'Người thực hiện',
                        value: _assigneeUserId,
                        items: {
                          0: '— Chưa giao —',
                          for (final o in options) o.userId: o.fullName,
                        },
                        onChanged: (v) =>
                            setState(() => _assigneeUserId = v ?? 0),
                      ),
                      const SizedBox(height: AppDimens.space16),
                      AppButton(
                        label: 'Cập nhật',
                        fullWidth: true,
                        isLoading: _submitting,
                        onPressed: _submitting ? null : _submit,
                      ),
                    ],
                  );
                },
              ),
            ],
          ),
        ),
      ),
    );
  }
}
