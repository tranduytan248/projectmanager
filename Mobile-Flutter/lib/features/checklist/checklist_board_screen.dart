import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../config/app_theme.dart';
import '../../core/classes/route_manager.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_dropdown.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import '../../core/widgets/app_text_field.dart';
import '../app_routes.dart';
import '../dashboard/dashboard_models.dart';
import '../mywork/task_status_sheet.dart' show kTaskStateOptions;
import 'add_task_screen.dart';
import 'checklist_models.dart';
import 'checklist_service.dart';

/// Gia tri quy uoc cho "chua giao" trong bo loc Nguoi thuc hien — khop sentinel assigneeUserId=-1
/// ben web (ten nguoi that khong bao gio trung gia tri nay).
const _unassignedSentinel = '__UNASSIGNED__';

/// Khop dung ChecklistDueFilters ben backend (Models/Work/ChecklistModels.cs).
const _dueOverdue = 'quahan';
const _dueSoon = 'saptoihan';
const _dueNone = 'chuadathan';
const _dueSoonDays = 7;

String _kindLabel(String kind) => kind == 'HoTro' ? 'Hỗ trợ' : 'Checklist';

/// Man Checklist cho mobile: 2 che do xem — Dang luoi (danh sach phang, khong cay cha/con) va
/// Kanban (5 cot theo trang thai, cham the de doi nhanh trang thai — KHONG keo-tha vi khong hop
/// voi dien thoai). Ca hai che do dung chung tu khoa + bo loc trang thai/loai viec/nguoi thuc
/// hien/han o client — cung mau bo loc voi man "Du an cua toi" (nut loc mo bottom sheet + chip
/// dieu kien).
class ChecklistBoardScreen extends StatefulWidget {
  const ChecklistBoardScreen({
    super.key,
    required this.projectId,
    this.initialKindFilter,
    this.initialDueFilter,
  });

  final String projectId;

  /// Loc san theo loai viec (vi du "HoTro") khi mo tu the "CV Hỗ trợ" tren man Chi tiet du an —
  /// nguoi dung van doi/xoa duoc binh thuong sau khi mo, chi la gia tri KHOI TAO.
  final String? initialKindFilter;

  /// Loc san theo han (vi du "quahan") khi mo tu the "Quá hạn" tren man Chi tiet du an.
  final String? initialDueFilter;

  @override
  State<ChecklistBoardScreen> createState() => _ChecklistBoardScreenState();
}

class _ChecklistBoardScreenState extends State<ChecklistBoardScreen> {
  final _service = ChecklistService();
  final _searchController = TextEditingController();

  late Future<ChecklistData> _future;
  String _query = '';

  /// Che do xem hien tai — false = Dang luoi (mac dinh), true = Kanban. Chi la lua chon hien thi,
  /// khong anh huong du lieu (ca hai deu dung chung `_filtered`).
  bool _isKanban = false;

  String? _stateFilter;
  late String? _kindFilter = widget.initialKindFilter;
  String? _assigneeFilter;
  late String? _dueFilter = widget.initialDueFilter;

  @override
  void initState() {
    super.initState();
    final id = int.tryParse(widget.projectId) ?? 0;
    _future = _service.fetch(id);
  }

