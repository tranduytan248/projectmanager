import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:ttkdgp_mobile/core/widgets/app_rich_editor.dart';

void main() {
  group('richTextToHtml', () {
    test('escape &, <, > truoc khi ghep the, khong mat noi dung o giua', () {
      final html = richTextToHtml('so sanh x < 5 va y > 10');

      expect(html, '<p>so sanh x &lt; 5 va y &gt; 10</p>');
    });

    test('escape dau & thanh thuc the', () {
      final html = richTextToHtml('A & B');

      expect(html, '<p>A &amp; B</p>');
    });

    test('nhieu doan cach nhau boi dong trong tao ra nhieu the <p>', () {
      final html = richTextToHtml('Doan mot\n\nDoan hai');

      expect(html, '<p>Doan mot</p><p>Doan hai</p>');
    });

    test('nhieu dong lien tiep trong cung mot doan noi nhau bang <br />', () {
      final html = richTextToHtml('Dong mot\nDong hai');

      expect(html, '<p>Dong mot<br />Dong hai</p>');
    });

    test('toan bo dong bat dau bang "- " duoc boc thanh <ul><li>', () {
      final html = richTextToHtml('- Muc mot\n- Muc hai');

      expect(html, '<ul><li>Muc mot</li><li>Muc hai</li></ul>');
    });

    test('toan bo dong bat dau bang "so. " duoc boc thanh <ol><li>', () {
      final html = richTextToHtml('1. Muc mot\n2. Muc hai');

      expect(html, '<ol><li>Muc mot</li><li>Muc hai</li></ol>');
    });

    test('cu phap dam/gach chan/nghieng duoc chuyen dung the', () {
      final html = richTextToHtml('**dam** __gach_chan__ //nghieng//');

      expect(html, '<p><b>dam</b> <u>gach_chan</u> <i>nghieng</i></p>');
    });

    test('link markup [chu](https://...) duoc chuyen thanh the <a href>', () {
      final html = richTextToHtml('[Trang chu](https://example.com)');

      expect(html, '<p><a href="https://example.com">Trang chu</a></p>');
    });

    test('link khong phai http/https khong duoc nhan dang, giu nguyen dang da escape', () {
      final html = richTextToHtml('[a](javascript:alert(1))');

      expect(html, isNot(contains('<a')));
      expect(html, '<p>[a](javascript:alert(1))</p>');
    });
  });

  group('AppRichEditorController.wrapSelection', () {
    test('co van ban duoc chon: boc cap ky hieu quanh phan da chon, giu nguyen vung chon', () {
      final controller = AppRichEditorController(text: 'xin chao ban');
      controller.selection = const TextSelection(baseOffset: 0, extentOffset: 3);

      controller.wrapSelection('**', '**');

      expect(controller.text, '**xin** chao ban');
      expect(controller.selection, const TextSelection(baseOffset: 2, extentOffset: 5));
    });

    test('khong co gi duoc chon: chen cap ky hieu voi con tro dat o giua', () {
      final controller = AppRichEditorController(text: 'xin chao');
      // Con tro dat ngay truoc "chao" (offset 4, sau ky tu khoang trang).
      controller.selection = const TextSelection.collapsed(offset: 4);

      controller.wrapSelection('**', '**');

      expect(controller.text, 'xin ****chao');
      // Con tro phai nam giua cap ky hieu vua chen (offset + do dai "**").
      expect(controller.selection, const TextSelection.collapsed(offset: 6));
    });
  });

  group('AppRichEditorController.toggleLinePrefix', () {
    test('bat tien to o dong hien tai (dong chua con tro), khong dong bang lien quan khac', () {
      final controller = AppRichEditorController(text: 'Dong mot\nDong hai\nDong ba');
      // Con tro dat trong "Dong hai".
      controller.selection = const TextSelection.collapsed(offset: 12);

      controller.toggleLinePrefix('- ');

      expect(controller.text, 'Dong mot\n- Dong hai\nDong ba');
    });

    test('tat tien to khi dong hien tai da co san tien to do', () {
      final controller = AppRichEditorController(text: 'Dong mot\n- Dong hai\nDong ba');
      // Con tro dat trong "Dong hai" (sau khi da co tien to "- ").
      controller.selection = const TextSelection.collapsed(offset: 14);

      controller.toggleLinePrefix('- ');

      expect(controller.text, 'Dong mot\nDong hai\nDong ba');
    });
  });

  group('AppRichEditorController.insertLink', () {
    test('chen dung cu phap [label](url) tai vi tri con tro', () {
      final controller = AppRichEditorController(text: 'Xem them: ');
      controller.selection = const TextSelection.collapsed(offset: 10);

      controller.insertLink('trang chu', 'https://example.com');

      expect(controller.text, 'Xem them: [trang chu](https://example.com)');
      expect(controller.selection,
          TextSelection.collapsed(offset: controller.text.length));
    });
  });

  group('AppRichEditorController.toHtml', () {
    test('goi dung richTextToHtml voi noi dung buffer hien tai', () {
      final controller = AppRichEditorController(text: 'Xin chao');

      expect(controller.toHtml(), richTextToHtml('Xin chao'));
      expect(controller.toHtml(), '<p>Xin chao</p>');
    });
  });
}
