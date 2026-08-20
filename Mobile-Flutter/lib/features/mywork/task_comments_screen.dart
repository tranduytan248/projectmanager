import 'dart:io';
import 'dart:typed_data';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:open_filex/open_filex.dart';
import 'package:path_provider/path_provider.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_dimens.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/dialog_service.dart';
import '../../core/utils/toast_service.dart';
import '../../core/widgets/app_app_bar.dart';
import '../../core/widgets/app_button.dart';
import '../../core/widgets/app_icon_button.dart';
import '../../core/widgets/app_loading.dart';
import '../../core/widgets/app_rich_editor.dart';
import '../../core/widgets/app_scaffold.dart';
import '../../core/widgets/app_text.dart';
import 'comment_html.dart';
import 'mention_picker.dart';
import 'task_detail_models.dart';
import 'task_detail_service.dart';

/// Dung whitelist duoi file CommentAttachments.AllowedExtensions ben backend (khong co video —
/// xem ke hoach da duyet) de nguoi dung thay loc ngay tu luc chon, server van la noi quyet dinh
/// cuoi cung.
const List<String> kAllowedAttachmentExtensions = [
  'png',
  'jpg',
  'jpeg',
  'gif',
  'webp',
  'bmp',
  'pdf',
  'doc',
  'docx',
  'xls',
  'xlsx',
  'ppt',
  'pptx',
  'txt',
  'csv',
  'md',
  'zip',
  'rar',
  '7z',
];

const Set<String> _kImageExtensions = {
  'png',
  'jpg',
  'jpeg',
  'gif',
  'webp',
  'bmp'
};

bool _isImageName(String? name) {
  if (name == null) return false;
  final dot = name.lastIndexOf('.');
  if (dot < 0) return false;
  return _kImageExtensions.contains(name.substring(dot + 1).toLowerCase());
}

String _sizeLabel(int bytes) {
  if (bytes < 1024) return '$bytes B';
  if (bytes < 1024 * 1024) return '${(bytes / 1024).toStringAsFixed(0)} KB';
  return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
}

/// Mo man "Trao doi" day du (danh sach + soan thao rich text + dinh kem + @nhac ten) — push
/// full-man hinh (khac 2 bottom sheet Ghi gio/Todolist) vi day la luong dang chat, can nhieu
/// khong gian doc + ban phim. Tra ve CommentsSummary moi nhat khi bam nut quay lai (neu co doi
/// gi do); vuot/back cung he thong tra null — man Chi tiet cong viec chi mat co hoi lam tuoi
/// truoc, khong mat du lieu (moi thao tac gui/thu hoi da luu xong tren server truoc do).
Future<CommentsSummary?> pushTaskCommentsScreen(
  BuildContext context, {
  required TaskDetailService service,
  required int taskId,
  required CommentsSummary initial,
}) {
  return Navigator.of(context).push<CommentsSummary>(
    MaterialPageRoute(
      builder: (context) => TaskCommentsScreen(
          service: service, taskId: taskId, initial: initial),
    ),
  );
}

class TaskCommentsScreen extends StatefulWidget {
  const TaskCommentsScreen({
    super.key,
    required this.service,
    required this.taskId,
    required this.initial,
  });

  final TaskDetailService service;
  final int taskId;
  final CommentsSummary initial;

  @override
  State<TaskCommentsScreen> createState() => _TaskCommentsScreenState();
}

class _TaskCommentsScreenState extends State<TaskCommentsScreen> {
  late CommentsSummary _summary;
  bool _changed = false;

  final _editorController = AppRichEditorController();
  bool _sending = false;
  final Set<int> _recallingIds = {};
  final Set<int> _openingAttachmentIds = {};
  PlatformFile? _pendingAttachment;

  @override
  void initState() {
    super.initState();
    _summary = widget.initial;
  }

  @override
  void dispose() {
    _editorController.dispose();
    super.dispose();
  }

  void _closeScreen() {
    Navigator.of(context).pop(_changed ? _summary : null);
  }

  Future<void> _pickAttachment() async {
    final result = await FilePicker.platform.pickFiles(
      type: FileType.custom,
      allowedExtensions: kAllowedAttachmentExtensions,
    );
    if (result == null || result.files.isEmpty) return;
    setState(() => _pendingAttachment = result.files.single);
  }