  void _reload() {
    setState(() {
      final id = int.tryParse(widget.projectId) ?? 0;
      _future = _service.fetch(id);
    });
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  bool get _hasActiveFilter =>
      _query.isNotEmpty ||
      _stateFilter != null ||
      _kindFilter != null ||
      _assigneeFilter != null ||
      _dueFilter != null;

  void _clearAllFilters() {
    setState(() {
      _query = '';
      _searchController.clear();
      _stateFilter = null;
      _kindFilter = null;
      _assigneeFilter = null;
      _dueFilter = null;
    });
  }

  bool _matchesDue(TaskItem t) {
    switch (_dueFilter) {
      case _dueOverdue:
        return t.isOverdue;
      case _dueSoon:
        if (t.isOverdue || t.dueDate == null) return false;
        return t.dueDate!.difference(DateTime.now()).inDays <= _dueSoonDays;
      case _dueNone:
        return t.dueDate == null;
      default:
        return true;
    }
  }

  List<TaskItem> _filtered(List<TaskItem> tasks) {
    var result = tasks;

    if (_query.isNotEmpty) {
      final needle = _query.toLowerCase();
      result = result
          .where((t) =>
              t.title.toLowerCase().contains(needle) || t.code.toLowerCase().contains(needle))
          .toList();
    }
    if (_stateFilter != null) {
      result = result.where((t) => t.state == _stateFilter).toList();
    }
    if (_kindFilter != null) {
      result = result.where((t) => t.kind == _kindFilter).toList();
    }
    if (_assigneeFilter == _unassignedSentinel) {
      result = result.where((t) => t.assigneeName == null || t.assigneeName!.isEmpty).toList();
    } else if (_assigneeFilter != null) {
      result = result.where((t) => t.assigneeName == _assigneeFilter).toList();
    }
    if (_dueFilter != null) {
      result = result.where(_matchesDue).toList();
    }

    return result;
  }

  Future<void> _openFilterSheet(List<TaskItem> tasks) async {
    final states = tasks.map((t) => t.state).where((s) => s.isNotEmpty).toSet().toList();
    final kinds = tasks.map((t) => t.kind).where((k) => k.isNotEmpty).toSet().toList();
    final assignees = tasks
        .map((t) => t.assigneeName)
        .where((a) => a != null && a.isNotEmpty)
        .cast<String>()
        .toSet()
        .toList();
    final hasUnassigned = tasks.any((t) => t.assigneeName == null || t.assigneeName!.isEmpty);

    final result = await showModalBottomSheet<_ChecklistFilterResult>(
      context: context,
      isScrollControlled: true,
      elevation: 0,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (context) => _ChecklistFilterSheet(
        states: states,
        kinds: kinds,
        assignees: assignees,
        hasUnassigned: hasUnassigned,
        initialState: _stateFilter,
        initialKind: _kindFilter,
        initialAssignee: _assigneeFilter,
        initialDue: _dueFilter,
      ),
    );

    if (result != null) {
      setState(() {
        _stateFilter = result.state;
        _kindFilter = result.kind;
        _assigneeFilter = result.assignee;
        _dueFilter = result.due;
      });
    }
  }

  Future<void> _openAddTaskSheet(ChecklistData data) async {
    final created = await pushAddTaskScreen(
      context,
      service: _service,
      projectId: data.projectId,
      assignees: data.assignees,
    );

    if (created == true) {
      _reload();
      ToastService.show('Đã thêm công việc mới.', type: ToastType.success);
    }
  }

  /// Cham the trong Kanban: mo dung man "Chi tiet cong viec" day du (gio cong/viec can lam/trao
  /// doi/cap nhat trang thai — nut Cap nhat trang thai tu an/hien theo canEdit ngay trong man do),
  /// giong het hanh vi cham the o Dang luoi. Quay lai tu man Chi tiet luon nap lai danh sach cua
  /// board de phan anh dung thay doi vua cap nhat (neu co).
  Future<void> _onTapKanbanCard(TaskItem task) async {
    await Nav.toNamed(context, AppRoutes.taskDetail, arguments: {'taskId': task.id.toString()});
    _reload();
  }

  /// Khoi tieu de dung chung cho ca 2 che do xem: ten du an + vong tron tien do, dong tom tat, o
  /// Tim kiem + nut Bo loc, chip dieu kien loc dang bat, va dong dem so viec khop.
  List<Widget> _buildHeader(ChecklistData data, List<TaskItem> tasks, bool hasFilterActive) {
    return [
      Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                AppText(data.projectName,
                    variant: AppTextVariant.title, fontSize: 17, weight: FontWeight.w800),
                if (data.pmName?.isNotEmpty == true) ...[
                  const SizedBox(height: 2),
                  AppText('PM: ${data.pmName}',
                      variant: AppTextVariant.caption,
                      fontSize: 12,
                      color: AppColors.textSecondary),
                ],
              ],
            ),
          ),
          SizedBox(
            width: 52,
            height: 52,
            child: Stack(
              alignment: Alignment.center,
              children: [
                CircularProgressIndicator(
                  value: data.donePercent / 100,
                  strokeWidth: 5,
                  backgroundColor: AppTheme.brandBlue.withValues(alpha: 0.12),
                  valueColor: const AlwaysStoppedAnimation(AppTheme.brandBlueDark),
                ),
                AppText('${data.donePercent}%',
                    variant: AppTextVariant.overline,
                    fontSize: 11,
                    weight: FontWeight.w800,
                    color: AppTheme.brandBlueDark,
                    letterSpacing: 0),
              ],
            ),
          ),
        ],
      ),
      const SizedBox(height: 4),
      AppText(
        '${data.doneCount}/${data.totalCount} xong'
        '${data.overdueCount > 0 ? ' · ${data.overdueCount} quá hạn' : ''}',
        variant: AppTextVariant.caption,
        fontSize: 12.5,
        color: AppColors.textSecondary,
      ),
      const SizedBox(height: 14),
      Row(
        children: [
          Expanded(
            child: AppTextField(
              label: 'Tìm kiếm',
              controller: _searchController,
              hint: 'Tìm theo mã, tên công việc...',
              prefixIcon: PhosphorIconsRegular.magnifyingGlass,
              onChanged: (v) => setState(() => _query = v),
            ),
          ),
          const SizedBox(width: 8),
          AppIconButton(
            icon: PhosphorIconsRegular.slidersHorizontal,
            tooltip: 'Bộ lọc',
            onPressed: () => _openFilterSheet(data.tasks),
            color: hasFilterActive ? AppColors.textOnPrimary : AppColors.textSecondary,
            background: hasFilterActive ? AppTheme.brandBlue : null,
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
            if (_stateFilter != null)
              _FilterTag(
                label: 'Trạng thái: ${taskStateLabel(_stateFilter!)}',
                onRemove: () => setState(() => _stateFilter = null),
              ),
            if (_kindFilter != null)
              _FilterTag(
                label: 'Loại việc: ${_kindLabel(_kindFilter!)}',
                onRemove: () => setState(() => _kindFilter = null),
              ),
            if (_assigneeFilter != null)
              _FilterTag(
                label:
                    'Người thực hiện: ${_assigneeFilter == _unassignedSentinel ? "(chưa giao)" : _assigneeFilter}',
                onRemove: () => setState(() => _assigneeFilter = null),
              ),
            if (_dueFilter != null)
              _FilterTag(
                label: 'Hạn: ${_dueFilterLabel(_dueFilter!)}',
                onRemove: () => setState(() => _dueFilter = null),
              ),
            InkWell(
              onTap: _clearAllFilters,
              child: ConstrainedBox(
                constraints: const BoxConstraints(minHeight: AppDimens.minTapTarget),
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
      const SizedBox(height: 14),
      AppText('${tasks.length}/${data.tasks.length} việc khớp',
          variant: AppTextVariant.overline,
          fontSize: 11.5,
          color: AppColors.textSecondary,
          letterSpacing: 0),
      const SizedBox(height: 8),
    ];
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      appBar: AppAppBar(
        title: 'Checklist',
        actions: [
          AppIconButton(
            icon: _isKanban ? PhosphorIconsRegular.listBullets : PhosphorIconsRegular.kanban,
            tooltip: _isKanban ? 'Xem dạng lưới' : 'Xem dạng Kanban',
            onPressed: () => setState(() => _isKanban = !_isKanban),
          ),
          FutureBuilder<ChecklistData>(
            future: _future,
            builder: (context, snapshot) {
              if (!snapshot.hasData || !snapshot.data!.canEdit) {
                return const SizedBox.shrink();
              }
              return AppIconButton(
                icon: PhosphorIconsRegular.plus,
                tooltip: 'Thêm mới',
                onPressed: () => _openAddTaskSheet(snapshot.data!),
              );
            },
          ),
        ],
      ),
      body: SafeArea(
        child: FutureBuilder<ChecklistData>(
          future: _future,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(
                child: AppLoading(),
              );
            }

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
                      const AppText('Không tải được checklist.', variant: AppTextVariant.body),
                      const SizedBox(height: 12),
                      AppButton(label: 'Thử lại', onPressed: _reload),
                    ],
                  ),
                ),
              );
            }

            final data = snapshot.data!;
            final tasks = _filtered(data.tasks);
            final hasFilterActive = _stateFilter != null ||
                _kindFilter != null ||
                _assigneeFilter != null ||
                _dueFilter != null;

            final header = _buildHeader(data, tasks, hasFilterActive);
            const emptyState = Padding(
              padding: EdgeInsets.symmetric(vertical: 24),
              child: AppText('Không có đầu việc nào khớp.',
                  variant: AppTextVariant.body, color: AppColors.textSecondary),
            );

            if (_isKanban) {
              return Column(
                children: [
                  Padding(
                    padding: const EdgeInsets.fromLTRB(20, 16, 20, 0),
                    child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: header),
                  ),
                  Expanded(
                    child: tasks.isEmpty
                        ? emptyState
                        : _KanbanBoard(tasks: tasks, onTapTask: _onTapKanbanCard),
                  ),
                ],
              );
            }

            return ListView(
              padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
              children: [
                ...header,
                if (tasks.isEmpty)
                  emptyState
                else
                  for (final task in tasks) _TaskRow(task: task),
              ],
            );
          },
        ),
      ),
    );
  }
}

