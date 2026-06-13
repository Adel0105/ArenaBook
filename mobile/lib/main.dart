import 'dart:async';

import 'package:app_links/app_links.dart';
import 'package:arena_book_mobile/core/app_theme.dart';
import 'package:arena_book_mobile/core/auth_storage.dart';
import 'package:arena_book_mobile/models/current_user.dart';
import 'package:arena_book_mobile/screens/login_screen.dart';
import 'package:arena_book_mobile/screens/player_shell.dart';
import 'package:arena_book_mobile/services/arena_book_api.dart';
import 'package:arena_book_mobile/services/paypal_checkout_handler.dart';
import 'package:arena_book_mobile/services/stripe_bootstrap.dart';
import 'package:flutter/material.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await StripeBootstrap.initialize();
  runApp(const ArenaBookMobileApp());
}

class ArenaBookMobileApp extends StatefulWidget {
  const ArenaBookMobileApp({super.key});

  @override
  State<ArenaBookMobileApp> createState() => _ArenaBookMobileAppState();
}

class _ArenaBookMobileAppState extends State<ArenaBookMobileApp> {
  final AppLinks _appLinks = AppLinks();
  StreamSubscription<Uri>? _linkSub;

  @override
  void initState() {
    super.initState();
    _initDeepLinks();
  }

  Future<void> _initDeepLinks() async {
    final initial = await _appLinks.getInitialLink();
    if (initial != null) {
      PayPalCheckoutHandler.instance.handleUri(initial);
    }
    _linkSub = _appLinks.uriLinkStream.listen(
      PayPalCheckoutHandler.instance.handleUri,
      onError: (_) {},
    );
  }

  @override
  void dispose() {
    _linkSub?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'ArenaBook',
      debugShowCheckedModeBanner: false,
      theme: buildAppTheme(),
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
      if (!u.isPlayer) {
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

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    if (_user != null) {
      return PlayerShell(
        api: _api,
        user: _user!,
        onLogout: _logout,
        onUserUpdated: (u) => setState(() => _user = u),
      );
    }
    return LoginScreen(
      api: _api,
      onLoggedIn: (u) => setState(() => _user = u),
    );
  }
}