  Future<void> _submit() async {
    final html = _editorController.toHtml();
    final hasText = !_editorController.isEmptyContent;
    final hasFile = _pendingAttachment != null;

    if (!hasText && !hasFile) {
      ToastService.show('Vui lòng nhập nội dung hoặc đính kèm tệp.',
          type: ToastType.error);
      return;
    }

    setState(() => _sending = true);
    final result = await widget.service.sendComment(
      taskId: widget.taskId,
      contentHtml: html,
      attachmentPath: _pendingAttachment?.path,
    );
    if (!mounted) return;
    setState(() => _sending = false);

    if (result.isSuccess) {
      setState(() {
        _summary = result.data!;
        _changed = true;
        _editorController.clear();
        _pendingAttachment = null;
      });
    } else {
      ToastService.show(result.error!, type: ToastType.error);
    }
  }

  Future<void> _confirmRecall(TaskComment c) async {
    final ok = await DialogService.showConfirm('Thu hồi nội dung này?',
        title: 'Thu hồi trao đổi');
    if (!ok) return;

    setState(() => _recallingIds.add(c.id));
    final result = await widget.service
        .recallComment(taskId: widget.taskId, commentId: c.id);
    if (!mounted) return;
    setState(() => _recallingIds.remove(c.id));

    if (result.isSuccess) {
      setState(() {
        _summary = result.data!;
        _changed = true;
      });
      ToastService.show('Đã thu hồi trao đổi.', type: ToastType.success);
    } else {
      ToastService.show(result.error!, type: ToastType.error);
    }
  }

  Future<void> _openAttachment(TaskComment c) async {
    if (_openingAttachmentIds.contains(c.id)) return;
    setState(() => _openingAttachmentIds.add(c.id));

    try {
      final bytes = await widget.service.downloadAttachment(c.id);
      if (!mounted) return;

      if (_isImageName(c.attachmentName)) {
        await showDialog<void>(
          context: context,
          builder: (context) => _ImageViewerDialog(bytes: bytes),
        );
        return;
      }

      final dir = await getTemporaryDirectory();
      final path = '${dir.path}/${c.attachmentName ?? 'tep-dinh-kem-${c.id}'}';
      await File(path).writeAsBytes(bytes, flush: true);
      await OpenFilex.open(path);
    } catch (_) {
      if (mounted) {
        ToastService.show('Không tải được tệp đính kèm. Hãy thử lại.',
            type: ToastType.error);
      }
    } finally {
      if (mounted) setState(() => _openingAttachmentIds.remove(c.id));
    }
  }

  @override
  Widget build(BuildContext context) {
    final list = _summary.comments;

    return AppScaffold(
      appBar: AppAppBar(
        title: 'Trao đổi',
        leading: AppIconButton(
          icon: PhosphorIconsRegular.arrowLeft,
          tooltip: 'Quay lại',
          onPressed: _closeScreen,
        ),
      ),
      body: SafeArea(
        child: Column(
          children: [
            Expanded(
              child: list.isEmpty
                  ? const Center(
                      child: Padding(
                        padding: EdgeInsets.all(AppDimens.space24),
                        child: AppText(
                          'Chưa có trao đổi nào. Có vướng mắc gì thì trao đổi ngay tại đây.',
                          variant: AppTextVariant.caption,
                          color: AppColors.textSecondary,
                          align: TextAlign.center,
                        ),
                      ),
                    )
                  : ListView.builder(
                      padding: const EdgeInsets.all(AppDimens.space16),
                      itemCount: list.length,
                      itemBuilder: (context, index) =>
                          _buildCommentRow(list[index]),
                    ),
            ),
            const Divider(height: 1, color: AppColors.border),
            _buildComposer(),
          ],
        ),
      ),
    );
  }