String _dueFilterLabel(String due) {
  switch (due) {
    case _dueOverdue:
      return 'Quá hạn';
    case _dueSoon:
      return 'Sắp tới hạn (trong $_dueSoonDays ngày)';
    case _dueNone:
      return 'Chưa đặt hạn';
    default:
      return due;
  }
}

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
          // cac chip khac (xem SizedBox width:6/spacing cua Wrap cha), giam xuong 32dp van du an
          // toan tranh cham nham ma khong pha ket cau dong.
          AppIconButton(
            icon: PhosphorIconsRegular.x,
            size: 13,
            color: AppTheme.brandBlue,
            tooltip: 'Bỏ điều kiện lọc này',
            onPressed: onRemove,
            minSize: 32,
          ),
        ],
      ),
    );
  }
}

class _ChecklistFilterResult {
  const _ChecklistFilterResult({this.state, this.kind, this.assignee, this.due});

  final String? state;
  final String? kind;
  final String? assignee;
  final String? due;
}

/// Hop thoai chon Trang thai/Loai viec/Nguoi thuc hien/Han — tuong duong 4 o <select> tren
/// thanh loc cua Views/Checklist/Index.cshtml.
class _ChecklistFilterSheet extends StatefulWidget {
  const _ChecklistFilterSheet({
    required this.states,
    required this.kinds,
    required this.assignees,
    required this.hasUnassigned,
    required this.initialState,
    required this.initialKind,
    required this.initialAssignee,
    required this.initialDue,
  });

