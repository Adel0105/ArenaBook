import 'package:arena_book_desktop/core/auth_storage.dart';
import 'package:arena_book_desktop/models/current_user.dart';
import 'package:arena_book_desktop/screens/login_screen.dart';
import 'package:arena_book_desktop/services/arena_book_api.dart';
import 'package:arena_book_desktop/widgets/admin_shell.dart';
import 'package:flutter/material.dart';

void main() {
  runApp(const ArenaBookAdminApp());
}

class ArenaBookAdminApp extends StatelessWidget {
  const ArenaBookAdminApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'ArenaBook Admin',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF16A34A),
          brightness: Brightness.light,
        ),
        useMaterial3: true,
      ),
      home: const _SessionGate(),
    );
  }
}

class _SessionGate extends StatefulWidget {
  const _SessionGate();

  @override
  State<_SessionGate> createState() => _SessionGateState();
}

class _SessionGateState extends State<_SessionGate> {
  final ArenaBookApi _api = ArenaBookApi();
  bool _loading = true;
  CurrentUser? _user;

  @override
  void initState() {
    super.initState();
    _restore();
  }

  Future<void> _restore() async {
    final token = await AuthStorage.readAccessToken();
    if (token == null || token.isEmpty) {
      setState(() {
        _loading = false;
        _user = null;
      });
      return;
    }
    _api.setAccessToken(token);
    _api.onUnauthorized = _handleUnauthorized;
    try {
      final u = await _api.me();
      if (!u.isAdministrator) {
        await AuthStorage.clearAccessToken();
        _api.setAccessToken(null);
        setState(() {
          _loading = false;
          _user = null;
        });
        return;
      }
      setState(() {
        _loading = false;
        _user = u;
      });
    } catch (_) {
      await AuthStorage.clearAccessToken();
      _api.setAccessToken(null);
      setState(() {
        _loading = false;
        _user = null;
      });
    }
  }

  Future<void> _logout() async {
    try {
      await _api.logout();
    } catch (_) {
      // Lokalno čišćenje i kad server nije dostupan.
    }
    await AuthStorage.clearAccessToken();
    _api.setAccessToken(null);
    setState(() => _user = null);
  }

  void _handleUnauthorized() {
    AuthStorage.clearAccessToken();
    _api.setAccessToken(null);
    if (mounted) {
      setState(() => _user = null);
    }
  }

  void _onLoggedIn(CurrentUser user) {
    setState(() => _user = user);
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    }
    if (_user != null) {
      return AdminShell(
        api: _api,
        user: _user!,
        onLogout: _logout,
      );
    }
    return LoginScreen(
      api: _api,
      onLoggedIn: _onLoggedIn,
    );
  }
}