  Widget _buildCommentRow(TaskComment c) {
    final busyRecall = _recallingIds.contains(c.id);
    final busyOpen = _openingAttachmentIds.contains(c.id);

    return Container(
      margin: const EdgeInsets.only(bottom: AppDimens.space8),
      padding: const EdgeInsets.all(AppDimens.space12),
      decoration: BoxDecoration(
        color: AppColors.primarySoft,
        borderRadius: BorderRadius.circular(AppDimens.radiusMd),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: AppText(
                  c.authorName?.isNotEmpty == true ? c.authorName! : '—',
                  variant: AppTextVariant.caption,
                  fontSize: 12.5,
                  weight: FontWeight.w700,
                  color: AppColors.textPrimary,
                ),
              ),
              AppText(
                c.createdAt == null
                    ? ''
                    : DateFormat('HH:mm dd/MM/yyyy').format(c.createdAt!),
                variant: AppTextVariant.caption,
                fontSize: 11,
                color: AppColors.textSecondary,
              ),
              if (!c.isDeleted && c.canRecall) ...[
                const SizedBox(width: AppDimens.space4),
                busyRecall
                    ? const AppLoading(size: 18)
                    : AppIconButton(
                        icon: PhosphorIconsRegular.arrowCounterClockwise,
                        tooltip: 'Thu hồi',
                        onPressed: () => _confirmRecall(c),
                      ),
              ],
            ],
          ),
          const SizedBox(height: AppDimens.space4),
          if (c.isDeleted)
            const AppText(
              'Nội dung đã thu hồi.',
              variant: AppTextVariant.body,
              fontSize: 13,
              color: AppColors.textSecondary,
            )
          else ...[
            if ((c.content ?? '').trim().isNotEmpty)
              renderCommentHtml(c.content!),
            if (c.hasAttachment) ...[
              const SizedBox(height: AppDimens.space8),
              InkWell(
                onTap: busyOpen ? null : () => _openAttachment(c),
                borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                child: Container(
                  constraints:
                      const BoxConstraints(minHeight: AppDimens.minTapTarget),
                  padding: const EdgeInsets.symmetric(
                      horizontal: AppDimens.space8, vertical: AppDimens.space8),
                  decoration: BoxDecoration(
                    color: AppColors.surface,
                    borderRadius: BorderRadius.circular(AppDimens.radiusSm),
                    border: Border.all(color: AppColors.border),
                  ),
                  child: Row(
                    children: [
                      if (busyOpen)
                        const AppLoading(size: 18)
                      else
                        const PhosphorIcon(PhosphorIconsRegular.paperclip,
                            size: 16, color: AppColors.textSecondary),
                      const SizedBox(width: AppDimens.space8),
                      Expanded(
                        child: AppText(
                          c.attachmentName?.isNotEmpty == true
                              ? c.attachmentName!
                              : 'Tệp đính kèm',
                          variant: AppTextVariant.caption,
                          fontSize: 12.5,
                          color: AppColors.textPrimary,
                          overflow: TextOverflow.ellipsis,
                          maxLines: 1,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ],
        ],
      ),
    );
  }

  Widget _buildComposer() {
    return Padding(
      padding: EdgeInsets.fromLTRB(16, 12, 16, 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          MentionSuggestionList(
              controller: _editorController,
              participants: _summary.participants),
          AppRichEditor(
              controller: _editorController, minLines: 2, maxLines: 6),
          if (_pendingAttachment != null) ...[
            const SizedBox(height: AppDimens.space8),
            _PendingAttachmentChip(
              file: _pendingAttachment!,
              onRemove: () => setState(() => _pendingAttachment = null),
            ),
          ],
          const SizedBox(height: AppDimens.space8),
          Row(
            children: [
              AppIconButton(
                icon: PhosphorIconsRegular.paperclip,
                tooltip: 'Đính kèm',
                onPressed: _pickAttachment,
              ),
              const SizedBox(width: AppDimens.space8),
              Expanded(
                child: AppButton(
                  label: 'Gửi',
                  icon: PhosphorIconsRegular.paperPlaneTilt,
                  isLoading: _sending,
                  onPressed: _sending ? null : _submit,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _PendingAttachmentChip extends StatelessWidget {
  const _PendingAttachmentChip({required this.file, required this.onRemove});

  final PlatformFile file;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(
          horizontal: AppDimens.space12, vertical: AppDimens.space8),
      decoration: BoxDecoration(
        color: AppColors.primarySoft,
        borderRadius: BorderRadius.circular(AppDimens.radiusSm),
      ),
      child: Row(
        children: [
          const PhosphorIcon(PhosphorIconsRegular.paperclip,
              size: 16, color: AppColors.primary),
          const SizedBox(width: AppDimens.space8),
          Expanded(
            child: AppText(
              '${file.name} (${_sizeLabel(file.size)})',
              variant: AppTextVariant.caption,
              fontSize: 12.5,
              color: AppColors.textPrimary,
              overflow: TextOverflow.ellipsis,
              maxLines: 1,
            ),
          ),
          AppIconButton(
              icon: PhosphorIconsRegular.x,
              tooltip: 'Bỏ đính kèm',
              onPressed: onRemove),
        ],
      ),
    );
  }
}

class _ImageViewerDialog extends StatelessWidget {
  const _ImageViewerDialog({required this.bytes});

  final Uint8List bytes;

  @override
  Widget build(BuildContext context) {
    return Dialog(
      backgroundColor: AppColors.mediaViewerBackground,
      insetPadding: const EdgeInsets.all(12),
      child: Stack(
        children: [
          Positioned.fill(
            child: InteractiveViewer(
              child: Center(child: Image.memory(bytes)),
            ),
          ),
          Positioned(
            top: 4,
            right: 4,
            child: AppIconButton(
              icon: PhosphorIconsRegular.x,
              color: AppColors.textOnPrimary,
              tooltip: 'Đóng',
              onPressed: () => Navigator.of(context).pop(),
            ),
          ),
        ],
      ),
    );
  }
}
