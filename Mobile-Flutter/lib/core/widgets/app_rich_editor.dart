import 'package:flutter/material.dart';
import 'package:phosphor_icons/phosphor_icons.dart';

import '../theme/app_colors.dart';
import '../theme/app_dimens.dart';
import '../theme/app_text_styles.dart';
import 'app_button.dart';
import 'app_icon_button.dart';
import 'app_text.dart';
import 'app_text_field.dart';

/// Cu phap dinh dang nhe tu dinh nghia (KHONG phai markdown chuan, chi vua du cho khoi Trao doi):
/// **dam**, __gach_chan__, //nghieng//, [chu](https://...) — cac dau phan cach deu la chuoi
/// KHONG chong lan nen khong co nhap nhang the long nhau nhu markdown that. Dong bat dau "- "
/// hoac "so. " la mot dong trong danh sach.
final RegExp kRichInlinePattern = RegExp(
  r'(\*\*.+?\*\*)|(__.+?__)|(//.+?//)|(\[.+?\]\(https?://[^\s)]+\))',
);

final RegExp _bulletLine = RegExp(r'^-\s+.+$');
final RegExp _numberedLine = RegExp(r'^\d+\.\s+.+$');
final RegExp _linkMarkup = RegExp(r'^\[(.+?)\]\((https?://[^\s)]+)\)$');

String _escapeHtml(String s) =>
    s.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');

/// Ap dung dinh dang INLINE (dam/nghieng/gach chan/link) cho MOT dong — dung chung cho ca
/// AppRichEditorController.toHtml() (buffer.write de EA test) va cac noi can render cu phap
/// nay sang HTML.
String applyInlineHtmlFormat(String line) {
  return line.replaceAllMapped(kRichInlinePattern, (m) {
    if (m.group(1) != null) {
      final full = m.group(1)!;
      return '<b>${full.substring(2, full.length - 2)}</b>';
    }
    if (m.group(2) != null) {
      final full = m.group(2)!;
      return '<u>${full.substring(2, full.length - 2)}</u>';
    }
    if (m.group(3) != null) {
      final full = m.group(3)!;
      return '<i>${full.substring(2, full.length - 2)}</i>';
    }
    final full = m.group(4)!;
    final link = _linkMarkup.firstMatch(full)!;
    return '<a href="${link.group(2)}">${link.group(1)}</a>';
  });
}

/// Chuyen NGUYEN buffer da go (cu phap nhe o tren) sang dung whitelist the ma HtmlSanitizer ben
/// backend chap nhan (p,div,br,b,strong,i,em,u,s,strike,ul,ol,li,blockquote,h3,h4,a). Escape
/// &/</> THANH THUC THE TRUOC KHI ghep the — day la diem mau chot de khong tai dien bug "mat noi
/// dung co </>" tung gap: buffer luon la van ban do nguoi dung go (co the chua < > & tuy y), phai
/// coi la VAN BAN THUAN can escape, roi moi ghep the dinh dang len tren ban da escape do.
String richTextToHtml(String raw) {
  final escaped = _escapeHtml(raw);
  final blocks = escaped.split(RegExp(r'\n{2,}'));
  final buffer = StringBuffer();

  for (final rawBlock in blocks) {
    final block = rawBlock.trim();
    if (block.isEmpty) continue;

    final lines = block.split('\n').where((l) => l.trim().isNotEmpty).toList();
    if (lines.isEmpty) continue;

    if (lines.every((l) => _bulletLine.hasMatch(l))) {
      buffer.write('<ul>');
      for (final l in lines) {
        final content = l.replaceFirst(RegExp(r'^-\s+'), '');
        buffer.write('<li>${applyInlineHtmlFormat(content)}</li>');
      }
      buffer.write('</ul>');
      continue;
    }

    if (lines.every((l) => _numberedLine.hasMatch(l))) {
      buffer.write('<ol>');
      for (final l in lines) {
        final content = l.replaceFirst(RegExp(r'^\d+\.\s+'), '');
        buffer.write('<li>${applyInlineHtmlFormat(content)}</li>');
      }
      buffer.write('</ol>');
      continue;
    }

    buffer.write('<p>');
    buffer.write(lines.map(applyInlineHtmlFormat).join('<br />'));
    buffer.write('</p>');
  }

  return buffer.toString();
}

/// TextEditingController rieng cho AppRichEditor — tu to dam/nghieng/gach chan/link NGAY khi go
/// (giu nguyen cac ky hieu **/__/// trong buffer, chi doi kieu chu hien thi — khong an ky hieu di
/// de tranh phai tinh lai vi tri con tro khi go), va chuyen sang HTML dung whitelist qua toHtml().
class AppRichEditorController extends TextEditingController {
  AppRichEditorController({super.text});

  bool get isEmptyContent => text.trim().isEmpty;

  String toHtml() => richTextToHtml(text);

