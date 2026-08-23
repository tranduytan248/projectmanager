import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';
import 'package:provider/provider.dart';

import '../../config/app_theme.dart';
import '../../core/classes/route_manager.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_card.dart';
import '../../core/widgets/app_checkbox.dart';
import '../../core/widgets/app_dropdown.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import '../../shared/widgets/app_bottom_nav.dart';
import '../app_routes.dart';
import '../auth/auth_provider.dart';
import 'add_project_screen.dart';
import 'my_projects_models.dart';
import 'my_projects_service.dart';

class MyProjectsScreen extends StatefulWidget {
  const MyProjectsScreen(
      {super.key, this.scope = 'mine', this.initialRoleFilter});

  /// "team": toan bo du an cua Ca To (dung khi mo tu the "Du an dang chay" tren Dashboard) thay
  /// vi chi du an cua rieng minh — server tu am tham tra ve "mine" cho tai khoan khong co quyen.
  final String scope;

  /// Gia tri 'pm' de loc san "Toi lam PM" khi mo tu the "Du an toi lam PM" tren Dashboard.
  final String? initialRoleFilter;

  @override
  State<MyProjectsScreen> createState() => _MyProjectsScreenState();
}

/// Gia tri quy uoc cho "chi du an toi lam PM" trong bo loc Vai tro — khop dung sentinel "pm"
/// cua MyProjectsViewModel.RolePm ben web (vai tro tu do khong bao gio trung gia tri nay).
const _rolePmSentinel = '__PM__';

class _MyProjectsScreenState extends State<MyProjectsScreen> {
  final _service = MyProjectsService();
  final _searchController = TextEditingController();

  late Future<MyProjectsData> _future;
  bool _showClosed = false;
  String _query = '';

  String? _phaseFilter;
  String? _stateFilter;
  late String? _roleFilter = widget.initialRoleFilter == 'pm'
      ? _rolePmSentinel
      : widget.initialRoleFilter;

  @override
  void initState() {
    super.initState();
    _future = _service.fetch(scope: widget.scope);
  }

  void _reload({bool forceRefresh = false}) {
    setState(() {
      _future = _service.fetch(
        showClosed: _showClosed,
        scope: widget.scope,
        forceRefresh: forceRefresh,
      );
    });
  }

  bool get _hasActiveFilter =>
      _query.isNotEmpty ||
      _phaseFilter != null ||
      _stateFilter != null ||
      _roleFilter != null;

