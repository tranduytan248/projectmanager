import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_dropdown.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import 'task_detail_models.dart';
import 'task_detail_service.dart';

/// Nam trang thai (khop TaskStates ben backend, Models/Work/WorkTask.cs) — giu dung gia tri +
/// nhan tieng Viet, khong tu suy dien.
const Map<String, String> kTaskStateOptions = {
  'ChuaBatDau': 'Chưa bắt đầu',
  'DangLam': 'Đang làm',
  'TamDung': 'Tạm dừng',
  'HoanThanh': 'Hoàn thành',
  'Huy': 'Huỷ',
};

/// Bottom sheet "Cập nhật trạng thái" — mirror form "Báo cáo tiến độ" bên web (MyWork/Report):
/// chọn Trạng thái + nhập Tiến độ % + Ghi chú tuỳ chọn. Backend giữ nguyên luật chặn "chưa ghi
/// giờ thì không đổi được sang Đang làm/Hoàn thành" — lỗi trả về hiện thẳng cho người dùng, sheet
/// không tự đoán trước quy tắc để tránh lệch với luật thật ở server.
Future<TaskFullDetail?> showTaskStatusSheet(
  BuildContext context, {
  required TaskDetailService service,
  required int taskId,
  required TaskDetail initial,
}) {
  return showModalBottomSheet<TaskFullDetail>(
    context: context,
    isScrollControlled: true,
    isDismissible: false,
    enableDrag: false,
    elevation: 0,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
    ),
    builder: (context) =>
        _TaskStatusSheet(service: service, taskId: taskId, initial: initial),
  );
}

class _TaskStatusSheet extends StatefulWidget {
  const _TaskStatusSheet(
      {required this.service, required this.taskId, required this.initial});

  final TaskDetailService service;
  final int taskId;
  final TaskDetail initial;

  @override
  State<_TaskStatusSheet> createState() => _TaskStatusSheetState();
}

class _TaskStatusSheetState extends State<_TaskStatusSheet> {
  late String _state;
  late final _progressController =
      TextEditingController(text: '${widget.initial.progress}');
  final _noteController = TextEditingController();
  bool _submitting = false;
  String? _progressFieldError;
  TaskFullDetail? _result;

  @override
  void initState() {
    super.initState();
    _state = widget.initial.state;
  }

  @override
  void dispose() {
    _progressController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final progress = int.tryParse(_progressController.text.trim());
    if (progress == null || progress < 0 || progress > 100) {
      setState(() => _progressFieldError = 'Nhập tiến độ từ 0 đến 100.');
      return;
    }
    setState(() => _progressFieldError = null);

    setState(() => _submitting = true);
    final result = await widget.service.updateStatus(
      taskId: widget.taskId,
      state: _state,
      progress: progress,
      note: _noteController.text.trim().isEmpty
          ? null
          : _noteController.text.trim(),
    );
    if (!mounted) return;
    setState(() => _submitting = false);

    if (result.isSuccess) {
      setState(() => _result = result.data!);
      ToastService.show('Đã cập nhật trạng thái.', type: ToastType.success);
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
                    child: AppText('Cập nhật trạng thái',
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
              AppDropdown<String>(
                label: 'Trạng thái',
                value: _state,
                items: kTaskStateOptions,
                onChanged: (v) {
                  if (v != null) setState(() => _state = v);
                },
              ),
              const SizedBox(height: AppDimens.space12),
              AppTextField(
                label: 'Tiến độ (%)',
                controller: _progressController,
                keyboardType: TextInputType.number,
                inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                onChanged: (_) {
                  if (_progressFieldError != null) {
                    setState(() => _progressFieldError = null);
                  }
                },
              ),
              if (_progressFieldError != null)
                Padding(
                  padding: const EdgeInsets.only(top: AppDimens.space4),
                  child: AppText(_progressFieldError!,
                      variant: AppTextVariant.caption,
                      fontSize: 12,
                      color: AppColors.danger),
                ),
              const SizedBox(height: AppDimens.space12),
              AppTextField(
                label: 'Ghi chú',
                controller: _noteController,
                hint: 'Không bắt buộc — sẽ thêm vào Mô tả kèm ngày giờ',
                maxLines: 3,
                minLines: 2,
              ),
              const SizedBox(height: AppDimens.space16),
              AppButton(
                label: 'Cập nhật',
                fullWidth: true,
                isLoading: _submitting,
                onPressed: _submitting ? null : _submit,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
