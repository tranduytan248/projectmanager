import 'package:flutter/material.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_date_field.dart';
import '../../core/widgets/app_dropdown.dart';
import '../../core/widgets/app_rich_editor.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import 'private_tasks_models.dart';
import 'private_tasks_service.dart';

class PrivateTaskFormScreen extends StatefulWidget {
  const PrivateTaskFormScreen({
    super.key,
    this.initialItem,
    required this.members,
  });

  final PrivateTaskItem? initialItem;
  final List<AssigneeOption> members;

  @override
  State<PrivateTaskFormScreen> createState() => _PrivateTaskFormScreenState();
}

class _PrivateTaskFormScreenState extends State<PrivateTaskFormScreen> {
  final _service = PrivateTasksService();
  final _formKey = GlobalKey<FormState>();

  late TextEditingController _titleController;
  late AppRichEditorController _descController;

  int? _selectedAssigneeId;
  DateTime? _startDate;
  DateTime? _dueDate;
  double _bonusPercent = 0.5;
  String _priority = 'TrungBinh';
  String _state = 'ChuaBatDau';
  int _progress = 0;

  bool _isSubmitting = false;
  String? _errorMessage;

  bool get _isEdit => widget.initialItem != null;

  @override
  void initState() {
    super.initState();
    final item = widget.initialItem;
    if (item != null) {
      _titleController = TextEditingController(text: item.title);
      _descController = AppRichEditorController(text: item.description);
      _selectedAssigneeId = item.assigneeUserId > 0 ? item.assigneeUserId : null;
      _startDate = item.startDate;
      _dueDate = item.dueDate ?? DateTime.now().add(const Duration(days: 3));
      _bonusPercent = item.bonusPercent > 0 ? item.bonusPercent : 0.5;
      _priority = item.priority;
      _state = item.state;
      _progress = item.progress;
    } else {
      _titleController = TextEditingController();
      _descController = AppRichEditorController();
      _selectedAssigneeId = widget.members.isNotEmpty ? widget.members.first.userId : null;
      _startDate = DateTime.now();
      _dueDate = DateTime.now().add(const Duration(days: 3));
      _bonusPercent = 0.5;
      _priority = 'TrungBinh';
      _state = 'ChuaBatDau';
      _progress = 0;
    }
  }

  @override
  void dispose() {
    _titleController.dispose();
    _descController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedAssigneeId == null || _selectedAssigneeId! <= 0) {
      setState(() => _errorMessage = 'Vui lòng chọn người được giao việc.');
      return;
    }
    if (_dueDate == null) {
      setState(() => _errorMessage = 'Vui lòng chọn hạn hoàn thành.');
      return;
    }

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    final htmlDescription = _descController.toHtml();

    PrivateTaskApiResult result;
    if (_isEdit) {
      result = await _service.update(
        id: widget.initialItem!.id,
        title: _titleController.text.trim(),
        assigneeUserId: _selectedAssigneeId!,
        startDate: _startDate,
        dueDate: _dueDate!,
        bonusPercent: _bonusPercent,
        priority: _priority,
        description: htmlDescription,
        state: _state,
        progress: _progress,
      );
    } else {
      result = await _service.create(
        title: _titleController.text.trim(),
        assigneeUserId: _selectedAssigneeId!,
        startDate: _startDate,
        dueDate: _dueDate!,
        bonusPercent: _bonusPercent,
        priority: _priority,
        description: htmlDescription,
      );
    }

    if (!mounted) return;
    setState(() => _isSubmitting = false);