  void _clearAllFilters() {
    setState(() {
      _query = '';
      _searchController.clear();
      _phaseFilter = null;
      _stateFilter = null;
      _roleFilter = null;
    });
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  List<MyProjectRow> _filtered(List<MyProjectRow> rows) {
    var result = rows;

    if (_query.isNotEmpty) {
      final needle = _query.toLowerCase();
      result = result
          .where((r) =>
              r.name.toLowerCase().contains(needle) ||
              (r.code ?? '').toLowerCase().contains(needle) ||
              (r.customer ?? '').toLowerCase().contains(needle) ||
              (r.pmName ?? '').toLowerCase().contains(needle))
          .toList();
    }

    if (_phaseFilter != null) {
      result = result.where((r) => r.phase == _phaseFilter).toList();
    }
    if (_stateFilter != null) {
      result = result.where((r) => r.state == _stateFilter).toList();
    }
    if (_roleFilter == _rolePmSentinel) {
      result = result.where((r) => r.isPm).toList();
    } else if (_roleFilter != null) {
      result = result.where((r) => r.role == _roleFilter).toList();
    }

    return result;
  }

  Future<void> _openFilterSheet(MyProjectsData data) async {
    final phases = data.projects
        .map((r) => r.phase)
        .where((p) => p.isNotEmpty)
        .toSet()
        .toList();
    final states = data.projects
        .map((r) => r.state)
        .where((s) => s.isNotEmpty)
        .toSet()
        .toList();
    final roles = data.projects
        .map((r) => r.role)
        .where((r) => r != null && r.isNotEmpty)
        .cast<String>()
        .toSet()
        .toList();
    final pmCount = data.projects.where((r) => r.isPm).length;

    final result = await showModalBottomSheet<_FilterResult>(
      context: context,
      isScrollControlled: true,
      elevation: 0,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (context) => _FilterSheet(
        phases: phases,
        states: states,
        roles: roles,
        pmCount: pmCount,
        initialPhase: _phaseFilter,
        initialState: _stateFilter,
        initialRole: _roleFilter,
      ),
    );

    if (result != null) {
      setState(() {
        _phaseFilter = result.phase;
        _stateFilter = result.state;
        _roleFilter = result.role;
      });
    }
  }

  Future<void> _openAddProject() async {
    final created = await pushAddProjectScreen(context, service: _service);
    if (created == true) _reload();
  }

  @override
  Widget build(BuildContext context) {
    // Nut "+ Thêm dự án" hien tren CA HAI bien the man hinh (Dự án của tôi / Dự án — Toàn Tổ),
    // mien tai khoan co quyen tao du an (canCreateProject — Quan ly/Quan tri/Quan ly To) — khop
    // hanh vi ben web (nut luon nam ngay tren trang "Dự án", khong an theo scope dang xem).
    final canCreateProject = context.watch<AuthProvider>().canCreateProject;

    return AppBottomNav(
      currentIndex: 1,
      appBar: AppAppBar(
        title: widget.scope == 'team' ? 'Dự án — Toàn Tổ' : 'Dự án của tôi',
        actions: canCreateProject
            ? [
                AppIconButton(
                  icon: PhosphorIconsRegular.plus,
                  tooltip: 'Thêm dự án',
                  onPressed: _openAddProject,
                ),
              ]
            : null,
      ),
      body: SafeArea(
        child: FutureBuilder<MyProjectsData>(
          future: _future,
          initialData: _service.getCached(showClosed: _showClosed, scope: widget.scope),
          builder: (context, snapshot) {
            final data = snapshot.data;

            if (data == null) {
              if (snapshot.hasError) {
                return Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const PhosphorIcon(PhosphorIconsRegular.warningCircle,
                            color: AppTheme.statusDanger, size: 32),
                        const SizedBox(height: 12),
                        const AppText('Không tải được danh sách dự án.',
                            variant: AppTextVariant.body),
                        const SizedBox(height: 12),
                        AppButton(
                          label: 'Thử lại',
                          onPressed: () => _reload(forceRefresh: true),
                        ),
                      ],
                    ),
                  ),
                );
              }
              return const Center(
                child: AppLoading(),
              );
            }

            final rows = _filtered(data.projects);

            return RefreshIndicator(
              color: AppColors.primary,
              backgroundColor: AppColors.surface,
              onRefresh: () async => _reload(forceRefresh: true),
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
                children: [
                  AppText(
                    widget.scope == 'team'
                        ? 'Cả Tổ có ${data.totalCount} dự án'
                        : 'Bạn có ${data.totalCount} dự án'
                            '${data.pmCount > 0 ? ' — làm PM ${data.pmCount} dự án' : ''}',
                    variant: AppTextVariant.body,
                    fontSize: 15,
                    weight: FontWeight.w700,
                  ),
                const SizedBox(height: 14),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: AppTextField(
                        label: 'Tìm kiếm',
                        controller: _searchController,
                        onChanged: (v) => setState(() => _query = v),
                        hint: 'Tìm theo tên, mã, khách hàng, PM...',
                        prefixIcon: PhosphorIconsRegular.magnifyingGlass,
                      ),
                    ),
                    const SizedBox(width: 8),
                    _FilterButton(
                      active: _phaseFilter != null ||
                          _stateFilter != null ||
                          _roleFilter != null,
                      onTap: () => _openFilterSheet(data),
                    ),
                  ],
                ),
                if (_hasActiveFilter) ...[
                  const SizedBox(height: 10),
                  Wrap(
                    spacing: 6,
                    runSpacing: 6,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      if (_phaseFilter != null)
                        _FilterTag(
                          label:
                              'Giai đoạn: ${projectPhaseLabel(_phaseFilter!)}',
                          onRemove: () => setState(() => _phaseFilter = null),
                        ),
                      if (_stateFilter != null)
                        _FilterTag(
                          label:
                              'Trạng thái: ${projectStateLabel(_stateFilter!)}',
                          onRemove: () => setState(() => _stateFilter = null),
                        ),
                      if (_roleFilter != null)
                        _FilterTag(
                          label:
                              'Vai trò: ${_roleFilter == _rolePmSentinel ? "Tôi làm PM" : _roleFilter}',
                          onRemove: () => setState(() => _roleFilter = null),
                        ),
                      InkWell(
                        onTap: _clearAllFilters,
                        child: ConstrainedBox(
                          constraints: const BoxConstraints(
                              minHeight: AppDimens.minTapTarget),
                          child: const Padding(
                            padding: EdgeInsets.symmetric(horizontal: 4),
                            child: Center(
                              child: AppText('Bỏ lọc',
                                  variant: AppTextVariant.caption,
                                  fontSize: 12,
                                  weight: FontWeight.w600,
                                  color: AppTheme.brandBlue),
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
                if (data.closedCount > 0) ...[
                  const SizedBox(height: 10),
                  AppCheckbox(
                    label: 'Hiện cả dự án đã đóng (${data.closedCount})',
                    value: _showClosed,
                    onChanged: (_) {
                      _showClosed = !_showClosed;
                      _reload();
                    },
                  ),
                ],
                const SizedBox(height: 12),
                if (rows.isEmpty)
                  const Padding(
                    padding: EdgeInsets.symmetric(vertical: 24),
                    child: AppText('Không có dự án nào khớp.',
                        variant: AppTextVariant.body,
                        color: AppColors.textSecondary),
                  )
                else
                  for (final row in rows) _ProjectCard(row: row),
              ],
            ),
          );
        },
      ),
    ),
  );
}
}

