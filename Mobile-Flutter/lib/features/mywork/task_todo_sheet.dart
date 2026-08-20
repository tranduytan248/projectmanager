import 'package:flutter/material.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../config/app_theme.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/dialog_service.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_checkbox.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import 'task_detail_models.dart';
import 'task_detail_service.dart';

/// Bottom sheet "Cập nhật danh sách" (việc cần làm) — tach ra khoi man Chi tiet cong viec de
/// trang chinh chi con VIEW-ONLY, cung mo hinh voi task_time_log_sheet.dart.
Future<TodoSummary?> showTaskTodoSheet(
  BuildContext context, {
  required TaskDetailService service,
  required int taskId,
  required TodoSummary initial,
}) {
  return showModalBottomSheet<TodoSummary>(
    context: context,
    isScrollControlled: true,
    // Tat vuot/bam-ngoai-de-dong — ly do xem task_time_log_sheet.dart.
    isDismissible: false,
    enableDrag: false,
    elevation: 0,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
    ),
    builder: (context) =>
        _TaskTodoSheet(service: service, taskId: taskId, initial: initial),
  );
}

class _TaskTodoSheet extends StatefulWidget {
  const _TaskTodoSheet(
      {required this.service, required this.taskId, required this.initial});

  final TaskDetailService service;
  final int taskId;
  final TodoSummary initial;

  @override
  State<_TaskTodoSheet> createState() => _TaskTodoSheetState();
}

class _TaskTodoSheetState extends State<_TaskTodoSheet> {
  late TodoSummary _summary;
  bool _changed = false;

  final _addController = TextEditingController();
  bool _adding = false;
  final Set<int> _busyIds = {};
  int? _editingId;
  final _editController = TextEditingController();
  String? _addFieldError;
  String? _editFieldError;

  @override
  void initState() {
    super.initState();
    _summary = widget.initial;
  }

  @override
  void dispose() {
    _addController.dispose();
    _editController.dispose();
    super.dispose();
  }

  Future<void> _submitAdd() async {
    final content = _addController.text.trim();
    if (content.isEmpty) {
      setState(() => _addFieldError = 'Vui lòng nhập nội dung việc cần làm.');
      return;
    }
    setState(() => _addFieldError = null);

    setState(() => _adding = true);
    final result =
        await widget.service.addTodo(taskId: widget.taskId, content: content);
    if (!mounted) return;
    setState(() => _adding = false);

    if (result.isSuccess) {
      setState(() {
        _summary = result.data!;
        _changed = true;
        _addController.clear();
      });
    } else {
      ToastService.show(result.error!, type: ToastType.error);
    }
  }

  Future<void> _toggle(TodoItem item) async {
    setState(() => _busyIds.add(item.id));
    final result =
        await widget.service.toggleTodo(taskId: widget.taskId, todoId: item.id);
    if (!mounted) return;
    setState(() => _busyIds.remove(item.id));

    if (result.isSuccess) {
      setState(() {
        _summary = result.data!;
        _changed = true;
      });
    } else {
      ToastService.show(result.error!, type: ToastType.error);
    }
  }

  void _startEdit(TodoItem item) {
    _editController.text = item.content;
    setState(() {
      _editingId = item.id;
      _editFieldError = null;
    });
  }

  Future<void> _submitEdit(TodoItem item) async {
    final content = _editController.text.trim();
    if (content.isEmpty) {
      setState(() => _editFieldError = 'Vui lòng nhập nội dung việc cần làm.');
      return;
    }
    setState(() => _editFieldError = null);

    setState(() => _busyIds.add(item.id));
    final result = await widget.service
        .editTodo(taskId: widget.taskId, todoId: item.id, content: content);
    if (!mounted) return;
    setState(() => _busyIds.remove(item.id));

    if (result.isSuccess) {
      setState(() {
        _summary = result.data!;
        _changed = true;
        _editingId = null;
      });
    } else {
      ToastService.show(result.error!, type: ToastType.error);
    }
  }

  Future<void> _confirmDelete(TodoItem item) async {
    final ok = await DialogService.showConfirm(
      'Xoá việc "${item.content}"? Hành động này không thể hoàn tác.',
      title: 'Xoá việc cần làm',
    );
    if (!ok) return;

    setState(() => _busyIds.add(item.id));
    final result =
        await widget.service.deleteTodo(taskId: widget.taskId, todoId: item.id);
    if (!mounted) return;
    setState(() => _busyIds.remove(item.id));

    if (result.isSuccess) {
      setState(() {
        _summary = result.data!;
        _changed = true;
      });
      ToastService.show('Đã xoá việc cần làm.', type: ToastType.success);
    } else {
      ToastService.show(result.error!, type: ToastType.error);
    }
  }