    if (result.isSuccess) {
      Navigator.of(context).pop(true);
    } else {
      setState(() => _errorMessage = result.error ?? 'Đã có lỗi xảy ra.');
    }
  }

  @override
  Widget build(BuildContext context) {
    final memberItems = <int, String>{
      for (final m in widget.members) m.userId: m.fullName,
    };

    const stateItems = <String, String>{
      'ChuaBatDau': 'Chưa làm',
      'DangLam': 'Đang làm',
      'TamDung': 'Tạm dừng',
      'HoanThanh': 'Hoàn thành',
      'Huy': 'Đã hủy',
    };

    return AppScaffold(
      appBar: AppAppBar(
        title: _isEdit ? 'Sửa việc riêng' : 'Giao việc riêng',
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(AppDimens.screenPadding),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (_errorMessage != null) ...[
                Container(
                  padding: const EdgeInsets.all(AppDimens.space12),
                  margin: const EdgeInsets.only(bottom: AppDimens.space16),
                  decoration: BoxDecoration(
                    color: AppColors.danger.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                    border: Border.all(color: AppColors.danger.withValues(alpha: 0.3)),
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.error_outline, color: AppColors.danger, size: 20),
                      const SizedBox(width: AppDimens.space8),
                      Expanded(
                        child: AppText(
                          _errorMessage!,
                          variant: AppTextVariant.caption,
                          color: AppColors.danger,
                        ),
                      ),
                    ],
                  ),
                ),
              ],

              // Người thực hiện
              AppDropdown<int>(
                label: 'Người thực hiện *',
                value: _selectedAssigneeId,
                items: memberItems,
                hint: 'Chọn người được giao việc...',
                onChanged: (val) => setState(() => _selectedAssigneeId = val),
              ),
              const SizedBox(height: AppDimens.space16),

              // Tên công việc
              AppTextField(
                label: 'Tên công việc',
                required: true,
                controller: _titleController,
                hint: 'Nhập tóm tắt nội dung công việc...',
                validator: (val) {
                  if (val == null || val.trim().isEmpty) {
                    return 'Vui lòng nhập tên công việc.';
                  }
                  return null;
                },
              ),
              const SizedBox(height: AppDimens.space16),

              // Ngày bắt đầu & Hạn hoàn thành
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: AppDateField(
                      label: 'Ngày bắt đầu',
                      value: _startDate,
                      onChanged: (d) => setState(() => _startDate = d),
                    ),
                  ),
                  const SizedBox(width: AppDimens.space12),
                  Expanded(
                    child: AppDateField(
                      label: 'Hạn hoàn thành',
                      required: true,
                      value: _dueDate,
                      onChanged: (d) => setState(() => _dueDate = d),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppDimens.space16),

              // Điểm cộng KPI (%)
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const AppText(
                    'Điểm cộng KPI',
                    variant: AppTextVariant.body,
                    weight: FontWeight.w600,
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: AppDimens.space8,
                      vertical: AppDimens.space4,
                    ),
                    decoration: BoxDecoration(
                      color: AppColors.primary.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(AppDimens.radiusPill),
                    ),
                    child: AppText(
                      '+${_bonusPercent.toStringAsFixed(1)}%',
                      variant: AppTextVariant.caption,
                      weight: FontWeight.bold,
                      color: AppColors.primary,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppDimens.space4),
              const AppText(
                'Điểm cộng thêm vào KPI tháng khi hoàn thành đúng hạn (0,2% — 1,5%).',
                variant: AppTextVariant.caption,
                color: AppColors.textSecondary,
              ),
              const SizedBox(height: AppDimens.space8),
              Wrap(
                spacing: AppDimens.space8,
                runSpacing: AppDimens.space8,
                children: [0.2, 0.3, 0.5, 0.8, 1.0, 1.2, 1.5].map((val) {
                  final isSelected = (_bonusPercent - val).abs() < 0.01;
                  return ChoiceChip(
                    label: AppText(
                      '${val.toStringAsFixed(1)}%',
                      variant: AppTextVariant.caption,
                      weight: isSelected ? FontWeight.bold : FontWeight.normal,
                      color: isSelected ? AppColors.textOnPrimary : AppColors.textPrimary,
                    ),
                    selected: isSelected,
                    selectedColor: AppColors.primary,
                    onSelected: (selected) {
                      if (selected) setState(() => _bonusPercent = val);
                    },
                  );
                }).toList(),
              ),
              const SizedBox(height: AppDimens.space16),

              // Độ ưu tiên
              const AppText(
                'Độ ưu tiên',
                variant: AppTextVariant.body,
                weight: FontWeight.w600,
              ),
              const SizedBox(height: AppDimens.space8),
              Row(
                children: [
                  _priorityChip('Cao', 'Cao', AppColors.danger),
                  const SizedBox(width: AppDimens.space8),
                  _priorityChip('TrungBinh', 'Trung bình', AppColors.primary),
                  const SizedBox(width: AppDimens.space8),
                  _priorityChip('Thap', 'Thấp', AppColors.textSecondary),
                ],
              ),
              const SizedBox(height: AppDimens.space16),

              if (_isEdit) ...[
                // Trạng thái (chỉ hiện khi sửa)
                AppDropdown<String>(
                  label: 'Trạng thái',
                  value: _state,
                  items: stateItems,
                  onChanged: (val) {
                    if (val != null) {
                      setState(() {
                        _state = val;
                        if (val == 'HoanThanh') _progress = 100;
                      });
                    }
                  },
                ),
                const SizedBox(height: AppDimens.space16),
              ],

              // Mô tả chi tiết
              AppRichEditor(
                label: 'Mô tả chi tiết',
                controller: _descController,
                minLines: 4,
                maxLines: 8,
                hint: 'Yêu cầu cụ thể, tài liệu tham khảo, kết quả mong đợi...',
              ),
              const SizedBox(height: AppDimens.space24),

              // Nút lưu
              AppButton(
                label: _isSubmitting
                    ? 'Đang lưu...'
                    : (_isEdit ? 'Cập nhật công việc' : 'Giao việc ngay'),
                onPressed: _isSubmitting ? null : _submit,
                isLoading: _isSubmitting,
                fullWidth: true,
                icon: Icons.check,
              ),
              const SizedBox(height: AppDimens.space24),
            ],
          ),
        ),
      ),
    );
  }

  Widget _priorityChip(String value, String label, Color color) {
    final isSelected = _priority == value;
    return Expanded(
      child: InkWell(
        onTap: () => setState(() => _priority = value),
        borderRadius: BorderRadius.circular(AppDimens.radiusSm),
        child: Container(
          padding: const EdgeInsets.symmetric(vertical: AppDimens.space12),
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: isSelected ? color.withValues(alpha: 0.12) : AppColors.surface,
            borderRadius: BorderRadius.circular(AppDimens.radiusSm),
            border: Border.all(
              color: isSelected ? color : AppColors.border,
              width: isSelected ? 1.5 : 1,
            ),
          ),
          child: AppText(
            label,
            variant: AppTextVariant.body,
            weight: isSelected ? FontWeight.bold : FontWeight.normal,
            color: isSelected ? color : AppColors.textSecondary,
          ),
        ),
      ),
    );
  }
}