class _ReportBadge extends StatelessWidget {
  const _ReportBadge({required this.status});

  final String status;

  @override
  Widget build(BuildContext context) {
    late final String label;
    late final Color color;

    switch (status) {
      case 'DaNopDungHan':
        label = 'Đã nộp đúng hạn';
        color = AppTheme.statusSuccess;
        break;
      case 'DaNopTreHan':
        label = 'Đã nộp trễ hạn';
        color = AppTheme.statusWarning;
        break;
      case 'ConBanNhap':
        label = 'Còn bản nháp';
        color = AppColors.textSecondary;
        break;
      case 'ChuaNop':
        label = 'Chưa nộp';
        color = AppTheme.statusDanger;
        break;
      default:
        return const SizedBox.shrink();
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(6),
      ),
      child: AppText(label,
          variant: AppTextVariant.overline,
          fontSize: 10.5,
          weight: FontWeight.w700,
          color: color,
          letterSpacing: 0),
    );
  }
}

class _ProjectCard extends StatelessWidget {
  const _ProjectCard({required this.row});

  final MyProjectRow row;

  @override
  Widget build(BuildContext context) {
    final progress = row.checklistTotal == 0 ? 0.0 : row.checklistPercent / 100;

    return AppCard(
      onTap: () => Nav.toNamed(context, AppRoutes.projectDetail,
          arguments: {'projectId': row.id.toString()}),
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: AppText(row.name,
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              variant: AppTextVariant.body,
                              fontSize: 14.5,
                              weight: FontWeight.w700),
                        ),
                        if (!row.isOpen) ...[
                          const SizedBox(width: 6),
                          Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 6, vertical: 2),
                            decoration: BoxDecoration(
                              color: AppColors.surfaceVariant,
                              borderRadius: BorderRadius.circular(4),
                            ),
                            child: const AppText('Đã đóng',
                                variant: AppTextVariant.overline,
                                fontSize: 9.5,
                                color: AppColors.textSecondary,
                                letterSpacing: 0),
                          ),
                        ],
                      ],
                    ),
                    const SizedBox(height: 2),
                    AppText(
                      [
                        if (row.customer?.isNotEmpty == true) row.customer!,
                        'PM: ${row.pmName?.isNotEmpty == true ? row.pmName : "(chưa có PM)"}',
                      ].join(' · '),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      variant: AppTextVariant.caption,
                      fontSize: 11.5,
                      color: AppColors.textSecondary,
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Wrap(
            spacing: 6,
            runSpacing: 6,
            children: [
              if (row.isPm)
                const _Chip(label: 'PM', color: AppTheme.brandBlue)
              else if (row.role?.isNotEmpty == true)
                _Chip(label: row.role!, color: AppTheme.brandBlueDark),
              _Chip(
                  label: projectPhaseLabel(row.phase),
                  color: AppColors.textSecondary),
              _Chip(
                  label: projectStateLabel(row.state),
                  color: AppColors.textSecondary),
              _ReportBadge(status: row.reportStatus),
              if (!row.isActiveMember && !row.isPm && row.leftAt != null)
                AppText(
                    'Đã rời ${DateFormat('dd/MM/yyyy').format(row.leftAt!)}',
                    variant: AppTextVariant.overline,
                    fontSize: 10.5,
                    color: AppColors.textSecondary,
                    letterSpacing: 0),
            ],
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        AppText(
                          row.checklistTotal == 0
                              ? 'Chưa có checklist'
                              : '${row.checklistDone}/${row.checklistTotal} việc (${row.checklistPercent}%)',
                          variant: AppTextVariant.caption,
                          fontSize: 11.5,
                          color: AppColors.textSecondary,
                        ),
                      ],
                    ),
                    const SizedBox(height: 4),
                    ClipRRect(
                      borderRadius: BorderRadius.circular(6),
                      child: LinearProgressIndicator(
                        value: progress,
                        minHeight: 5,
                        backgroundColor:
                            AppTheme.brandBlue.withValues(alpha: 0.1),
                        valueColor: const AlwaysStoppedAnimation(
                            AppTheme.brandBlueDark),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 14),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  AppText('${row.myOpenCount} việc của tôi',
                      variant: AppTextVariant.caption,
                      fontSize: 11.5,
                      weight: FontWeight.w600),
                  if (row.myOverdueCount > 0) ...[
                    const SizedBox(height: 3),
                    AppText('${row.myOverdueCount} quá hạn',
                        variant: AppTextVariant.overline,
                        fontSize: 10.5,
                        weight: FontWeight.w700,
                        color: AppTheme.statusDanger,
                        letterSpacing: 0),
                  ],
                ],
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _FilterButton extends StatelessWidget {
  const _FilterButton({required this.active, required this.onTap});

  final bool active;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return AppIconButton(
      icon: PhosphorIconsRegular.slidersHorizontal,
      onPressed: onTap,
      size: 19,
      color: active ? AppColors.textOnPrimary : AppColors.textSecondary,
      background: active ? AppColors.primary : AppColors.surface,
    );
  }
}

