import 'package:arena_book_mobile/screens/reset_password_screen.dart';
import 'package:arena_book_mobile/services/api_error.dart';
import 'package:arena_book_mobile/services/arena_book_api.dart';
import 'package:arena_book_mobile/widgets/app_logo.dart';
import 'package:arena_book_mobile/widgets/app_section.dart';
import 'package:flutter/material.dart';

class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key, required this.api});

  final ArenaBookApi api;

  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  final _emailCtrl = TextEditingController();
  bool _loading = false;
  String? _message;
  String? _devToken;

  @override
  void dispose() {
    _emailCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() {
      _loading = true;
      _message = null;
      _devToken = null;
    });
    try {
      final token = await widget.api.forgotPassword(_emailCtrl.text.trim());
      setState(() {
        _message = 'Ako račun postoji, poslan je e-mail za reset lozinke.';
        _devToken = token;
      });
    } on ApiError catch (e) {
      setState(() => _message = e.message);
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Zaboravljena lozinka')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          const Center(child: AppLogo(size: 44, subtitle: 'Reset lozinke')),
          const SizedBox(height: 16),
          AppSection(
            title: 'Zaboravljena lozinka',
            subtitle: 'Unesite e-mail računa',
            icon: Icons.lock_reset,
            tone: AppSectionTone.slate,
            padding: EdgeInsets.zero,
            children: [
              TextField(
                controller: _emailCtrl,
                decoration: const InputDecoration(
                  labelText: 'E-mail',
                  prefixIcon: Icon(Icons.email_outlined),
                ),
              ),
              const SizedBox(height: 12),
              FilledButton.icon(
                onPressed: _loading ? null : _submit,
                icon: _loading
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(
                            strokeWidth: 2, color: Colors.white),
                      )
                    : const Icon(Icons.send_outlined),
                label: Text(_loading ? 'Slanje…' : 'Pošalji'),
              ),
            ],
          ),
          if (_message != null) ...[
            const SizedBox(height: 16),
            Text(_message!),
          ],
          if (_devToken != null) ...[
            const SizedBox(height: 8),
            Text('Development token:',
                style: Theme.of(context).textTheme.labelSmall),
            SelectableText(_devToken!),
            const SizedBox(height: 8),
            OutlinedButton.icon(
              onPressed: () {
                Navigator.of(context).push(
                  MaterialPageRoute(
                    builder: (_) => ResetPasswordScreen(
                      api: widget.api,
                      email: _emailCtrl.text.trim(),
                      token: _devToken!,
                    ),
                  ),
                );
              },
              icon: const Icon(Icons.vpn_key_outlined),
              label: const Text('Unesi novu lozinku'),
            ),
          ],
        ],
      ),
    );
  }
}

