import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_dropdown.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_rich_editor.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import 'checklist_models.dart';
import 'checklist_service.dart';

/// Man "Thêm công việc mới" — tach thanh full-screen rieng (truoc day la bottom sheet
/// `_AddTaskSheet` trong checklist_board_screen.dart), gop them khoi "Việc cần làm" ngay luc tao
/// (danh sach todo nhap tai cho, gui kem trong CUNG mot request voi ChecklistApiController.Create
/// — xem quyet dinh trong Memory.md, muc "Thêm công việc mới" full-screen kèm todo-list). Truong
/// "Mô tả" dung AppRichEditor (dam/nghieng/gach chan/danh sach/link) thay vi o van ban thuong, cung
/// bo soan thao voi khoi Trao doi. Tra ve true khi tao thanh cong de man Checklist tu _reload().
Future<bool?> pushAddTaskScreen(
  BuildContext context, {
  required ChecklistService service,
  required int projectId,
  required List<AssigneeOption> assignees,
}) {
  return Navigator.of(context).push<bool>(
    MaterialPageRoute(
      builder: (context) => AddTaskScreen(
        service: service,
        projectId: projectId,
        assignees: assignees,
      ),
    ),
  );
}

class AddTaskScreen extends StatefulWidget {
  const AddTaskScreen({
    super.key,
    required this.service,
    required this.projectId,
    required this.assignees,
  });

  final ChecklistService service;
  final int projectId;
  final List<AssigneeOption> assignees;

  @override
  State<AddTaskScreen> createState() => _AddTaskScreenState();
}

class _AddTaskScreenState extends State<AddTaskScreen> {
  final _formKey = GlobalKey<FormState>();
  final _titleController = TextEditingController();
  final _codeController = TextEditingController();
  final _descriptionController = AppRichEditorController();
  final _startDateController = TextEditingController();
  final _dueDateController = TextEditingController();
  final _todoInputController = TextEditingController();

  String _kind = 'Checklist';
  String _priority = 'TrungBinh';
  int _assigneeUserId = 0;
  // Mac dinh hom nay — dung truoc day backend tu gan lang le, gio hien tuong minh cho nguoi
  // dung thay va doi lai duoc (yeu cau bo sung sau khi da lam xong ban rut gon).
  DateTime _startDate = DateTime.now();
  DateTime? _dueDate;
  bool _submitting = false;
  final List<String> _todos = [];

  @override
  void initState() {
    super.initState();
    _startDateController.text = DateFormat('dd/MM/yyyy').format(_startDate);
  }

  @override
  void dispose() {
    _titleController.dispose();
    _codeController.dispose();
    _descriptionController.dispose();
    _startDateController.dispose();
    _dueDateController.dispose();
    _todoInputController.dispose();
    super.dispose();
  }