class _Chip extends StatelessWidget {
  const _Chip({required this.label, required this.color});

  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        border: Border.all(color: color.withValues(alpha: 0.4)),
        borderRadius: BorderRadius.circular(6),
      ),
      child: AppText(label,
          variant: AppTextVariant.overline,
          fontSize: 10.5,
          weight: FontWeight.w600,
          color: color,
          letterSpacing: 0),
    );
  }
}

/// Mot dieu kien loc dang ap dung, hien nhu chip co nut bo — cung tinh than voi cach web hien
/// bo loc dang chon (nhung o day thao tac ngay tren chip thay vi phai mo lai form).
class _FilterTag extends StatelessWidget {
  const _FilterTag({required this.label, required this.onRemove});

  final String label;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.only(left: 10, right: 4, top: 4, bottom: 4),
      decoration: BoxDecoration(
        color: AppTheme.brandBlueSoft,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          AppText(label,
              variant: AppTextVariant.caption,
              fontSize: 11.5,
              weight: FontWeight.w600,
              color: AppTheme.brandBlue),
          // Vung cham 48dp mac dinh se lam phinh to chip — day la hanh dong PHU, tach biet voi
          // cac chip khac, giam xuong 32dp van du an toan tranh cham nham ma khong pha ket cau
          // dong (xem ghi chu tuong tu trong checklist_board_screen.dart._FilterTag).
          AppIconButton(
            icon: PhosphorIconsRegular.x,
            onPressed: onRemove,
            size: 13,
            color: AppTheme.brandBlue,
            minSize: 32,
          ),
        ],
      ),
    );
  }
}

