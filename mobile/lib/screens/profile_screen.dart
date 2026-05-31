import 'package:arena_book_mobile/core/app_currency.dart';

import 'package:arena_book_mobile/core/app_theme.dart';

import 'package:arena_book_mobile/models/current_user.dart';

import 'package:arena_book_mobile/models/hall_models.dart';

import 'package:arena_book_mobile/models/session_models.dart';

import 'package:arena_book_mobile/screens/reservation_history_screen.dart';

import 'package:arena_book_mobile/services/api_error.dart';

import 'package:arena_book_mobile/services/arena_book_api.dart';

import 'package:arena_book_mobile/widgets/app_section.dart';

import 'package:flutter/material.dart';

import 'package:intl/intl.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({
    super.key,
    required this.api,
    required this.user,
    required this.onLogout,
    required this.onUserUpdated,
  });

  final ArenaBookApi api;

  final CurrentUser user;

  final VoidCallback onLogout;

  final void Function(CurrentUser user) onUserUpdated;

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  final _firstNameCtrl = TextEditingController();

  final _lastNameCtrl = TextEditingController();

  final _profileUrlCtrl = TextEditingController();

  final _currentPassCtrl = TextEditingController();

  final _newPassCtrl = TextEditingController();

  List<CityItem> _cities = [];

  PlayerProfileStats? _stats;

  int? _cityId;

  DateTime? _dob;

  double _balance = 0;

  bool _saving = false;

  static final _dateFmt = DateFormat('dd.MM.yyyy');

  @override
  void initState() {
    super.initState();

    _fill(widget.user);
    _profileUrlCtrl.addListener(() {
      if (mounted) setState(() {});
    });

    _loadExtra();
  }

  void _fill(CurrentUser u) {
    _firstNameCtrl.text = u.firstName;

    _lastNameCtrl.text = u.lastName;

    _profileUrlCtrl.text = u.profileImageUrl ?? '';

    _cityId = u.cityId;

    _dob = u.dateOfBirth;
  }

  Future<void> _loadExtra() async {
    try {
      final cities = await widget.api.cities();

      final wallet = await widget.api.wallet();

      final stats = await widget.api.profileStats();

      if (mounted) {
        setState(() {
          _cities = cities.items;

          _balance = wallet.balanceCoins;

          _stats = stats;
        });
      }
    } catch (_) {}
  }

  @override
  void dispose() {
    _firstNameCtrl.dispose();

    _lastNameCtrl.dispose();

    _profileUrlCtrl.dispose();

    _currentPassCtrl.dispose();

    _newPassCtrl.dispose();

    super.dispose();
  }

  Future<void> _pickDob() async {
    final date = await showDatePicker(
      context: context,
      initialDate: _dob ?? DateTime(2000, 1, 1),
      firstDate: DateTime(1950),
      lastDate: DateTime.now(),
    );

    if (date != null) {
      setState(() => _dob = date);
    }
  }

  String? _dobIso() {
    if (_dob == null) {
      return null;
    }

    final d = _dob!;

    return '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';
  }

  Future<void> _saveProfile() async {
    if (_dob == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Datum rođenja je obavezan.')),
      );

      return;
    }

    setState(() => _saving = true);

    try {
      final updated = await widget.api.updateProfile({
        'firstName': _firstNameCtrl.text.trim(),
        'lastName': _lastNameCtrl.text.trim(),
        'dateOfBirth': _dobIso(),
        if (_cityId != null) 'cityId': _cityId,
        'profileImageUrl': _profileUrlCtrl.text.trim().isEmpty
            ? null
            : _profileUrlCtrl.text.trim(),
      });

      widget.onUserUpdated(updated);

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Profil ažuriran.')),
        );
      }
    } on ApiError catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.message)));
      }
    } finally {
      if (mounted) {
        setState(() => _saving = false);
      }
    }
  }

  Future<void> _changePassword() async {
    try {
      await widget.api.changePassword(
        currentPassword: _currentPassCtrl.text,
        newPassword: _newPassCtrl.text,
      );

      _currentPassCtrl.clear();

      _newPassCtrl.clear();

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Lozinka promijenjena.')),
        );
      }
    } on ApiError catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(e.message)));
      }
    }
  }

  String get _profileImagePreview {
    final typed = _profileUrlCtrl.text.trim();
    if (typed.isNotEmpty) return typed;
    return widget.user.profileImageUrl ?? '';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Row(
          children: [
            Icon(Icons.person_outline, size: 22),
            SizedBox(width: 8),
            Text('Profil'),
          ],
        ),
        actions: [
          IconButton(
            tooltip: 'Odjava',
            icon: const Icon(Icons.logout),
            onPressed: widget.onLogout,
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(0, 8, 0, 24),
        children: [
          AppSection(
            title: 'Račun',
            subtitle: widget.user.email,
            icon: Icons.badge_outlined,
            tone: AppSectionTone.mint,
            children: [
              Row(
                children: [
                  CircleAvatar(
                    radius: 28,
                    backgroundColor: AppColors.mintSurface,
                    backgroundImage: _profileImagePreview.isNotEmpty
                        ? NetworkImage(_profileImagePreview)
                        : null,
                    child: _profileImagePreview.isEmpty
                        ? Text(
                            widget.user.firstName.isNotEmpty
                                ? widget.user.firstName[0].toUpperCase()
                                : '?',
                            style: const TextStyle(
                                fontSize: 24,
                                fontWeight: FontWeight.bold,
                                color: AppColors.forest),
                          )
                        : null,
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(widget.user.displayName,
                            style: Theme.of(context).textTheme.titleMedium),
                        const SizedBox(height: 4),
                        NovcicChip(amount: _balance),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              OutlinedButton.icon(
                onPressed: () {
                  Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (_) => ReservationHistoryScreen(api: widget.api),
                    ),
                  );
                },
                icon: const Icon(Icons.history),
                label: const Text('Historija rezervacija'),
              ),
            ],
          ),
          if (_stats != null)
            AppSection(
              title: 'Statistika igranja',
              subtitle: 'Pregled aktivnosti',
              icon: Icons.insights_outlined,
              tone: AppSectionTone.slate,
              children: [
                _statRow(Icons.event_available, 'Ukupno rezervacija',
                    '${_stats!.totalParticipations}'),
                _statRow(Icons.check_circle_outline, 'Završeno',
                    '${_stats!.completedParticipations}'),
                _statRow(Icons.event_note, 'Organizirano',
                    '${_stats!.organizedSessions}'),
                _statRow(Icons.trending_up, 'Učestalost',
                    '${_stats!.playFrequencyPerMonth} / mjesec'),
                _statRow(Icons.toll_outlined, 'Potrošeno',
                    AppCurrency.format(_stats!.totalCoinsSpentOnSessions)),
                _statRow(Icons.shopping_bag_outlined, 'Kupljeno',
                    AppCurrency.format(_stats!.totalCoinsPurchased)),
              ],
            ),
          AppSection(
            title: 'Uredi profil',
            subtitle: 'Lični podaci',
            icon: Icons.edit_outlined,
            tone: AppSectionTone.neutral,
            children: [
              TextField(
                controller: _firstNameCtrl,
                decoration: const InputDecoration(
                    labelText: 'Ime', prefixIcon: Icon(Icons.person_outline)),
              ),
              const SizedBox(height: 8),
              TextField(
                controller: _lastNameCtrl,
                decoration: const InputDecoration(
                    labelText: 'Prezime',
                    prefixIcon: Icon(Icons.person_outline)),
              ),
              ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.cake_outlined),
                title: Text(
                    _dob == null ? 'Datum rođenja *' : _dateFmt.format(_dob!)),
                trailing: const Icon(Icons.calendar_today),
                onTap: _pickDob,
              ),
              DropdownButtonFormField<int>(
                initialValue: _cityId,
                decoration: const InputDecoration(
                    labelText: 'Grad',
                    prefixIcon: Icon(Icons.location_city_outlined)),
                items: _cities
                    .map((c) =>
                        DropdownMenuItem(value: c.id, child: Text(c.name)))
                    .toList(),
                onChanged: (v) => setState(() => _cityId = v),
              ),
              TextField(
                controller: _profileUrlCtrl,
                decoration: const InputDecoration(
                  labelText: 'URL profilne slike',
                  prefixIcon: Icon(Icons.image_outlined),
                  helperText: 'Unesite URL slike — pregled se ažurira odmah',
                ),
              ),
              FilledButton.icon(
                onPressed: _saving ? null : _saveProfile,
                icon: _saving
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(
                            strokeWidth: 2, color: Colors.white),
                      )
                    : const Icon(Icons.save_outlined),
                label: Text(_saving ? 'Spremanje…' : 'Spremi profil'),
              ),
            ],
          ),
          AppSection(
            title: 'Promjena lozinke',
            icon: Icons.lock_outline,
            tone: AppSectionTone.slate,
            children: [
              TextField(
                controller: _currentPassCtrl,
                decoration:
                    const InputDecoration(labelText: 'Trenutna lozinka'),
                obscureText: true,
              ),
              const SizedBox(height: 8),
              TextField(
                controller: _newPassCtrl,
                decoration: const InputDecoration(labelText: 'Nova lozinka'),
                obscureText: true,
              ),
              OutlinedButton.icon(
                onPressed: _changePassword,
                icon: const Icon(Icons.vpn_key_outlined),
                label: const Text('Promijeni lozinku'),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _statRow(IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Icon(icon, size: 20, color: AppColors.forest),
          const SizedBox(width: 10),
          Expanded(child: Text(label)),
          Text(value, style: const TextStyle(fontWeight: FontWeight.w600)),
        ],
      ),
    );
  }
}

