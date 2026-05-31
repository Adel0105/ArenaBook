import 'package:arena_book_mobile/core/app_currency.dart';

import 'package:arena_book_mobile/core/app_theme.dart';

import 'package:arena_book_mobile/models/coin_models.dart';

import 'package:arena_book_mobile/models/session_models.dart';

import 'package:arena_book_mobile/screens/coins_screen.dart';

import 'package:arena_book_mobile/services/api_error.dart';

import 'package:arena_book_mobile/services/arena_book_api.dart';

import 'package:arena_book_mobile/widgets/app_section.dart';

import 'package:flutter/material.dart';

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

    final canPay = balance >= cost && _acceptedTerms;

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
              if (widget.session.sessionKindCode == 'INVITE') ...[
                const SizedBox(height: 8),
                TextField(
                  controller: _inviteCtrl,
                  decoration: const InputDecoration(
                    labelText: 'Kod poziva',
                    prefixIcon: Icon(Icons.vpn_key_outlined),
                  ),
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
                onChanged: (v) => setState(() => _acceptedTerms = v ?? false),
                title: const Text('Prihvatam uslove korištenja'),
              ),
              if (_error != null)
                Text(_error!,
                    style:
                        TextStyle(color: Theme.of(context).colorScheme.error)),
              FilledButton.icon(
                onPressed: canPay && !_joining ? _joinWithCoins : null,
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

