import 'package:flutter/material.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import '../leaves/leave_models.dart';

/// Hộp thoại duyệt hoặc từ chối đơn nghỉ phép.
class LeaveActionDialog extends StatefulWidget {
  const LeaveActionDialog({
    super.key,
    required this.item,
    required this.isApprove,
  });

  final LeaveRequestItem item;
  final bool isApprove;

  static Future<String?> show(
    BuildContext context, {
    required LeaveRequestItem item,
    required bool isApprove,
  }) {
    return showModalBottomSheet<String>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) => LeaveActionDialog(
        item: item,
        isApprove: isApprove,
      ),
    );
  }

  @override
  State<LeaveActionDialog> createState() => _LeaveActionDialogState();
}

class _LeaveActionDialogState extends State<LeaveActionDialog> {
  final _noteController = TextEditingController();
  final _formKey = GlobalKey<FormState>();

  @override
  void dispose() {
    _noteController.dispose();
    super.dispose();
  }

  void _onConfirm() {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    final text = _noteController.text.trim();
    Navigator.of(context).pop(text);
  }

  @override
  Widget build(BuildContext context) {
    final bottomInset = MediaQuery.of(context).viewInsets.bottom;

    return Container(
      decoration: const BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.vertical(
          top: Radius.circular(AppDimens.radiusLg),
        ),
      ),
      padding: EdgeInsets.fromLTRB(
        AppDimens.space16,
        AppDimens.space16,
        AppDimens.space16,
        AppDimens.space24 + bottomInset,
      ),
      child: Form(
        key: _formKey,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Thanh kéo trên modal
              Center(
                child: Container(
                  width: 40,
                  height: 4,
                  margin: const EdgeInsets.only(bottom: AppDimens.space16),
                  decoration: BoxDecoration(
                    color: AppColors.borderStrong,
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              ),

              // Tiêu đề modal
              Row(
                children: [
                  Container(
                    width: 40,
                    height: 40,
                    decoration: BoxDecoration(
                      color: widget.isApprove
                          ? AppColors.successSoft
                          : AppColors.dangerSoft,
                      shape: BoxShape.circle,
                    ),
                    child: Icon(
                      widget.isApprove ? Icons.check_circle_outline : Icons.cancel_outlined,
                      color: widget.isApprove ? AppColors.success : AppColors.danger,
                      size: 24,
                    ),
                  ),
                  const SizedBox(width: AppDimens.space12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        AppText(
                          widget.isApprove ? 'Duyệt đơn nghỉ phép' : 'Từ chối đơn nghỉ phép',
                          variant: AppTextVariant.heading,
                        ),
                        const SizedBox(height: AppDimens.space4),
                        AppText(
                          '${widget.item.userFullName ?? "Nhân viên"} • ${formatLeaveDays(widget.item.days)} ngày (${leaveKindLabel(widget.item.kind)})',
                          variant: AppTextVariant.caption,
                          color: AppColors.textSecondary,
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppDimens.space16),

              // Thông tin tóm tắt
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(AppDimens.space12),
                decoration: BoxDecoration(
                  color: AppColors.background,
                  borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                  border: Border.all(color: AppColors.border),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        const Icon(Icons.calendar_today_outlined, size: 16, color: AppColors.textSecondary),
                        const SizedBox(width: AppDimens.space8),
                        AppText(
                          '${widget.item.fromDate.day}/${widget.item.fromDate.month}/${widget.item.fromDate.year} – ${widget.item.toDate.day}/${widget.item.toDate.month}/${widget.item.toDate.year}',
                          variant: AppTextVariant.body,
                        ),
                      ],
                    ),
                    if (widget.item.reason != null && widget.item.reason!.isNotEmpty) ...[
                      const SizedBox(height: AppDimens.space8),
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Icon(Icons.notes_outlined, size: 16, color: AppColors.textSecondary),
                          const SizedBox(width: AppDimens.space8),
                          Expanded(
                            child: AppText(
                              'Lý do: ${widget.item.reason}',
                              variant: AppTextVariant.caption,
                              color: AppColors.textSecondary,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ],
                ),
              ),
              const SizedBox(height: AppDimens.space16),

              // Ô nhập ghi chú
              AppTextField(
                label: widget.isApprove ? 'Ý kiến duyệt (tùy chọn)' : 'Lý do từ chối *',
                hint: widget.isApprove
                    ? 'Nhập ý kiến hoặc dặn dò nhân sự...'
                    : 'Nhập lý do từ chối đơn nghỉ phép...',
                controller: _noteController,
                maxLines: 3,
                minLines: 2,
                validator: (val) {
                  if (!widget.isApprove && (val == null || val.trim().isEmpty)) {
                    return 'Vui lòng nhập lý do từ chối để người đăng ký nắm được.';
                  }
                  return null;
                },
              ),
              const SizedBox(height: AppDimens.space24),

              // Các nút thao tác
              Row(
                children: [
                  Expanded(
                    child: AppButton(
                      label: 'Đóng',
                      type: AppButtonType.outline,
                      onPressed: () => Navigator.of(context).pop(),
                    ),
                  ),
                  const SizedBox(width: AppDimens.space12),
                  Expanded(
                    flex: 2,
                    child: AppButton(
                      label: widget.isApprove ? 'Xác nhận duyệt' : 'Xác nhận từ chối',
                      type: widget.isApprove ? AppButtonType.primary : AppButtonType.danger,
                      icon: widget.isApprove ? Icons.check : Icons.close,
                      onPressed: _onConfirm,
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
