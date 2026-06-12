import 'package:arena_book_mobile/core/app_currency.dart';

import 'package:arena_book_mobile/core/app_theme.dart';

import 'package:arena_book_mobile/models/coin_models.dart';

import 'package:arena_book_mobile/models/session_models.dart';

import 'package:arena_book_mobile/screens/coins_screen.dart';

import 'package:arena_book_mobile/services/api_error.dart';

import 'package:arena_book_mobile/services/arena_book_api.dart';

import 'package:arena_book_mobile/widgets/app_section.dart';

import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

class JoinSessionScreen extends StatefulWidget {
  const JoinSessionScreen(
      {super.key, required this.api, required this.session});

  final ArenaBookApi api;

  final SessionListItem session;

  @override
  State<JoinSessionScreen> createState() => _JoinSessionScreenState();
}

class _JoinSessionScreenState extends State<JoinSessionScreen> {
  SessionJoinQuote? _quote;

  CoinWallet? _wallet;

  final _inviteCtrl = TextEditingController();

  bool _acceptedTerms = false;

  bool _loading = true;

  bool _joining = false;

  String? _error;
  String? _inviteError;
  String? _termsError;
  String? _balanceError;
  static final _fmt = DateFormat('dd.MM.yyyy HH:mm');

  @override
  void initState() {
    super.initState();

    _load();
  }

  @override
  void dispose() {
    _inviteCtrl.dispose();

    super.dispose();
  }

  Future<void> _load() async {
    setState(() => _loading = true);

    try {
      final quote = await widget.api.joinQuote(widget.session.id);

      final wallet = await widget.api.wallet();

      if (mounted) {
        setState(() {
          _quote = quote;

          _wallet = wallet;

          _loading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = e.toString();

          _loading = false;
        });
      }
    }
  }

  String? _validateInviteCode() {
    if (widget.session.sessionKindCode != 'INVITE') {
      return null;
    }
    final code = _inviteCtrl.text.trim();
    if (code.isEmpty) {
      return 'Unesite kod poziva za privatni termin.';
    }
    if (code.length < 4) {
      return 'Kod poziva mora imati najmanje 4 znaka.';
    }
    return null;
  }

  String? _validateTerms() {
    if (!_acceptedTerms) {
      return 'Morate prihvatiti uslove korištenja prije rezervacije.';
    }
    return null;
  }

  String? _validateBalance(double cost, double balance) {
    if (balance < cost) {
      return 'Nedovoljno novčića na računu. Dokupite novčiće prije rezervacije.';
    }
    return null;
  }

  bool _validateJoinForm(double cost, double balance) {
    final inviteError = _validateInviteCode();
    final termsError = _validateTerms();
    final balanceError = _validateBalance(cost, balance);
    setState(() {
      _inviteError = inviteError;
      _termsError = termsError;
      _balanceError = balanceError;
      _error = null;
    });
    return inviteError == null && termsError == null && balanceError == null;
  }