class _FilterResult {
  const _FilterResult({this.phase, this.state, this.role});

  final String? phase;
  final String? state;
  final String? role;
}

/// Hop thoai chon Giai doan/Trang thai/Vai tro — tuong duong 3 o <select> tren form loc cua
/// Views/MyWork/Projects.cshtml, nhung gop lai mot cho thay vi 3 dropdown rieng tren thanh
/// cong cu (khong du cho ngang tren dien thoai).
class _FilterSheet extends StatefulWidget {
  const _FilterSheet({
    required this.phases,
    required this.states,
    required this.roles,
    required this.pmCount,
    required this.initialPhase,
    required this.initialState,
    required this.initialRole,
  });

  final List<String> phases;
  final List<String> states;
  final List<String> roles;
  final int pmCount;
  final String? initialPhase;
  final String? initialState;
  final String? initialRole;

  @override
  State<_FilterSheet> createState() => _FilterSheetState();
}

class _FilterSheetState extends State<_FilterSheet> {
  late String? _phase = widget.initialPhase;
  late String? _state = widget.initialState;
  late String? _role = widget.initialRole;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: EdgeInsets.fromLTRB(
            20, 20, 20, MediaQuery.of(context).viewInsets.bottom + 20),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const AppText('Bộ lọc dự án',
                variant: AppTextVariant.body,
                fontSize: 16,
                weight: FontWeight.w700),
            const SizedBox(height: 18),
            AppDropdown<String>(
              label: 'Giai đoạn',
              value: _phase,
              items: {for (final p in widget.phases) p: projectPhaseLabel(p)},
              onChanged: (v) => setState(() => _phase = v),
            ),
            const SizedBox(height: 14),
            AppDropdown<String>(
              label: 'Trạng thái',
              value: _state,
              items: {for (final s in widget.states) s: projectStateLabel(s)},
              onChanged: (v) => setState(() => _state = v),
            ),
            const SizedBox(height: 14),
            AppDropdown<String>(
              label: 'Vai trò',
              value: _role,
              items: {
                if (widget.pmCount > 0)
                  _rolePmSentinel: 'Tôi làm PM (${widget.pmCount})',
                for (final r in widget.roles) r: r,
              },
              onChanged: (v) => setState(() => _role = v),
            ),
            const SizedBox(height: 22),
            Row(
              children: [
                Expanded(
                  child: AppButton(
                    type: AppButtonType.outline,
                    label: 'Bỏ lọc',
                    onPressed: () => Navigator.of(context).pop(
                        const _FilterResult(
                            phase: null, state: null, role: null)),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: AppButton(
                    label: 'Áp dụng',
                    onPressed: () => Navigator.of(context).pop(_FilterResult(
                        phase: _phase, state: _state, role: _role)),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