  /// Boc/go phan bôi den bang mot cap ky hieu (vi du "**"/"**" cho dam). Khong co gi duoc chon
  /// thi chen cap ky hieu voi con tro o giua de go tiep vao trong.
  void wrapSelection(String left, String right) {
    final sel = selection;
    final start = sel.isValid ? sel.start : text.length;
    final end = sel.isValid ? sel.end : text.length;
    final selected = text.substring(start, end);
    final newText = text.replaceRange(start, end, '$left$selected$right');

    if (selected.isEmpty) {
      value = TextEditingValue(
          text: newText,
          selection: TextSelection.collapsed(offset: start + left.length));
    } else {
      value = TextEditingValue(
        text: newText,
        selection: TextSelection(
          baseOffset: start + left.length,
          extentOffset: start + left.length + selected.length,
        ),
      );
    }
  }

  /// Bat/tat tien to o DAU DONG hien tai (con tro dang dung) — dung cho nut danh sach.
  void toggleLinePrefix(String prefix) {
    final sel = selection;
    final caret = sel.isValid ? sel.start : text.length;

    var lineStart = caret;
    while (lineStart > 0 && text[lineStart - 1] != '\n') {
      lineStart--;
    }
    var lineEnd = caret;
    while (lineEnd < text.length && text[lineEnd] != '\n') {
      lineEnd++;
    }

    final line = text.substring(lineStart, lineEnd);
    String newLine;
    int delta;
    if (line.startsWith(prefix)) {
      newLine = line.substring(prefix.length);
      delta = -prefix.length;
    } else {
      newLine = '$prefix$line';
      delta = prefix.length;
    }

    final newText = text.replaceRange(lineStart, lineEnd, newLine);
    final newCaret =
        (caret + delta).clamp(lineStart, lineStart + newLine.length);
    value = TextEditingValue(
        text: newText, selection: TextSelection.collapsed(offset: newCaret));
  }

  /// Chen cu phap lien ket "[nhan](url)" tai vi tri dang chon/con tro.
  void insertLink(String label, String url) {
    final sel = selection;
    final start = sel.isValid ? sel.start : text.length;
    final end = sel.isValid ? sel.end : text.length;
    final markup = '[$label]($url)';
    final newText = text.replaceRange(start, end, markup);
    value = TextEditingValue(
        text: newText,
        selection: TextSelection.collapsed(offset: start + markup.length));
  }

  @override
  TextSpan buildTextSpan(
      {required BuildContext context,
      TextStyle? style,
      bool withComposing = false}) {
    final base = style ?? const TextStyle();
    final source = text;
    if (source.isEmpty) return TextSpan(style: base);

    final spans = <TextSpan>[];
    var cursor = 0;

    for (final m in kRichInlinePattern.allMatches(source)) {
      if (m.start > cursor) {
        spans.add(
            TextSpan(text: source.substring(cursor, m.start), style: base));
      }

      var matchStyle = base;
      if (m.group(1) != null) {
        matchStyle = base.copyWith(fontWeight: FontWeight.w800);
      } else if (m.group(2) != null) {
        matchStyle = base.copyWith(decoration: TextDecoration.underline);
      } else if (m.group(3) != null) {
        matchStyle = base.copyWith(fontStyle: FontStyle.italic);
      } else if (m.group(4) != null) {
        matchStyle = base.copyWith(
            color: AppColors.primary, decoration: TextDecoration.underline);
      }
      spans.add(TextSpan(text: m.group(0), style: matchStyle));
      cursor = m.end;
    }

    if (cursor < source.length) {
      spans.add(TextSpan(text: source.substring(cursor), style: base));
    }

    return TextSpan(style: base, children: spans);
  }
}

/// Trinh soan thao rich text toi gian cua du an — dung `.claude/rules/FLUTTER_RULES.md`. Man hinh
/// KHONG duoc dung TextField/TextFormField truc tiep cho o Trao doi, luon qua AppRichEditor. Thanh
/// cong cu 4 nut (Dam/Nghieng/Gach chan/Danh sach) + 1 nut Lien ket mo hop thoai nho nhap URL —
/// dung y nguyen tac voi AppRichText ben web (Scripts/app.js) nhung thao tac truc tiep tren chuoi
/// thay vi contenteditable.
class AppRichEditor extends StatelessWidget {
  const AppRichEditor({
    super.key,
    required this.controller,
    this.label = 'Trao đổi',
    this.hint = 'Nhập nội dung trao đổi...',
    this.minLines = 4,
    this.maxLines = 10,
    this.onChanged,
  });

  final AppRichEditorController controller;

  /// Nhan hien phia tren o soan thao — mac dinh "Trao đổi" (dung ban dau, giu nguyen de khong
  /// doi hanh vi cac noi da dung), doi lai khi tai su dung cho ngu canh khac (vi du "Mô tả" o
  /// form Them cong viec moi).
  final String label;
  final String hint;
  final int minLines;
  final int maxLines;
  final ValueChanged<String>? onChanged;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _RichToolbar(controller: controller),
        const SizedBox(height: AppDimens.space4),
        AppTextField(
          label: label,
          hint: hint,
          controller: controller,
          maxLines: maxLines,
          minLines: minLines,
          onChanged: onChanged,
        ),
      ],
    );
  }
}