  Future<bool> _confirmJoin(double cost, double balance) async {
    final startLocal = _fmt.format(widget.session.startUtc.toLocal());
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        icon: const Icon(Icons.event_available_outlined, size: 40),
        title: const Text('Potvrdite rezervaciju'),
        content: Text(
          'Dvorana: ${widget.session.hallName}\n'
          'Termin: $startLocal\n'
          'Iznos: ${AppCurrency.format(cost)}\n'
          'Trenutno stanje: ${AppCurrency.format(balance)}\n\n'
          'Sa vašeg novčanika bit će skinuto ${AppCurrency.format(cost)}. '
          'Povrat novčića nije automatski ako kasnije odustanete — moguće je '
          'samo ako organizator otkaže termin.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: const Text('Odustani'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(ctx).pop(true),
            child: const Text('Potvrdi i plati'),
          ),
        ],
      ),
    );
    return confirmed ?? false;
  }

  Future<void> _onJoinPressed() async {
    final cost = _quote?.coinsRequired ?? widget.session.priceTotalCoins;
    final balance = _wallet?.balanceCoins ?? 0;

    if (!_validateJoinForm(cost, balance)) {
      return;
    }

    if (!await _confirmJoin(cost, balance)) {
      return;
    }

    await _joinWithCoins();
  }

  Future<void> _joinWithCoins() async {
    setState(() {
      _joining = true;

      _error = null;
    });

    try {
      final cost = _quote?.coinsRequired ?? widget.session.priceTotalCoins;

      await widget.api.joinSession(
        widget.session.id,
        inviteCode: widget.session.sessionKindCode == 'INVITE'
            ? _inviteCtrl.text.trim()
            : null,
      );

      if (!mounted) {
        return;
      }

      await showDialog<void>(
        context: context,
        builder: (ctx) => AlertDialog(
          icon: const Icon(Icons.check_circle,
              color: AppColors.primaryGreen, size: 48),
          title: const Text('Rezervacija uspješna'),
          content: Text(
            'Termin "${widget.session.hallName}" je rezervisan. Plaćanje od ${AppCurrency.format(cost)} je obrađeno.',
          ),
          actions: [
            FilledButton(
              onPressed: () => Navigator.of(ctx).pop(),
              child: const Text('Nastavi'),
            ),
          ],
        ),
      );

      if (mounted) {
        Navigator.of(context).pop(true);
      }
    } on ApiError catch (e) {
      setState(() => _error = e.message);
    } finally {
      if (mounted) {
        setState(() => _joining = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return Scaffold(
        appBar: AppBar(title: const Text('Pridruži se')),
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    final cost = _quote?.coinsRequired ?? widget.session.priceTotalCoins;

    final balance = _wallet?.balanceCoins ?? 0;

    return Scaffold(
      appBar: AppBar(
        title: const Row(
          children: [
            Icon(Icons.login, size: 22),
            SizedBox(width: 8),
            Text('Plaćanje novčićima'),
          ],
        ),
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(0, 8, 0, 24),
        children: [
          AppSection(
            title: widget.session.hallName,
            subtitle: 'Pregled rezervacije',
            icon: Icons.stadium_outlined,
            tone: AppSectionTone.mint,
            children: [
              _infoRow(
                  Icons.toll_outlined, 'Potrebno', AppCurrency.format(cost)),
              _infoRow(Icons.account_balance_wallet_outlined, 'Stanje',
                  AppCurrency.format(balance)),
              if (_balanceError != null) ...[
                const SizedBox(height: 4),
                Text(
                  _balanceError!,
                  style: TextStyle(
                    color: Theme.of(context).colorScheme.error,
                    fontSize: 13,
                  ),
                ),
              ],
              if (widget.session.sessionKindCode == 'INVITE') ...[
                const SizedBox(height: 8),
                TextField(
                  controller: _inviteCtrl,
                  decoration: InputDecoration(
                    labelText: 'Kod poziva',
                    prefixIcon: const Icon(Icons.vpn_key_outlined),
                    errorText: _inviteError,
                  ),
                  onChanged: (_) {
                    if (_inviteError != null) {
                      setState(() => _inviteError = _validateInviteCode());
                    }
                  },
                ),
              ],
              if (widget.session.maxAgeYears != null)
                _infoRow(Icons.cake_outlined, 'Max dob',
                    '${widget.session.maxAgeYears} godina'),
            ],
          ),
          AppSection(
            title: 'Uslovi',
            icon: Icons.gavel_outlined,
            tone: AppSectionTone.slate,
            children: [
              CheckboxListTile(
                value: _acceptedTerms,
                onChanged: (v) => setState(() {
                  _acceptedTerms = v ?? false;
                  if (_termsError != null) {
                    _termsError = _validateTerms();
                  }
                }),
                title: const Text('Prihvatam uslove korištenja'),
                subtitle: _termsError != null
                    ? Text(
                        _termsError!,
                        style: TextStyle(
                          color: Theme.of(context).colorScheme.error,
                        ),
                      )
                    : null,
                controlAffinity: ListTileControlAffinity.leading,
              ),
              if (_error != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Text(
                    _error!,
                    style: TextStyle(
                      color: Theme.of(context).colorScheme.error,
                    ),
                  ),
                ),
              FilledButton.icon(
                onPressed: _joining ? null : _onJoinPressed,
                icon: _joining
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(
                            strokeWidth: 2, color: Colors.white),
                      )
                    : const Icon(Icons.check),
                label: Text(
                    _joining ? 'Obrada…' : 'Plati ${AppCurrency.format(cost)}'),
              ),
              OutlinedButton.icon(
                onPressed: () {
                  Navigator.of(context)
                      .push(
                        MaterialPageRoute(
                            builder: (_) => CoinsScreen(api: widget.api)),
                      )
                      .then((_) => _load());
                },
                icon: const Icon(Icons.add_shopping_cart_outlined),
                label: const Text('Dokupi novčiće'),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _infoRow(IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Icon(icon, size: 20, color: AppColors.forest),
          const SizedBox(width: 8),
          Text('$label: ', style: const TextStyle(fontWeight: FontWeight.w500)),
          Expanded(child: Text(value)),
        ],
      ),
    );
  }
}