  final List<String> states;
  final List<String> kinds;
  final List<String> assignees;
  final bool hasUnassigned;
  final String? initialState;
  final String? initialKind;
  final String? initialAssignee;
  final String? initialDue;

  @override
  State<_ChecklistFilterSheet> createState() => _ChecklistFilterSheetState();
}

class _ChecklistFilterSheetState extends State<_ChecklistFilterSheet> {
  late String? _state = widget.initialState;
  late String? _kind = widget.initialKind;
  late String? _assignee = widget.initialAssignee;
  late String? _due = widget.initialDue;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: Padding(
        padding: EdgeInsets.fromLTRB(20, 20, 20, MediaQuery.of(context).viewInsets.bottom + 20),
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const AppText('Bộ lọc checklist',
                  variant: AppTextVariant.body, fontSize: 16, weight: FontWeight.w700),
              const SizedBox(height: 18),
              AppDropdown<String>(
                label: 'Trạng thái',
                value: _state,
                items: {for (final s in widget.states) s: taskStateLabel(s)},
                onChanged: (v) => setState(() => _state = v),
              ),
              const SizedBox(height: 14),
              AppDropdown<String>(
                label: 'Loại việc',
                value: _kind,
                items: {for (final k in widget.kinds) k: _kindLabel(k)},
                onChanged: (v) => setState(() => _kind = v),
              ),
              const SizedBox(height: 14),
              AppDropdown<String>(
                label: 'Người thực hiện',
                value: _assignee,
                items: {
                  if (widget.hasUnassigned) _unassignedSentinel: '(chưa giao)',
                  for (final a in widget.assignees) a: a,
                },
                onChanged: (v) => setState(() => _assignee = v),
              ),
              const SizedBox(height: 14),
              AppDropdown<String>(
                label: 'Hạn hoàn thành',
                value: _due,
                items: const {
                  _dueOverdue: 'Quá hạn',
                  _dueSoon: 'Sắp tới hạn (trong $_dueSoonDays ngày)',
                  _dueNone: 'Chưa đặt hạn',
                },
                onChanged: (v) => setState(() => _due = v),
              ),
              const SizedBox(height: 22),
              Row(
                children: [
                  Expanded(
                    child: AppButton(
                      label: 'Bỏ lọc',
                      type: AppButtonType.outline,
                      onPressed: () => Navigator.of(context).pop(const _ChecklistFilterResult()),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: AppButton(
                      label: 'Áp dụng',
                      onPressed: () => Navigator.of(context).pop(_ChecklistFilterResult(
                          state: _state, kind: _kind, assignee: _assignee, due: _due)),
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

Color _stateColor(String state) {
  switch (state) {
    case 'HoanThanh':
      return AppTheme.statusSuccess;
    case 'TamDung':
      return AppTheme.statusWarning;
    case 'Huy':
      return AppColors.textSecondary;
    default:
      return AppColors.textSecondary;
  }
}

class _TaskRow extends StatelessWidget {
  const _TaskRow({required this.task});

  final TaskItem task;

  @override
  Widget build(BuildContext context) {
    final color = _stateColor(task.state);
    final highPriority = task.priority == 'Cao';

    return InkWell(
      onTap: () => Nav.toNamed(context, AppRoutes.taskDetail,
          arguments: {'taskId': task.id.toString()}),
      borderRadius: BorderRadius.circular(14),
      child: Container(
        margin: const EdgeInsets.only(bottom: 10),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: AppColors.border),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: AppText(
                    task.code.isNotEmpty ? '${task.code} · ${task.title}' : task.title,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    variant: AppTextVariant.body,
                    fontSize: 13.5,
                    weight: FontWeight.w600,
                  ),
                ),
                const SizedBox(width: 8),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(
                    color: color.withValues(alpha: 0.16),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: AppText(taskStateLabel(task.state),
                      variant: AppTextVariant.overline,
                      fontSize: 10,
                      weight: FontWeight.w700,
                      color: color,
                      letterSpacing: 0),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                if (task.assigneeName?.isNotEmpty == true) ...[
                  const PhosphorIcon(PhosphorIconsRegular.userCircle,
                      size: 13, color: AppColors.textFaint),
                  const SizedBox(width: 4),
                  AppText(task.assigneeName!,
                      variant: AppTextVariant.caption, fontSize: 11.5, color: AppColors.textSecondary),
                  const SizedBox(width: 10),
                ],
                if (highPriority)
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                    decoration: BoxDecoration(
                      color: AppTheme.statusDanger.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(4),
                    ),
                    child: const AppText('Ưu tiên cao',
                        variant: AppTextVariant.overline,
                        fontSize: 9.5,
                        weight: FontWeight.w700,
                        color: AppTheme.statusDanger,
                        letterSpacing: 0),
                  ),
                const Spacer(),
                if (task.dueDate != null)
                  AppText(
                    DateFormat('dd/MM').format(task.dueDate!),
                    variant: AppTextVariant.caption,
                    fontSize: 11.5,
                    weight: FontWeight.w600,
                    color: task.isOverdue ? AppTheme.statusDanger : AppColors.textSecondary,
                  ),
              ],
            ),
            if (task.state != 'HoanThanh' && task.state != 'Huy') ...[
              const SizedBox(height: 8),
              ClipRRect(
                borderRadius: BorderRadius.circular(6),
                child: LinearProgressIndicator(
                  value: task.progress / 100,
                  minHeight: 5,
                  backgroundColor: AppTheme.brandBlue.withValues(alpha: 0.1),
                  valueColor: const AlwaysStoppedAnimation(AppTheme.brandBlueDark),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Chieu rong 1 cot Kanban — du hien du noi dung the rut gon, van thay duoc mep cot ke ben de
/// nguoi dung biet con cuon ngang duoc.
const _kanbanColumnWidth = 232.0;

/// Sap xep viec MOI den CU trong tung cot Kanban, theo Ngay tao (createdAt). Viec khong co
/// createdAt (JSON loi/thieu) coi nhu cu nhat, day xuong cuoi thay vi len dau lam sai thu tu.
int _byNewestFirst(TaskItem a, TaskItem b) {
  final ca = a.createdAt;
  final cb = b.createdAt;
  if (ca == null && cb == null) return 0;
  if (ca == null) return 1;
  if (cb == null) return -1;
  return cb.compareTo(ca);
}

/// Board Kanban 5 cot theo Trang thai (dung dung thu tu + nhan cua `kTaskStateOptions`, khop web).
/// Cuon NGANG toan bo cot; moi cot tu cuon DOC rieng ben trong — khong keo-tha, cham the de doi
/// nhanh trang thai (xu ly o [onTapTask], man cha quyet dinh mo sheet doi trang thai hay mo Chi
/// tiet chi xem tuy `TaskItem.canEdit`).
class _KanbanBoard extends StatefulWidget {
  const _KanbanBoard({required this.tasks, required this.onTapTask});

  final List<TaskItem> tasks;
  final ValueChanged<TaskItem> onTapTask;

  @override
  State<_KanbanBoard> createState() => _KanbanBoardState();
}

class _KanbanBoardState extends State<_KanbanBoard> {
  final _scrollController = ScrollController();

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  /// Chuot cuon (wheel) tren web/desktop mac dinh gui delta THEO CHIEU DOC, khong tu lam cuon
  /// NGANG mot ListView nam ngang — nguoi dung tren web se thay board "khong cuon duoc" du keo-tha
  /// bang chuot van hoat dong. Tu doi delta doc thanh cuon ngang de wheel dung duoc luon, khong
  /// chi keo-tha moi cuon duoc.
  void _handlePointerSignal(PointerSignalEvent event) {
    if (event is! PointerScrollEvent || !_scrollController.hasClients) return;

    final position = _scrollController.position;
    final target =
        (position.pixels + event.scrollDelta.dy).clamp(0.0, position.maxScrollExtent);
    _scrollController.jumpTo(target);
  }

  @override
  Widget build(BuildContext context) {
    final states = kTaskStateOptions.keys.toList();

    return Listener(
      onPointerSignal: _handlePointerSignal,
      child: ListView.separated(
        controller: _scrollController,
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.fromLTRB(20, 0, 20, 16),
        itemCount: states.length,
        separatorBuilder: (_, __) => const SizedBox(width: AppDimens.space12),
        itemBuilder: (context, index) {
          final state = states[index];
          final columnTasks = widget.tasks.where((t) => t.state == state).toList()
            ..sort(_byNewestFirst);
          return _KanbanColumn(state: state, tasks: columnTasks, onTapTask: widget.onTapTask);
        },
      ),
    );
  }
}

class _KanbanColumn extends StatelessWidget {
  const _KanbanColumn({required this.state, required this.tasks, required this.onTapTask});

  final String state;
  final List<TaskItem> tasks;
  final ValueChanged<TaskItem> onTapTask;

  @override
  Widget build(BuildContext context) {
    final color = _stateColor(state);

    return Container(
      width: _kanbanColumnWidth,
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(AppDimens.radiusLg),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Container(
            padding: const EdgeInsets.symmetric(
                horizontal: AppDimens.space12, vertical: AppDimens.space12),
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.1),
              borderRadius:
                  const BorderRadius.vertical(top: Radius.circular(AppDimens.radiusLg)),
            ),
            child: Row(
              children: [
                Container(
                  width: 8,
                  height: 8,
                  decoration: BoxDecoration(color: color, shape: BoxShape.circle),
                ),
                const SizedBox(width: AppDimens.space8),
                Expanded(
                  child: AppText(
                    kTaskStateOptions[state] ?? state,
                    variant: AppTextVariant.body,
                    fontSize: 13,
                    weight: FontWeight.w700,
                    color: color,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                  decoration: BoxDecoration(
                    color: color.withValues(alpha: 0.18),
                    borderRadius: BorderRadius.circular(AppDimens.radiusPill),
                  ),
                  child: AppText('${tasks.length}',
                      variant: AppTextVariant.overline,
                      fontSize: 11,
                      weight: FontWeight.w700,
                      color: color,
                      letterSpacing: 0),
                ),
              ],
            ),
          ),
          Expanded(
            child: tasks.isEmpty
                ? const Center(
                    child: Padding(
                      padding: EdgeInsets.all(AppDimens.space16),
                      child: AppText('Không có việc',
                          variant: AppTextVariant.caption,
                          fontSize: 12,
                          color: AppColors.textSecondary,
                          align: TextAlign.center),
                    ),
                  )
                : ListView.builder(
                    padding: const EdgeInsets.all(AppDimens.space8),
                    itemCount: tasks.length,
                    itemBuilder: (context, index) {
                      final task = tasks[index];
                      return _KanbanCard(task: task, onTap: () => onTapTask(task));
                    },
                  ),
          ),
        ],
      ),
    );
  }
}

/// The rut gon cho Kanban — bo bot huy hieu Trang thai (da the hien qua mau/tieu de cot) so voi
/// _TaskRow o Dang luoi de vua cot hep, giu lai Uu tien cao/Nguoi thuc hien/Han/Tien do.
class _KanbanCard extends StatelessWidget {
  const _KanbanCard({required this.task, required this.onTap});

  final TaskItem task;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final highPriority = task.priority == 'Cao';

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(AppDimens.radiusMd),
      child: Container(
        constraints: const BoxConstraints(minHeight: AppDimens.minTapTarget),
        margin: const EdgeInsets.only(bottom: AppDimens.space8),
        padding: const EdgeInsets.all(AppDimens.space8),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(AppDimens.radiusMd),
          border: Border.all(color: AppColors.border),
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            AppText(
              task.code.isNotEmpty ? '${task.code} · ${task.title}' : task.title,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              variant: AppTextVariant.body,
              fontSize: 12.5,
              weight: FontWeight.w600,
            ),
            if (task.assigneeName?.isNotEmpty == true) ...[
              const SizedBox(height: AppDimens.space4),
              Row(
                children: [
                  const PhosphorIcon(PhosphorIconsRegular.userCircle,
                      size: 12, color: AppColors.textFaint),
                  const SizedBox(width: 4),
                  Expanded(
                    child: AppText(task.assigneeName!,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        variant: AppTextVariant.caption,
                        fontSize: 11,
                        color: AppColors.textSecondary),
                  ),
                ],
              ),
            ],
            if (highPriority || task.dueDate != null) ...[
              const SizedBox(height: AppDimens.space4),
              Row(
                children: [
                  if (highPriority)
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                      decoration: BoxDecoration(
                        color: AppTheme.statusDanger.withValues(alpha: 0.1),
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: const AppText('Ưu tiên cao',
                          variant: AppTextVariant.overline,
                          fontSize: 9,
                          weight: FontWeight.w700,
                          color: AppTheme.statusDanger,
                          letterSpacing: 0),
                    ),
                  const Spacer(),
                  if (task.dueDate != null)
                    AppText(
                      DateFormat('dd/MM').format(task.dueDate!),
                      variant: AppTextVariant.caption,
                      fontSize: 11,
                      weight: FontWeight.w600,
                      color: task.isOverdue ? AppTheme.statusDanger : AppColors.textSecondary,
                    ),
                ],
              ),
            ],
            if (task.state != 'HoanThanh' && task.state != 'Huy') ...[
              const SizedBox(height: AppDimens.space4),
              ClipRRect(
                borderRadius: BorderRadius.circular(6),
                child: LinearProgressIndicator(
                  value: task.progress / 100,
                  minHeight: 4,
                  backgroundColor: AppTheme.brandBlue.withValues(alpha: 0.1),
                  valueColor: const AlwaysStoppedAnimation(AppTheme.brandBlueDark),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

