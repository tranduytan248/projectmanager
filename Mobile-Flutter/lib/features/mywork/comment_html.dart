import 'package:flutter/material.dart';

import '../../core/theme/app_colors.dart';

/// Bo doc HTML rieng cho khoi "Trao doi" — CHI hieu dung whitelist the ma HtmlSanitizer ben
/// backend chap nhan (p,div,br,b,strong,i,em,u,s,strike,ul,ol,li,blockquote,h3,h4,a), KHONG phai
/// mot trinh doc HTML tong quat. Dau vao luon da qua HtmlSanitizer.Clean o server (content tra ve
/// tu ChecklistApi da duoc loc), nen an toan de duyet true — khong co nguy co the la/script vi
/// server da xoa het truoc khi luu.
///
/// Dung mot tokenizer/duyet cay don gian (khong tep phu thuoc HTML-parser nao) vi bo the can
/// hieu rat nho va co dinh.
sealed class _Node {}

class _ElementNode extends _Node {
  _ElementNode(this.tag, this.attrs);

  final String tag;
  final Map<String, String> attrs;
  final List<_Node> children = [];
}

class _TextNode extends _Node {
  _TextNode(this.text);

  final String text;
}

final RegExp _tagPattern = RegExp(r'<(/?)([a-zA-Z0-9]+)([^>]*)>');
final RegExp _attrPattern =
    RegExp('''([a-zA-Z-]+)\\s*=\\s*("([^"]*)"|'([^']*)')''');

const _blockTags = {'p', 'div', 'blockquote', 'h3', 'h4', 'ul', 'ol'};

List<_Node> _parseNodes(String html) {
  final root = _ElementNode('root', const {});
  final stack = <_ElementNode>[root];
  var pos = 0;

  for (final m in _tagPattern.allMatches(html)) {
    if (m.start > pos) {
      final text = html.substring(pos, m.start);
      if (text.isNotEmpty) stack.last.children.add(_TextNode(_unescape(text)));
    }
    pos = m.end;

    final closing = m.group(1) == '/';
    final tag = m.group(2)!.toLowerCase();

    if (tag == 'br') {
      stack.last.children.add(_ElementNode('br', const {}));
      continue;
    }

    if (closing) {
      for (var i = stack.length - 1; i > 0; i--) {
        if (stack[i].tag == tag) {
          while (stack.length > i) {
            stack.removeLast();
          }
          break;
        }
      }
      continue;
    }

    final attrs = _parseAttrs(m.group(3) ?? '');
    final node = _ElementNode(tag, attrs);
    stack.last.children.add(node);
    stack.add(node);
  }

  if (pos < html.length) {
    final text = html.substring(pos);
    if (text.isNotEmpty) stack.last.children.add(_TextNode(_unescape(text)));
  }

  return root.children;
}

Map<String, String> _parseAttrs(String raw) {
  final result = <String, String>{};
  for (final m in _attrPattern.allMatches(raw)) {
    final name = m.group(1)!.toLowerCase();
    result[name] = m.group(3) ?? m.group(4) ?? '';
  }
  return result;
}

String _unescape(String s) => s
    .replaceAll('&nbsp;', ' ')
    .replaceAll('&lt;', '<')
    .replaceAll('&gt;', '>')
    .replaceAll('&quot;', '"')
    .replaceAll('&#39;', "'")
    .replaceAll('&amp;', '&');

/// Dung cho man hinh hien mot binh luan da luu — dung lai NGUYEN VEN whitelist the cua backend,
/// tra ve mot Column cac khoi (doan van/danh sach/trich dan...). [baseStyle] la kieu chu goc cho
/// phan van ban thuan, cac the dinh dang (dam/nghieng...) chi doi tren nen kieu nay.
Widget renderCommentHtml(String html, {TextStyle? baseStyle}) {
  final base = baseStyle ??
      const TextStyle(
          fontSize: 13.5, height: 1.45, color: AppColors.textPrimary);
  final nodes = _parseNodes(html);
  final blocks = _groupBlocks(nodes);

  return Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    mainAxisSize: MainAxisSize.min,
    children: [for (final block in blocks) _renderBlock(block, base)],
  );
}

/// Gom cac node LIEN TIEP khong phai the khoi (chu thuong/the inline nam ngoai p/div/ul/ol...)
/// thanh mot khoi doan van an, giu nguyen thu tu voi cac the khoi thuc su.
List<_Node> _groupBlocks(List<_Node> nodes) {
  final result = <_Node>[];
  var buffer = <_Node>[];

  void flush() {
    if (buffer.isNotEmpty) {
      final wrapper = _ElementNode('p', const {})..children.addAll(buffer);
      result.add(wrapper);
      buffer = [];
    }
  }

  for (final node in nodes) {
    if (node is _ElementNode && _blockTags.contains(node.tag)) {
      flush();
      result.add(node);
    } else {
      buffer.add(node);
    }
  }
  flush();

  return result;
}

