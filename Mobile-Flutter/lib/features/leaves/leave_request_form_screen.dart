import 'package:flutter/material.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_checkbox.dart';
import '../../core/widgets/app_date_field.dart';
import '../../core/widgets/app_dropdown.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import 'leave_models.dart';
import 'leave_service.dart';

/// Man "Đăng ký nghỉ phép" / "Sửa đơn nghỉ phép" — full-screen, cung tinh than voi AddTaskScreen/
/// AddProjectScreen. [existing] == null la che do TAO MOI (goi Create), khac null la che do SUA
/// (goi Update, chi thanh cong khi don con o trang thai Cho duyet — backend tu choi neu khong).
/// Tra ve true khi luu thanh cong de man danh sach tu _reload().
Future<bool?> pushLeaveFormScreen(
  BuildContext context, {
  required LeaveService service,
  LeaveRequestItem? existing,
}) {
  return Navigator.of(context).push<bool>(
    MaterialPageRoute(
      builder: (context) =>
          LeaveRequestFormScreen(service: service, existing: existing),
    ),
  );
}

class LeaveRequestFormScreen extends StatefulWidget {
  const LeaveRequestFormScreen({super.key, required this.service, this.existing});

  final LeaveService service;
  final LeaveRequestItem? existing;

  @override
  State<LeaveRequestFormScreen> createState() => _LeaveRequestFormScreenState();
}

