import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:ttkdgp_mobile/features/mywork/comment_html.dart';

void main() {
  Future<void> pump(WidgetTester tester, String html) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(body: renderCommentHtml(html)),
      ),
    );
  }

  testWidgets('doan van don gian <p>...</p> hien dung noi dung chu', (tester) async {
    await pump(tester, '<p>Xin chào</p>');

    expect(find.text('Xin chào'), findsOneWidget);
  });

  testWidgets('the <b> va <i> long nhau trong cung mot doan khong bi loi', (tester) async {
    await pump(tester, '<p><b>đậm</b> và <i>nghiêng</i></p>');

    expect(tester.takeException(), isNull);
  });

  testWidgets('danh sach <ul><li> hien du tung dong', (tester) async {
    await pump(tester, '<ul><li>Một</li><li>Hai</li></ul>');

    expect(find.text('Một'), findsOneWidget);
    expect(find.text('Hai'), findsOneWidget);
  });

  testWidgets('dau vao rong van hien thi khong throw', (tester) async {
    await pump(tester, '');

    expect(tester.takeException(), isNull);
  });

  testWidgets('dau vao chi la chu thuong (khong the nao) hien dung chu do', (tester) async {
    await pump(tester, 'chi la chu thuong khong co the');

    expect(find.text('chi la chu thuong khong co the'), findsOneWidget);
  });

  testWidgets('the la khong nam trong whitelist khong lam parser throw', (tester) async {
    await pump(tester, '<script>alert(1)</script><p>Vẫn còn nội dung</p>');

    expect(tester.takeException(), isNull);
  });
}
