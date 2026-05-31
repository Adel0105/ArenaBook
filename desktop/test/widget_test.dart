import 'package:arena_book_desktop/main.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('App loads login gate', (WidgetTester tester) async {
    SharedPreferences.setMockInitialValues({});

    await tester.pumpWidget(const ArenaBookAdminApp());
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 500));

    expect(find.text('ArenaBook'), findsOneWidget);
    expect(find.text('Administracija'), findsOneWidget);
  });
}