  @override
  Widget build(BuildContext context) {
    final todo = _summary;

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
                  Expanded(
                    child: AppText(
                      todo.totalCount > 0
                          ? 'Cập nhật danh sách — ${todo.doneCount}/${todo.totalCount} xong'
                          : 'Cập nhật danh sách',
                      variant: AppTextVariant.body,
                      fontSize: 16,
                      weight: FontWeight.w700,
                    ),
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
              if (todo.items.isEmpty)
                const AppText('Chưa có việc con nào.',
                    variant: AppTextVariant.caption,
                    color: AppColors.textSecondary)
              else
                Column(children: [
                  for (final item in todo.items) _buildRow(item, todo.canManage)
                ]),
              if (todo.canManage) ...[
                const SizedBox(height: AppDimens.space8),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Expanded(
                      child: AppTextField(
                        label: 'Thêm việc cần làm',
                        controller: _addController,
                        hint: 'Nhập nội dung rồi bấm Thêm',
                        textInputAction: TextInputAction.done,
                        onFieldSubmitted: (_) => _submitAdd(),
                        onChanged: (_) {
                          if (_addFieldError != null) {
                            setState(() => _addFieldError = null);
                          }
                        },
                      ),
                    ),
                    const SizedBox(width: AppDimens.space8),
                    ConstrainedBox(
                      constraints: const BoxConstraints(maxWidth: 120),
                      child: AppButton(
                        label: 'Thêm',
                        icon: PhosphorIconsRegular.plus,
                        isLoading: _adding,
                        onPressed: _adding ? null : _submitAdd,
                      ),
                    ),
                  ],
                ),
                if (_addFieldError != null)
                  Padding(
                    padding: const EdgeInsets.only(top: AppDimens.space4),
                    child: AppText(_addFieldError!,
                        variant: AppTextVariant.caption,
                        fontSize: 12,
                        color: AppColors.danger),
                  ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildRow(TodoItem item, bool canManage) {
    final busy = _busyIds.contains(item.id);

    if (_editingId == item.id) {
      return Padding(
        padding: const EdgeInsets.only(bottom: AppDimens.space12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            AppTextField(
              label: 'Sửa việc cần làm',
              controller: _editController,
              onChanged: (_) {
                if (_editFieldError != null) {
                  setState(() => _editFieldError = null);
                }
              },
            ),
            if (_editFieldError != null)
              Padding(
                padding: const EdgeInsets.only(top: AppDimens.space4),
                child: AppText(_editFieldError!,
                    variant: AppTextVariant.caption,
                    fontSize: 12,
                    color: AppColors.danger),
              ),
            const SizedBox(height: AppDimens.space8),
            Row(
              children: [
                Expanded(
                  child: AppButton(
                    label: 'Lưu',
                    isLoading: busy,
                    onPressed: busy ? null : () => _submitEdit(item),
                  ),
                ),
                const SizedBox(width: AppDimens.space8),
                Expanded(
                  child: AppButton(
                    label: 'Hủy',
                    type: AppButtonType.outline,
                    onPressed: busy
                        ? null
                        : () => setState(() {
                              _editingId = null;
                              _editFieldError = null;
                            }),
                  ),
                ),
              ],
            ),
          ],
        ),
      );
    }

    return Padding(
      padding: const EdgeInsets.only(bottom: AppDimens.space4),
      child: Row(
        children: [
          Expanded(
            child: canManage
                ? AppCheckbox(
                    label: item.content,
                    value: item.isDone,
                    strikeThrough: true,
                    onChanged: busy ? (_) {} : (_) => _toggle(item),
                  )
                : Row(
                    children: [
                      PhosphorIcon(
                        item.isDone
                            ? PhosphorIconsRegular.checkCircle
                            : PhosphorIconsRegular.circle,
                        size: 20,
                        color: item.isDone
                            ? AppTheme.statusSuccess
                            : AppColors.textFaint,
                      ),
                      const SizedBox(width: AppDimens.space8),
                      Expanded(
                        child: AppText(
                          item.content,
                          variant: AppTextVariant.body,
                          color: item.isDone ? AppColors.textSecondary : null,
                          decoration:
                              item.isDone ? TextDecoration.lineThrough : null,
                        ),
                      ),
                    ],
                  ),
          ),
          if (canManage)
            if (busy)
              const Padding(
                padding: EdgeInsets.symmetric(horizontal: AppDimens.space4),
                child: AppLoading(size: 20),
              )
            else
              Row(
                children: [
                  AppIconButton(
                    icon: PhosphorIconsRegular.pencilSimple,
                    tooltip: 'Sửa',
                    onPressed: () => _startEdit(item),
                  ),
                  const SizedBox(width: AppDimens.space8),
                  AppIconButton(
                    icon: PhosphorIconsRegular.trash,
                    color: AppTheme.statusDanger,
                    tooltip: 'Xoá',
                    onPressed: () => _confirmDelete(item),
                  ),
                ],
              ),
        ],
      ),
    );
  }
}