Widget _renderBlock(_Node node, TextStyle base) {
  if (node is! _ElementNode) return const SizedBox.shrink();

  switch (node.tag) {
    case 'ul':
      return Padding(
        padding: const EdgeInsets.only(bottom: 4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            for (final item in node.children
                .whereType<_ElementNode>()
                .where((e) => e.tag == 'li'))
              _buildListItem('•', item, base),
          ],
        ),
      );
    case 'ol':
      var index = 1;
      return Padding(
        padding: const EdgeInsets.only(bottom: 4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            for (final item in node.children
                .whereType<_ElementNode>()
                .where((e) => e.tag == 'li'))
              _buildListItem('${index++}.', item, base),
          ],
        ),
      );
    case 'blockquote':
      return Container(
        margin: const EdgeInsets.only(bottom: 4),
        padding: const EdgeInsets.only(left: 10),
        decoration: const BoxDecoration(
          border: Border(left: BorderSide(color: AppColors.border, width: 3)),
        ),
        child: Text.rich(
          TextSpan(
            children: _buildInlineSpans(
                node.children,
                base.copyWith(
                    fontStyle: FontStyle.italic,
                    color: AppColors.textSecondary)),
          ),
        ),
      );
    case 'h3':
      return Padding(
        padding: const EdgeInsets.only(bottom: 4),
        child: Text.rich(TextSpan(
            children: _buildInlineSpans(node.children,
                base.copyWith(fontSize: 16, fontWeight: FontWeight.w800)))),
      );
    case 'h4':
      return Padding(
        padding: const EdgeInsets.only(bottom: 4),
        child: Text.rich(TextSpan(
            children: _buildInlineSpans(node.children,
                base.copyWith(fontSize: 14.5, fontWeight: FontWeight.w800)))),
      );
    case 'p':
    case 'div':
    default:
      final spans = _buildInlineSpans(node.children, base);
      if (spans.isEmpty) return const SizedBox.shrink();
      return Padding(
        padding: const EdgeInsets.only(bottom: 4),
        child: Text.rich(TextSpan(children: spans)),
      );
  }
}

Widget _buildListItem(String marker, _ElementNode item, TextStyle base) {
  return Padding(
    padding: const EdgeInsets.only(bottom: 2),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        SizedBox(width: 18, child: Text(marker, style: base)),
        Flexible(
            child: Text.rich(
                TextSpan(children: _buildInlineSpans(item.children, base)))),
      ],
    ),
  );
}

List<InlineSpan> _buildInlineSpans(List<_Node> nodes, TextStyle base) {
  final spans = <InlineSpan>[];

  for (final node in nodes) {
    if (node is _TextNode) {
      if (node.text.isEmpty) continue;
      spans.add(TextSpan(text: node.text, style: base));
      continue;
    }
    if (node is! _ElementNode) continue;

    switch (node.tag) {
      case 'br':
        spans.add(TextSpan(text: '\n', style: base));
        break;
      case 'b':
      case 'strong':
        spans.add(TextSpan(
            children: _buildInlineSpans(
                node.children, base.copyWith(fontWeight: FontWeight.w800))));
        break;
      case 'i':
      case 'em':
        spans.add(TextSpan(
            children: _buildInlineSpans(
                node.children, base.copyWith(fontStyle: FontStyle.italic))));
        break;
      case 'u':
        spans.add(TextSpan(
            children: _buildInlineSpans(node.children,
                base.copyWith(decoration: TextDecoration.underline))));
        break;
      case 's':
      case 'strike':
        spans.add(TextSpan(
            children: _buildInlineSpans(node.children,
                base.copyWith(decoration: TextDecoration.lineThrough))));
        break;
      case 'a':
        // Chi to mau/gach chan de bao "day la lien ket" — KHONG mo trinh duyet o ban dau (chua
        // them goi url_launcher, xem ke hoach da duyet).
        spans.add(TextSpan(
            children: _buildInlineSpans(
                node.children,
                base.copyWith(
                    color: AppColors.primary,
                    decoration: TextDecoration.underline))));
        break;
      default:
        spans.add(TextSpan(children: _buildInlineSpans(node.children, base)));
    }
  }

  return spans;
}