class _RichToolbar extends StatelessWidget {
  const _RichToolbar({required this.controller});

  final AppRichEditorController controller;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        AppIconButton(
          icon: PhosphorIconsRegular.textB,
          tooltip: 'Đậm',
          onPressed: () => controller.wrapSelection('**', '**'),
        ),
        AppIconButton(
          icon: PhosphorIconsRegular.textItalic,
          tooltip: 'Nghiêng',
          onPressed: () => controller.wrapSelection('//', '//'),
        ),
        AppIconButton(
          icon: PhosphorIconsRegular.textUnderline,
          tooltip: 'Gạch chân',
          onPressed: () => controller.wrapSelection('__', '__'),
        ),
        AppIconButton(
          icon: PhosphorIconsRegular.listBullets,
          tooltip: 'Danh sách',
          onPressed: () => controller.toggleLinePrefix('- '),
        ),
        AppIconButton(
          icon: PhosphorIconsRegular.linkSimple,
          tooltip: 'Liên kết',
          onPressed: () => _promptLink(context),
        ),
      ],
    );
  }

  Future<void> _promptLink(BuildContext context) async {
    final sel = controller.selection;
    final selectedText = sel.isValid && !sel.isCollapsed
        ? controller.text.substring(sel.start, sel.end)
        : '';

    final result = await showDialog<_LinkInput>(
      context: context,
      builder: (_) => _LinkDialog(initialLabel: selectedText),
    );
    if (result == null) return;

    controller.insertLink(result.label, result.url);
  }
}

class _LinkInput {
  const _LinkInput(this.label, this.url);

  final String label;
  final String url;
}

/// Hop thoai nho nhap nhan + URL cho nut Lien ket — dung `Dialog` (widget bo cuc thuan) thay vi
/// goi thang `AlertDialog` (DialogService khong ho tro nhap lieu tuy y, chi co san 5 dang thong
/// bao/xac nhan mot dong chu).
class _LinkDialog extends StatefulWidget {
  const _LinkDialog({required this.initialLabel});

  final String initialLabel;

  @override
  State<_LinkDialog> createState() => _LinkDialogState();
}

class _LinkDialogState extends State<_LinkDialog> {
  late final _labelController =
      TextEditingController(text: widget.initialLabel);
  final _urlController = TextEditingController();
  String? _error;

  @override
  void dispose() {
    _labelController.dispose();
    _urlController.dispose();
    super.dispose();
  }

  void _submit() {
    final label = _labelController.text.trim();
    final url = _urlController.text.trim();

    if (label.isEmpty) {
      setState(() => _error = 'Vui lòng nhập nhãn hiển thị.');
      return;
    }
    if (!url.startsWith('http://') && !url.startsWith('https://')) {
      setState(
          () => _error = 'Đường dẫn phải bắt đầu bằng http:// hoặc https://.');
      return;
    }
    if (url.contains('"')) {
      // richTextToHtml chi escape &/</> trong VAN BAN thuong, khong escape dau nhay kep trong
      // URL khi ghep the <a href="...">, vi day la markup do chinh trinh soan thao sinh ra chu
      // khong phai van ban nguoi dung go tuy y — chan tu day de khong bao gio tao ra href vo y
      // bi cat cut boi HtmlSanitizer ben server (SafeHref chi doc toi dau nhay kep dau tien).
      setState(() => _error = 'Đường dẫn không được chứa dấu ngoặc kép (").');
      return;
    }

    Navigator.of(context).pop(_LinkInput(label, url));
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppDimens.radiusLg)),
      child: Padding(
        padding: const EdgeInsets.all(AppDimens.space16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const AppText('Chèn liên kết',
                variant: AppTextVariant.heading, weight: FontWeight.w700),
            const SizedBox(height: AppDimens.space12),
            AppTextField(
                label: 'Nhãn hiển thị',
                controller: _labelController,
                required: true),
            const SizedBox(height: AppDimens.space12),
            AppTextField(
              label: 'Đường dẫn (URL)',
              controller: _urlController,
              hint: 'https://...',
              keyboardType: TextInputType.url,
              required: true,
            ),
            if (_error != null) ...[
              const SizedBox(height: AppDimens.space8),
              AppText(_error!,
                  variant: AppTextVariant.caption,
                  fontSize: 12,
                  color: AppColors.danger),
            ],
            const SizedBox(height: AppDimens.space16),
            Row(
              children: [
                Expanded(
                  child: AppButton(
                    label: 'Huỷ',
                    type: AppButtonType.outline,
                    onPressed: () => Navigator.of(context).pop(),
                  ),
                ),
                const SizedBox(width: AppDimens.space8),
                Expanded(child: AppButton(label: 'Chèn', onPressed: _submit)),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
