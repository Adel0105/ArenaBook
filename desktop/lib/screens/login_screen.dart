import 'package:arena_book_desktop/core/auth_storage.dart';
import 'package:arena_book_desktop/models/current_user.dart';
import 'package:arena_book_desktop/services/api_error.dart';
import 'package:arena_book_desktop/services/arena_book_api.dart';
import 'package:flutter/material.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({
    super.key,
    required this.api,
    required this.onLoggedIn,
  });

  final ArenaBookApi api;
  final void Function(CurrentUser user) onLoggedIn;

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController(text: 'admin@arena.local');
  final _password = TextEditingController();
  bool _obscure = true;
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() => _error = null);
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }
    setState(() => _busy = true);
    try {
      widget.api.setAccessToken(null);
      final tokens = await widget.api.login(
        email: _email.text.trim(),
        password: _password.text,
      );
      widget.api.setAccessToken(tokens.accessToken);
      await AuthStorage.writeAccessToken(tokens.accessToken);
      final user = await widget.api.me();
      if (!user.isAdministrator) {
        await AuthStorage.clearAccessToken();
        widget.api.setAccessToken(null);
        setState(() {
          _busy = false;
          _error = 'Samo korisnici s ulogom administratora mogu se prijaviti u ovu aplikaciju.';
        });
        return;
      }
      if (!mounted) {
        return;
      }
      setState(() => _busy = false);
      widget.onLoggedIn(user);
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() {
        _busy = false;
        _error = e.message;
      });
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() {
        _busy = false;
        _error = 'Povezivanje s API-jem nije uspjelo. Provjerite da li je server pokrenut.';
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: Container(
        width: double.infinity,
        height: double.infinity,
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [
              Color(0xFF0F172A),
              Color(0xFF14532D),
            ],
          ),
        ),
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: Card(
              elevation: 8,
              child: Padding(
                padding: const EdgeInsets.all(28),
                child: Form(
                  key: _formKey,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Icon(Icons.stadium, size: 48, color: theme.colorScheme.primary),
                      const SizedBox(height: 12),
                      Text(
                        'ArenaBook',
                        textAlign: TextAlign.center,
                        style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w700),
                      ),
                      Text(
                        'Administracija',
                        textAlign: TextAlign.center,
                        style: theme.textTheme.titleMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                      ),
                      const SizedBox(height: 28),
                      TextFormField(
                        controller: _email,
                        keyboardType: TextInputType.emailAddress,
                        autofillHints: const [AutofillHints.username],
                        decoration: const InputDecoration(
                          labelText: 'E-mail',
                          border: OutlineInputBorder(),
                          prefixIcon: Icon(Icons.mail_outline),
                        ),
                        validator: (v) {
                          if (v == null || v.trim().isEmpty) {
                            return 'Unesite e-mail.';
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _password,
                        obscureText: _obscure,
                        autofillHints: const [AutofillHints.password],
                        decoration: InputDecoration(
                          labelText: 'Lozinka',
                          border: const OutlineInputBorder(),
                          prefixIcon: const Icon(Icons.lock_outline),
                          suffixIcon: IconButton(
                            onPressed: () => setState(() => _obscure = !_obscure),
                            icon: Icon(_obscure ? Icons.visibility_outlined : Icons.visibility_off_outlined),
                          ),
                        ),
                        validator: (v) {
                          if (v == null || v.isEmpty) {
                            return 'Unesite lozinku.';
                          }
                          return null;
                        },
                        onFieldSubmitted: (_) => _busy ? null : _submit(),
                      ),
                      if (_error != null) ...[
                        const SizedBox(height: 16),
                        Material(
                          color: theme.colorScheme.errorContainer,
                          borderRadius: BorderRadius.circular(8),
                          child: Padding(
                            padding: const EdgeInsets.all(12),
                            child: Row(
                              children: [
                                Icon(Icons.error_outline, color: theme.colorScheme.onErrorContainer),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: Text(
                                    _error!,
                                    style: TextStyle(color: theme.colorScheme.onErrorContainer),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ],
                      const SizedBox(height: 24),
                      FilledButton(
                        onPressed: _busy ? null : _submit,
                        child: _busy
                            ? const SizedBox(
                                height: 22,
                                width: 22,
                                child: CircularProgressIndicator(strokeWidth: 2),
                              )
                            : const Text('Prijava'),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