class _LeaveRequestFormScreenState extends State<LeaveRequestFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _reasonController = TextEditingController();

  late String _kind;
  late DateTime? _fromDate;
  late DateTime? _toDate;
  late bool _isHalfDay;
  late String _halfDaySession;
  String? _dateError;
  bool _submitting = false;

  bool get _isEditing => widget.existing != null;

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    if (existing != null) {
      _kind = existing.kind;
      _fromDate = existing.fromDate;
      _toDate = existing.toDate;
      _isHalfDay = existing.isHalfDay;
      _halfDaySession = existing.halfDaySession ?? HalfDaySessions.morning;
      _reasonController.text = existing.reason ?? '';
    } else {
      // Mac dinh hom nay cho ca hai — khop dung "Số ngày nghỉ" hien san 1 ngày công tren man mau
      // web ngay khi vua mo form, truoc khi nguoi dung tu doi lai ngay.
      final today = DateTime.now();
      _fromDate = DateTime(today.year, today.month, today.day);
      _toDate = _fromDate;
      _kind = LeaveKinds.annual;
      _isHalfDay = false;
      _halfDaySession = HalfDaySessions.morning;
    }
  }

  @override
  void dispose() {
    _reasonController.dispose();
    super.dispose();
  }

  bool _isSameDate(DateTime a, DateTime b) =>
      a.year == b.year && a.month == b.month && a.day == b.day;

  double get _previewDays {
    if (_fromDate == null || _toDate == null) return 0;
    return calcLeaveDays(from: _fromDate!, to: _toDate!, isHalfDay: _isHalfDay);
  }

  String? _computeDateError() {
    if (_fromDate != null && _toDate != null && _toDate!.isBefore(_fromDate!)) {
      return 'Ngày kết thúc phải từ ngày bắt đầu trở đi.';
    }
    return null;
  }

  void _onFromDateChanged(DateTime d) {
    setState(() {
      _fromDate = d;
      _dateError = _computeDateError();
    });
    _maybeAutoUntickHalfDay();
  }

  void _onToDateChanged(DateTime d) {
    setState(() {
      _toDate = d;
      _dateError = _computeDateError();
    });
    _maybeAutoUntickHalfDay();
  }

  /// Nghi nua ngay chi hop le khi Tu ngay va Den ngay trung nhau — neu nguoi dung da tick roi lai
  /// doi ngay khac nhau thi tu bo tick kem hint ngan, giu trai nghiem giong web (JS an field lien
  /// quan khi khong hop le) thay vi de nguoi dung tu phat hien loi khi bam Gui.
  void _maybeAutoUntickHalfDay() {
    if (!_isHalfDay || _fromDate == null || _toDate == null) return;
    if (_isSameDate(_fromDate!, _toDate!)) return;

    setState(() => _isHalfDay = false);
    ToastService.show(
      'Đã bỏ chọn "Nghỉ nửa ngày" vì Từ ngày và Đến ngày khác nhau.',
      type: ToastType.warning,
    );
  }

  void _onHalfDayChanged(bool value) {
    if (value &&
        !(_fromDate != null && _toDate != null && _isSameDate(_fromDate!, _toDate!))) {
      ToastService.show(
        'Chỉ dùng "Nghỉ nửa ngày" khi Từ ngày và Đến ngày là cùng một ngày.',
        type: ToastType.warning,
      );
      return;
    }
    setState(() => _isHalfDay = value);
  }

  Future<void> _submit() async {
    final dateError = _computeDateError();
    setState(() => _dateError = dateError);
    if (dateError != null || _fromDate == null || _toDate == null) return;

    setState(() => _submitting = true);
    final result = _isEditing
        ? await widget.service.update(
            id: widget.existing!.id,
            kind: _kind,
            fromDate: _fromDate!,
            toDate: _toDate!,
            isHalfDay: _isHalfDay,
            halfDaySession: _isHalfDay ? _halfDaySession : null,
            reason: _reasonController.text.trim(),
          )
        : await widget.service.create(
            kind: _kind,
            fromDate: _fromDate!,
            toDate: _toDate!,
            isHalfDay: _isHalfDay,
            halfDaySession: _isHalfDay ? _halfDaySession : null,
            reason: _reasonController.text.trim(),
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
        title: _isEditing ? 'Sửa đơn nghỉ phép' : 'Đăng ký nghỉ phép',
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
                AppDropdown<String>(
                  label: 'Loại nghỉ',
                  value: _kind,
                  items: {for (final k in LeaveKinds.all) k: leaveKindLabel(k)},
                  onChanged: (v) => setState(() => _kind = v ?? LeaveKinds.annual),
                ),
                const SizedBox(height: AppDimens.space16),
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(AppDimens.space16),
                  decoration: BoxDecoration(
                    color: AppColors.primarySoft,
                    borderRadius: BorderRadius.circular(AppDimens.radiusMd),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const AppText('Số ngày nghỉ',
                          variant: AppTextVariant.caption,
                          color: AppColors.textSecondary),
                      const SizedBox(height: AppDimens.space4),
                      AppText('${formatLeaveDays(_previewDays)} ngày công',
                          variant: AppTextVariant.title,
                          fontSize: 22,
                          weight: FontWeight.w800,
                          color: AppColors.primary),
                      const SizedBox(height: AppDimens.space4),
                      const AppText('Đã bỏ Thứ 7 và Chủ nhật.',
                          variant: AppTextVariant.caption,
                          color: AppColors.textSecondary),
                    ],
                  ),
                ),
                const SizedBox(height: AppDimens.space16),
                AppDateField(
                  label: 'Từ ngày',
                  required: true,
                  value: _fromDate,
                  onChanged: _onFromDateChanged,
                ),
                const SizedBox(height: AppDimens.space12),
                AppDateField(
                  label: 'Đến ngày',
                  required: true,
                  value: _toDate,
                  onChanged: _onToDateChanged,
                ),
                const SizedBox(height: AppDimens.space4),
                if (_dateError != null)
                  Padding(
                    padding: const EdgeInsets.only(bottom: AppDimens.space4),
                    child: AppText(_dateError!,
                        variant: AppTextVariant.caption,
                        fontSize: 12,
                        color: AppColors.danger),
                  ),
                const AppText('Nghỉ một ngày thì để trùng ngày bắt đầu.',
                    variant: AppTextVariant.caption, color: AppColors.textSecondary),
                const SizedBox(height: AppDimens.space16),
                AppCheckbox(
                  label: 'Nghỉ nửa ngày (một buổi)',
                  value: _isHalfDay,
                  onChanged: _onHalfDayChanged,
                ),
                const Padding(
                  padding: EdgeInsets.only(left: AppDimens.space4),
                  child: AppText(
                    'Chỉ dùng khi ngày bắt đầu và kết thúc là cùng một ngày — khi đó tính 0,5 ngày.',
                    variant: AppTextVariant.caption,
                    color: AppColors.textSecondary,
                  ),
                ),
                if (_isHalfDay) ...[
                  const SizedBox(height: AppDimens.space12),
                  AppDropdown<String>(
                    label: 'Buổi',
                    value: _halfDaySession,
                    items: const {
                      HalfDaySessions.morning: 'Buổi sáng',
                      HalfDaySessions.afternoon: 'Buổi chiều',
                    },
                    onChanged: (v) =>
                        setState(() => _halfDaySession = v ?? HalfDaySessions.morning),
                  ),
                ],
                const SizedBox(height: AppDimens.space16),
                AppTextField(
                  label: 'Lý do',
                  hint: 'Lý do nghỉ (không bắt buộc)',
                  controller: _reasonController,
                  maxLines: 3,
                  keyboardType: TextInputType.multiline,
                ),
                const SizedBox(height: AppDimens.space24),
                Row(
                  children: [
                    Expanded(
                      child: AppButton(
                        label: 'Huỷ',
                        type: AppButtonType.outline,
                        onPressed: () => Navigator.of(context).pop(),
                      ),
                    ),
                    const SizedBox(width: AppDimens.space12),
                    Expanded(
                      child: AppButton(
                        label: _isEditing ? 'Lưu thay đổi' : 'Gửi đơn',
                        isLoading: _submitting,
                        onPressed: _submitting ? null : _submit,
                      ),
                    ),
                  ],
                ),
                if (!_isEditing) ...[
                  const SizedBox(height: AppDimens.space12),
                  const AppText(
                    'Đơn gửi đi sẽ ở trạng thái "Chờ duyệt" cho tới khi Quản lý Tổ duyệt.',
                    variant: AppTextVariant.caption,
                    color: AppColors.textSecondary,
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}
