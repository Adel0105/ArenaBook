import 'package:arena_book_mobile/core/app_theme.dart';

import 'package:arena_book_mobile/core/auth_storage.dart';

import 'package:arena_book_mobile/models/auth_tokens.dart';

import 'package:arena_book_mobile/models/current_user.dart';

import 'package:arena_book_mobile/screens/forgot_password_screen.dart';

import 'package:arena_book_mobile/screens/register_screen.dart';

import 'package:arena_book_mobile/services/api_error.dart';

import 'package:arena_book_mobile/services/arena_book_api.dart';

import 'package:arena_book_mobile/widgets/app_logo.dart';

import 'package:flutter/material.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key, required this.api, required this.onLoggedIn});

  final ArenaBookApi api;

  final void Function(CurrentUser user) onLoggedIn;

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _emailCtrl = TextEditingController();

  final _passwordCtrl = TextEditingController();

  bool _loading = false;

  String? _error;

  @override
  void dispose() {
    _emailCtrl.dispose();

    _passwordCtrl.dispose();

    super.dispose();
  }

  Future<void> _submit() async {
    setState(() {
      _loading = true;

      _error = null;
    });

    try {
      final tokens = await widget.api.login(
        email: _emailCtrl.text.trim(),
        password: _passwordCtrl.text,
      );

      await _persistAndEnter(tokens);
    } on ApiError catch (e) {
      setState(() => _error = e.message);
    } catch (e) {
      setState(() => _error = ApiError.friendlyMessage(e));
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _persistAndEnter(AuthTokens tokens) async {
    await AuthStorage.writeAccessToken(tokens.accessToken);
    widget.api.setAccessToken(tokens.accessToken);
    try {
      final user = await widget.api.me();
      if (!user.isPlayer) {
        await AuthStorage.clearAccessToken();
        widget.api.setAccessToken(null);
        throw ApiError(403, 'Ovaj račun nije igrački profil.');
      }
      widget.onLoggedIn(user);
    } catch (e) {
      await AuthStorage.clearAccessToken();
      widget.api.setAccessToken(null);
      rethrow;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Container(
        width: double.infinity,
        height: double.infinity,
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [AppColors.slateDark, AppColors.forest],
          ),
        ),
        child: SafeArea(
          child: Center(
            child: SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const AppLogo(
                    size: 56,
                    subtitle: 'Prijava igrača',
                    lightOnDark: true,
                  ),
                  const SizedBox(height: 28),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(20),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          TextField(
                            controller: _emailCtrl,
                            decoration: const InputDecoration(
                              labelText: 'E-mail',
                              prefixIcon: Icon(Icons.email_outlined),
                            ),
                            keyboardType: TextInputType.emailAddress,
                          ),
                          const SizedBox(height: 12),
                          TextField(
                            controller: _passwordCtrl,
                            decoration: const InputDecoration(
                              labelText: 'Lozinka',
                              prefixIcon: Icon(Icons.lock_outline),
                            ),
                            obscureText: true,
                          ),
                          if (_error != null) ...[
                            const SizedBox(height: 12),
                            Row(
                              children: [
                                Icon(Icons.error_outline,
                                    size: 18,
                                    color: Theme.of(context).colorScheme.error),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: Text(
                                    _error!,
                                    style: TextStyle(
                                        color: Theme.of(context)
                                            .colorScheme
                                            .error),
                                  ),
                                ),
                              ],
                            ),
                          ],
                          const SizedBox(height: 20),
                          FilledButton.icon(
                            onPressed: _loading ? null : _submit,
                            icon: _loading
                                ? const SizedBox(
                                    width: 18,
                                    height: 18,
                                    child: CircularProgressIndicator(
                                        strokeWidth: 2, color: Colors.white),
                                  )
                                : const Icon(Icons.login),
                            label: Text(_loading ? 'Prijava…' : 'Prijavi se'),
                          ),
                          TextButton.icon(
                            onPressed: () {
                              Navigator.of(context).push(
                                MaterialPageRoute(
                                  builder: (_) => RegisterScreen(
                                      api: widget.api,
                                      onRegistered: widget.onLoggedIn),
                                ),
                              );
                            },
                            icon: const Icon(Icons.person_add_outlined),
                            label: const Text('Registracija'),
                          ),
                          TextButton.icon(
                            onPressed: () {
                              Navigator.of(context).push(
                                MaterialPageRoute(
                                    builder: (_) =>
                                        ForgotPasswordScreen(api: widget.api)),
                              );
                            },
                            icon: const Icon(Icons.help_outline),
                            label: const Text('Zaboravljena lozinka'),
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

