import 'package:arena_book_mobile/main.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('App starts with session gate', (tester) async {
    await tester.pumpWidget(const ArenaBookMobileApp());
    expect(find.byType(ArenaBookMobileApp), findsOneWidget);
  });
}