  Future<void> _pickStartDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _startDate,
      firstDate: DateTime.now().subtract(const Duration(days: 365)),
      lastDate: DateTime.now().add(const Duration(days: 365 * 3)),
    );
    if (picked != null) {
      setState(() {
        _startDate = picked;
        _startDateController.text = DateFormat('dd/MM/yyyy').format(picked);
      });
    }
  }

  Future<void> _pickDueDate() async {
    final startDay = DateTime(_startDate.year, _startDate.month, _startDate.day);
    final initial = _dueDate ?? _startDate.add(const Duration(days: 7));
    final picked = await showDatePicker(
      context: context,
      // Han hoan thanh khong duoc som hon Ngay bat dau — chan tu luc chon, khong doi den luc
      // validate moi bao loi (yeu cau bo sung sau khi Ngay bat dau tro thanh truong tu chinh).
      initialDate: initial.isBefore(startDay) ? startDay : initial,
      firstDate: startDay,
      lastDate: DateTime.now().add(const Duration(days: 365 * 3)),
    );
    if (picked != null) {
      setState(() {
        _dueDate = picked;
        _dueDateController.text = DateFormat('dd/MM/yyyy').format(picked);
      });
    }
  }

  void _addTodo() {
    final text = _todoInputController.text.trim();
    if (text.isEmpty) return;
    setState(() {
      _todos.add(text);
      _todoInputController.clear();
    });
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;

    setState(() => _submitting = true);
    final result = await widget.service.create(
      projectId: widget.projectId,
      title: _titleController.text.trim(),
      code: _codeController.text.trim(),
      kind: _kind,
      priority: _priority,
      assigneeUserId: _assigneeUserId,
      startDate: _startDate,
      dueDate: _dueDate!,
      description: _descriptionController.toHtml(),
      todos: _todos,
    );
    if (!mounted) return;
    setState(() => _submitting = false);

    if (result.isSuccess) {
      Navigator.of(context).pop(true);
    } else {
      ToastService.show(result.error!, type: ToastType.error);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: AppAppBar(
        title: 'Thêm công việc mới',
        leading: AppIconButton(
          icon: PhosphorIconsRegular.arrowLeft,
          tooltip: 'Quay lại',
          onPressed: () => Navigator.of(context).pop(),
        ),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                AppTextField(
                  label: 'Tên công việc',
                  required: true,
                  controller: _titleController,
                  validator: (v) =>
                      (v == null || v.trim().isEmpty) ? 'Vui lòng nhập tên công việc' : null,
                ),
                const SizedBox(height: AppDimens.space12),
                AppTextField(
                  label: 'Mã việc (không bắt buộc)',
                  controller: _codeController,
                ),
                const SizedBox(height: AppDimens.space12),
                Row(
                  children: [
                    Expanded(
                      child: AppDropdown<String>(
                        label: 'Loại việc',
                        value: _kind,
                        items: const {'Checklist': 'Checklist', 'HoTro': 'Hỗ trợ'},
                        onChanged: (v) => setState(() => _kind = v!),
                      ),
                    ),
                    const SizedBox(width: AppDimens.space12),
                    Expanded(
                      child: AppDropdown<String>(
                        label: 'Ưu tiên',
                        value: _priority,
                        items: const {'Cao': 'Cao', 'TrungBinh': 'Trung bình', 'Thap': 'Thấp'},
                        onChanged: (v) => setState(() => _priority = v!),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: AppDimens.space12),
                AppDropdown<int>(
                  label: 'Người thực hiện',
                  value: _assigneeUserId,
                  items: {
                    0: '— Chưa giao —',
                    for (final a in widget.assignees) a.userId: a.fullName,
                  },
                  onChanged: (v) => setState(() => _assigneeUserId = v ?? 0),
                ),
                const SizedBox(height: AppDimens.space12),
                AppTextField(
                  label: 'Ngày bắt đầu',
                  controller: _startDateController,
                  hint: 'Chọn ngày',
                  readOnly: true,
                  onTap: _pickStartDate,
                  suffixIcon: PhosphorIconsRegular.calendarBlank,
                ),
                const SizedBox(height: AppDimens.space12),
                AppTextField(
                  label: 'Hạn hoàn thành',
                  required: true,
                  controller: _dueDateController,
                  hint: 'Chọn ngày',
                  readOnly: true,
                  onTap: _pickDueDate,
                  suffixIcon: PhosphorIconsRegular.calendarCheck,
                  validator: (v) {
                    if (v == null || v.isEmpty) return 'Vui lòng chọn hạn hoàn thành';
                    if (_dueDate != null &&
                        _dueDate!.isBefore(
                            DateTime(_startDate.year, _startDate.month, _startDate.day))) {
                      return 'Hạn hoàn thành không được sớm hơn ngày bắt đầu';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: AppDimens.space12),
                AppRichEditor(
                  controller: _descriptionController,
                  label: 'Mô tả (không bắt buộc)',
                  hint: 'Nhập mô tả công việc...',
                  minLines: 3,
                  maxLines: 8,
                ),
                const SizedBox(height: AppDimens.space24),
                const AppText('Việc cần làm',
                    variant: AppTextVariant.body, fontSize: 15, weight: FontWeight.w700),
                const SizedBox(height: AppDimens.space4),
                const AppText(
                  'Không bắt buộc — thêm ngay các việc con, hoặc bổ sung sau trong Chi tiết công việc.',
                  variant: AppTextVariant.caption,
                  fontSize: 12,
                  color: AppColors.textSecondary,
                ),
                const SizedBox(height: AppDimens.space12),
                for (var i = 0; i < _todos.length; i++) _TodoDraftRow(
                  content: _todos[i],
                  onRemove: () => setState(() => _todos.removeAt(i)),
                ),
                if (_todos.isNotEmpty) const SizedBox(height: AppDimens.space8),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Expanded(
                      child: AppTextField(
                        label: 'Thêm việc cần làm',
                        controller: _todoInputController,
                        hint: 'Nhập nội dung rồi bấm Thêm',
                        textInputAction: TextInputAction.done,
                        onFieldSubmitted: (_) => _addTodo(),
                      ),
                    ),
                    const SizedBox(width: AppDimens.space8),
                    ConstrainedBox(
                      constraints: const BoxConstraints(maxWidth: 120),
                      child: AppButton(
                        label: 'Thêm',
                        icon: PhosphorIconsRegular.plus,
                        onPressed: _addTodo,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: AppDimens.space24),
                AppButton(
                  label: 'Thêm công việc',
                  fullWidth: true,
                  isLoading: _submitting,
                  onPressed: _submitting ? null : _submit,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _TodoDraftRow extends StatelessWidget {
  const _TodoDraftRow({required this.content, required this.onRemove});

  final String content;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AppDimens.space8),
      child: Container(
        padding: const EdgeInsets.symmetric(
            horizontal: AppDimens.space12, vertical: AppDimens.space8),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(AppDimens.radiusSm),
          border: Border.all(color: AppColors.border),
        ),
        child: Row(
          children: [
            const PhosphorIcon(PhosphorIconsRegular.circleDashed,
                size: 16, color: AppColors.textSecondary),
            const SizedBox(width: AppDimens.space8),
            Expanded(
              child: AppText(content,
                  variant: AppTextVariant.body, fontSize: 13.5),
            ),
            AppIconButton(
              icon: PhosphorIconsRegular.x,
              tooltip: 'Bỏ dòng này',
              onPressed: onRemove,
            ),
          ],
        ),
      ),
    );
  }
}
