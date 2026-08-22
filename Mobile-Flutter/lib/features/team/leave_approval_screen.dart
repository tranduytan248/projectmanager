import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_card.dart';
import '../../core/widgets/app_dropdown.dart';
import '../../core/widgets/app_error_state.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import '../leaves/leave_models.dart';
import '../leaves/leave_service.dart';
import 'leave_action_dialog.dart';

/// Màn hình Duyệt nghỉ phép toàn Tổ (dành cho Quản lý Tổ và Quản trị viên).
class LeaveApprovalScreen extends StatefulWidget {
  const LeaveApprovalScreen({super.key});

  @override
  State<LeaveApprovalScreen> createState() => _LeaveApprovalScreenState();
}

class _LeaveApprovalScreenState extends State<LeaveApprovalScreen> {
  final _service = LeaveService();
  final _searchController = TextEditingController();

  LeaveApprovalsData? _data;
  bool _isLoading = true;
  String? _errorMessage;

  // Bộ lọc
  String _searchQuery = '';
  int _selectedUserId = 0;
  String _selectedState = LeaveStates.pending; // Mặc định là Chờ duyệt
  int _selectedYear = 0;

  // Thao tác đang xử lý
  int? _processingItemId;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadData() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final data = await _service.fetchApprovals(
        q: _searchQuery.isNotEmpty ? _searchQuery : null,
        userId: _selectedUserId > 0 ? _selectedUserId : null,
        state: _selectedState.isNotEmpty ? _selectedState : null,
        year: _selectedYear > 0 ? _selectedYear : null,
      );
      if (mounted) {
        setState(() {
          _data = data;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _errorMessage = 'Không thể tải danh sách duyệt nghỉ phép: $e';
          _isLoading = false;
        });
      }
    }
  }

  Future<void> _handleAction(LeaveRequestItem item, bool isApprove) async {
    final note = await LeaveActionDialog.show(
      context,
      item: item,
      isApprove: isApprove,
    );

    if (note == null) return; // Người dùng bấm đóng

    setState(() => _processingItemId = item.id);

    final targetState = isApprove ? LeaveStates.approved : LeaveStates.rejected;
    final result = await _service.setState(
      id: item.id,
      state: targetState,
      note: note,
    );

    if (!mounted) return;
    setState(() => _processingItemId = null);

    if (result.isSuccess) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            isApprove
                ? 'Đã duyệt đơn nghỉ ${formatLeaveDays(item.days)} ngày của ${item.userFullName ?? "nhân viên"}.'
                : 'Đã từ chối đơn nghỉ phép của ${item.userFullName ?? "nhân viên"}.',
          ),
          backgroundColor: isApprove ? AppColors.success : AppColors.danger,
        ),
      );
      _loadData();
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(result.error ?? 'Thao tác không thành công. Hãy thử lại.'),
          backgroundColor: AppColors.danger,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: AppAppBar(
        title: 'Duyệt nghỉ phép',
        leading: AppIconButton(
          icon: Icons.arrow_back,
          tooltip: 'Quay lại',
          onPressed: () => Navigator.of(context).pop(),
        ),
      ),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: _loadData,
          color: AppColors.primary,
          child: Column(
            children: [
              // Thanh thống kê & bộ lọc cố định phía trên
              _buildHeader(),

              // Nội dung danh sách
              Expanded(
                child: _buildContent(),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildHeader() {
    final memberItems = <int, String>{
      0: 'Tất cả nhân sự',
      if (_data != null)
        for (final m in _data!.members) m.userId: m.fullName,
    };

    final yearItems = <int, String>{
      0: 'Tất cả năm',
      if (_data != null)
        for (final y in _data!.years) y: 'Năm $y',
    };

    return Container(
      color: AppColors.surface,
      padding: const EdgeInsets.fromLTRB(
        AppDimens.space16,
        AppDimens.space12,
        AppDimens.space16,
        AppDimens.space12,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Thống kê nhanh theo trạng thái
          if (_data != null) ...[
            SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                  _buildStatTab(
                    label: 'Chờ duyệt',
                    count: _data!.pendingCount,
                    stateValue: LeaveStates.pending,
                    color: AppColors.warning,
                    softColor: AppColors.warningSoft,
                  ),
                  const SizedBox(width: AppDimens.space8),
                  _buildStatTab(
                    label: 'Đã duyệt',
                    count: _data!.approvedCount,
                    stateValue: LeaveStates.approved,
                    color: AppColors.success,
                    softColor: AppColors.successSoft,
                  ),
                  const SizedBox(width: AppDimens.space8),
                  _buildStatTab(
                    label: 'Từ chối',
                    count: _data!.rejectedCount,
                    stateValue: LeaveStates.rejected,
                    color: AppColors.danger,
                    softColor: AppColors.dangerSoft,
                  ),
                  const SizedBox(width: AppDimens.space8),
                  _buildStatTab(
                    label: 'Tất cả',
                    count: _data!.totalCount,
                    stateValue: '',
                    color: AppColors.primary,
                    softColor: AppColors.primarySoft,
                  ),
                ],
              ),
            ),
            const SizedBox(height: AppDimens.space12),
          ],

          // Thanh tìm kiếm
          AppTextField(
            label: 'Tìm kiếm',
            hint: 'Tìm theo tên nhân sự, lý do...',
            controller: _searchController,
            prefixIcon: Icons.search,
            onChanged: (val) {
              setState(() => _searchQuery = val.trim());
              _loadData();
            },
          ),
          const SizedBox(height: AppDimens.space8),

          // Bộ lọc Nhân sự và Năm
          Row(
            children: [
              // Lọc Nhân sự
              Expanded(
                flex: 3,
                child: AppDropdown<int>(
                  label: 'Nhân sự',
                  value: _selectedUserId,
                  items: memberItems,
                  onChanged: (val) {
                    if (val != null && val != _selectedUserId) {
                      setState(() => _selectedUserId = val);
                      _loadData();
                    }
                  },
                ),
              ),
              const SizedBox(width: AppDimens.space8),

              // Lọc Năm
              Expanded(
                flex: 2,
                child: AppDropdown<int>(
                  label: 'Năm',
                  value: _selectedYear,
                  items: yearItems,
                  onChanged: (val) {
                    if (val != null && val != _selectedYear) {
                      setState(() => _selectedYear = val);
                      _loadData();
                    }
                  },
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildStatTab({
    required String label,
    required int count,
    required String stateValue,
    required Color color,
    required Color softColor,
  }) {
    final isSelected = _selectedState == stateValue;

    return InkWell(
      onTap: () {
        if (_selectedState != stateValue) {
          setState(() => _selectedState = stateValue);
          _loadData();
        }
      },
      borderRadius: BorderRadius.circular(AppDimens.radiusPill),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        padding: const EdgeInsets.symmetric(
          horizontal: AppDimens.space12,
          vertical: AppDimens.space8,
        ),
        decoration: BoxDecoration(
          color: isSelected ? softColor : AppColors.background,
          borderRadius: BorderRadius.circular(AppDimens.radiusPill),
          border: Border.all(
            color: isSelected ? color : AppColors.border,
            width: isSelected ? 1.5 : 1.0,
          ),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 8,
              height: 8,
              decoration: BoxDecoration(
                color: color,
                shape: BoxShape.circle,
              ),
            ),
            const SizedBox(width: AppDimens.space8),
            AppText(
              label,
              variant: AppTextVariant.caption,
              color: isSelected ? color : AppColors.textPrimary,
              weight: isSelected ? FontWeight.w700 : FontWeight.w500,
            ),
            const SizedBox(width: AppDimens.space4),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
              decoration: BoxDecoration(
                color: isSelected ? color : AppColors.borderStrong,
                borderRadius: BorderRadius.circular(10),
              ),
              child: Text(
                '$count',
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.bold,
                  color: isSelected ? Colors.white : AppColors.textSecondary,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildContent() {
    if (_isLoading) {
      return const Center(
        child: AppLoading(),
      );
    }

    if (_errorMessage != null) {
      return Center(
        child: AppErrorState(
          message: _errorMessage!,
          onRetry: _loadData,
        ),
      );
    }

    final items = _data?.items ?? [];

    if (items.isEmpty) {
      return Center(
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          child: Padding(
            padding: const EdgeInsets.all(AppDimens.space32),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Container(
                  width: 72,
                  height: 72,
                  decoration: const BoxDecoration(
                    color: AppColors.primarySoft,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Icons.event_available_outlined,
                    size: 36,
                    color: AppColors.primary,
                  ),
                ),
                const SizedBox(height: AppDimens.space16),
                const AppText(
                  'Không có đơn nghỉ phép nào',
                  variant: AppTextVariant.heading,
                ),
                const SizedBox(height: AppDimens.space8),
                AppText(
                  _selectedState.isNotEmpty
                      ? 'Không tìm thấy đơn nào ở trạng thái "${leaveStateLabel(_selectedState)}".'
                      : 'Toàn bộ đơn nghỉ phép của Tổ sẽ hiển thị tại đây.',
                  variant: AppTextVariant.body,
                  color: AppColors.textSecondary,
                  align: TextAlign.center,
                ),
              ],
            ),
          ),
        ),
      );
    }

    return ListView.separated(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(
        AppDimens.space16,
        AppDimens.space8,
        AppDimens.space16,
        AppDimens.space24,
      ),
      itemCount: items.length,
      separatorBuilder: (_, __) => const SizedBox(height: AppDimens.space12),
      itemBuilder: (context, index) => _buildLeaveCard(items[index]),
    );
  }

  Widget _buildLeaveCard(LeaveRequestItem item) {
    final isPending = item.state == LeaveStates.pending;
    final isApproved = item.state == LeaveStates.approved;
    final isRejected = item.state == LeaveStates.rejected;
    final isProcessing = _processingItemId == item.id;

    Color stateColor;
    Color stateSoftColor;
    switch (item.state) {
      case LeaveStates.approved:
        stateColor = AppColors.success;
        stateSoftColor = AppColors.successSoft;
        break;
      case LeaveStates.rejected:
        stateColor = AppColors.danger;
        stateSoftColor = AppColors.dangerSoft;
        break;
      case LeaveStates.pending:
        stateColor = AppColors.warning;
        stateSoftColor = AppColors.warningSoft;
        break;
      default:
        stateColor = AppColors.textSecondary;
        stateSoftColor = AppColors.border;
    }

    final dateFmt = DateFormat('dd/MM/yyyy');
    final rangeText = '${dateFmt.format(item.fromDate)} – ${dateFmt.format(item.toDate)}';

    return AppCard(
      padding: const EdgeInsets.all(AppDimens.space16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header: Người làm đơn & Trạng thái
          Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              // Avatar
              CircleAvatar(
                radius: 20,
                backgroundColor: AppColors.primarySoft,
                child: Text(
                  _getInitials(item.userFullName ?? 'NV'),
                  style: const TextStyle(
                    color: AppColors.primary,
                    fontWeight: FontWeight.bold,
                    fontSize: 14,
                  ),
                ),
              ),
              const SizedBox(width: AppDimens.space12),

              // Tên & Ngày tạo đơn
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    AppText(
                      item.userFullName ?? 'Nhân sự',
                      variant: AppTextVariant.heading,
                    ),
                    const SizedBox(height: 2),
                    AppText(
                      'Gửi lúc ${DateFormat("HH:mm dd/MM/yyyy").format(item.createdAt)}',
                      variant: AppTextVariant.caption,
                      color: AppColors.textSecondary,
                    ),
                  ],
                ),
              ),

              // Badge trạng thái
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: AppDimens.space8,
                  vertical: AppDimens.space4,
                ),
                decoration: BoxDecoration(
                  color: stateSoftColor,
                  borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                  border: Border.all(color: stateColor.withValues(alpha: 0.3)),
                ),
                child: AppText(
                  leaveStateLabel(item.state),
                  variant: AppTextVariant.caption,
                  color: stateColor,
                  weight: FontWeight.w700,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppDimens.space12),

          const Divider(height: 1, color: AppColors.border),
          const SizedBox(height: AppDimens.space12),

          // Thông tin kỳ nghỉ
          Row(
            children: [
              const Icon(Icons.date_range_outlined, size: 18, color: AppColors.primary),
              const SizedBox(width: AppDimens.space8),
              Expanded(
                child: AppText(
                  rangeText,
                  variant: AppTextVariant.body,
                  weight: FontWeight.w600,
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: AppColors.background,
                  borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                  border: Border.all(color: AppColors.border),
                ),
                child: AppText(
                  '${formatLeaveDays(item.days)} ngày • ${leaveKindLabel(item.kind)}',
                  variant: AppTextVariant.caption,
                  color: AppColors.textSecondary,
                  weight: FontWeight.w600,
                ),
              ),
            ],
          ),

          // Chi tiết nửa ngày
          if (item.isHalfDay && item.halfDaySession != null) ...[
            const SizedBox(height: AppDimens.space8),
            Padding(
              padding: const EdgeInsets.only(left: 26),
              child: AppText(
                'Nghỉ ${halfDaySessionLabel(item.halfDaySession).toLowerCase()}',
                variant: AppTextVariant.caption,
                color: AppColors.textSecondary,
              ),
            ),
          ],

          // Lý do nghỉ
          if (item.reason != null && item.reason!.isNotEmpty) ...[
            const SizedBox(height: AppDimens.space8),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(AppDimens.space8),
              decoration: BoxDecoration(
                color: AppColors.background,
                borderRadius: BorderRadius.circular(AppDimens.radiusSm),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(Icons.chat_bubble_outline, size: 15, color: AppColors.textSecondary),
                  const SizedBox(width: AppDimens.space8),
                  Expanded(
                    child: AppText(
                      item.reason!,
                      variant: AppTextVariant.caption,
                      color: AppColors.textPrimary,
                    ),
                  ),
                ],
              ),
            ),
          ],

          // Thông tin kết quả duyệt (nếu đã xử lý)
          if (isApproved || isRejected) ...[
            const SizedBox(height: AppDimens.space12),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(AppDimens.space8),
              decoration: BoxDecoration(
                color: stateSoftColor,
                borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                border: Border.all(color: stateColor.withValues(alpha: 0.2)),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(
                        isApproved ? Icons.check_circle : Icons.cancel,
                        size: 16,
                        color: stateColor,
                      ),
                      const SizedBox(width: AppDimens.space8),
                      Expanded(
                        child: AppText(
                          '${isApproved ? "Đã duyệt" : "Từ chối"} bởi ${item.approvedByName ?? "Quản lý"}${item.approvedAt != null ? " lúc ${DateFormat("HH:mm dd/MM/yyyy").format(item.approvedAt!)}" : ""}',
                          variant: AppTextVariant.caption,
                          color: stateColor,
                          weight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                  if (item.approverNote != null && item.approverNote!.isNotEmpty) ...[
                    const SizedBox(height: AppDimens.space4),
                    Padding(
                      padding: const EdgeInsets.only(left: 24),
                      child: AppText(
                        '${isApproved ? "Ý kiến" : "Lý do"}: ${item.approverNote}',
                        variant: AppTextVariant.caption,
                        color: AppColors.textSecondary,
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ],

          // Nút thao tác (khi đơn đang Chờ duyệt)
          if (isPending) ...[
            const SizedBox(height: AppDimens.space16),
            if (isProcessing)
              const Center(
                child: Padding(
                  padding: EdgeInsets.all(AppDimens.space8),
                  child: AppLoading(size: 24),
                ),
              )
            else
              Row(
                children: [
                  // Nút Từ chối
                  Expanded(
                    child: AppButton(
                      label: 'Từ chối',
                      type: AppButtonType.outline,
                      icon: Icons.close,
                      onPressed: () => _handleAction(item, false),
                    ),
                  ),
                  const SizedBox(width: AppDimens.space12),

                  // Nút Duyệt
                  Expanded(
                    child: AppButton(
                      label: 'Duyệt đơn',
                      type: AppButtonType.primary,
                      icon: Icons.check,
                      onPressed: () => _handleAction(item, true),
                    ),
                  ),
                ],
              ),
          ],
        ],
      ),
    );
  }

  String _getInitials(String fullName) {
    final parts = fullName.trim().split(RegExp(r'\s+'));
    if (parts.isEmpty) return 'NV';
    if (parts.length == 1) return parts.first.substring(0, 1).toUpperCase();
    return (parts.first.substring(0, 1) + parts.last.substring(0, 1)).toUpperCase();
  }
}
