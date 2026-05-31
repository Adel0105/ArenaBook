import 'package:arena_book_mobile/core/auth_storage.dart';
import 'package:arena_book_mobile/models/current_user.dart';
import 'package:arena_book_mobile/models/hall_models.dart';
import 'package:arena_book_mobile/services/api_error.dart';
import 'package:arena_book_mobile/services/arena_book_api.dart';
import 'package:arena_book_mobile/widgets/app_logo.dart';
import 'package:arena_book_mobile/widgets/app_section.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen(
      {super.key, required this.api, required this.onRegistered});

  final ArenaBookApi api;
  final void Function(CurrentUser user) onRegistered;

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _emailCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();
  final _firstNameCtrl = TextEditingController();
  final _lastNameCtrl = TextEditingController();
  List<CityItem> _cities = [];
  int? _cityId;
  DateTime? _dob;
  bool _loading = false;
  bool _citiesLoading = true;
  String? _citiesError;
  String? _error;
  static final _dateFmt = DateFormat('dd.MM.yyyy');

  @override
  void initState() {
    super.initState();
    _loadCities();
  }

  @override
  void dispose() {
    _emailCtrl.dispose();
    _passwordCtrl.dispose();
    _firstNameCtrl.dispose();
    _lastNameCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadCities() async {
    setState(() {
      _citiesLoading = true;
      _citiesError = null;
    });
    try {
      final res = await widget.api.cities();
      if (mounted) {
        setState(() {
          _cities = res.items;
          _citiesLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _citiesLoading = false;
          _citiesError =
              'Gradovi se nisu učitali. Povucite ekran za ponovni pokušaj.';
        });
      }
    }
  }

  Future<void> _pickDob() async {
    final date = await showDatePicker(
      context: context,
      initialDate: DateTime(2000, 1, 1),
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

  Future<void> _submit() async {
    if (_dob == null) {
      setState(() => _error = 'Datum rođenja je obavezan.');
      return;
    }
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final tokens = await widget.api.register({
        'email': _emailCtrl.text.trim(),
        'password': _passwordCtrl.text,
        'firstName': _firstNameCtrl.text.trim(),
        'lastName': _lastNameCtrl.text.trim(),
        'dateOfBirth': _dobIso(),
        if (_cityId != null) 'cityId': _cityId,
      });
      await AuthStorage.writeAccessToken(tokens.accessToken);
      widget.api.setAccessToken(tokens.accessToken);
      final user = await widget.api.me();
      widget.onRegistered(user);
      if (mounted) {
        Navigator.of(context).pop();
      }
    } on ApiError catch (e) {
      setState(() => _error = e.message);
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Registracija')),
      body: RefreshIndicator(
        onRefresh: _loadCities,
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
          children: [
            const Center(
                child: AppLogo(
                    size: 48, subtitle: 'Novi igrački račun', showTitle: true)),
            const SizedBox(height: 20),
            AppSection(
              title: 'Podaci za registraciju',
              icon: Icons.person_add_outlined,
              tone: AppSectionTone.mint,
              padding: EdgeInsets.zero,
              children: [
                TextField(
                  controller: _firstNameCtrl,
                  decoration: const InputDecoration(
                      labelText: 'Ime *',
                      prefixIcon: Icon(Icons.person_outline)),
                ),
                const SizedBox(height: 8),
                TextField(
                  controller: _lastNameCtrl,
                  decoration: const InputDecoration(
                      labelText: 'Prezime *',
                      prefixIcon: Icon(Icons.person_outline)),
                ),
                TextField(
                  controller: _emailCtrl,
                  decoration: const InputDecoration(
                      labelText: 'E-mail *',
                      prefixIcon: Icon(Icons.email_outlined)),
                  keyboardType: TextInputType.emailAddress,
                ),
                ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: const Icon(Icons.cake_outlined),
                  title: Text(_dob == null
                      ? 'Datum rođenja *'
                      : _dateFmt.format(_dob!)),
                  trailing: const Icon(Icons.calendar_today),
                  onTap: _pickDob,
                ),
                DropdownButtonFormField<int>(
                  key: ValueKey('cities-${_cities.length}-$_citiesLoading'),
                  initialValue: _cityId,
                  decoration: InputDecoration(
                    labelText: 'Grad',
                    prefixIcon: const Icon(Icons.location_city_outlined),
                    hintText: _citiesLoading
                        ? 'Učitavanje gradova…'
                        : 'Odaberite grad',
                  ),
                  items: _cities
                      .map((c) =>
                          DropdownMenuItem(value: c.id, child: Text(c.name)))
                      .toList(),
                  onChanged: _citiesLoading || _cities.isEmpty
                      ? null
                      : (v) => setState(() => _cityId = v),
                ),
                if (_citiesError != null) ...[
                  const SizedBox(height: 4),
                  Text(_citiesError!,
                      style: TextStyle(
                          color: Theme.of(context).colorScheme.error)),
                ],
                TextField(
                  controller: _passwordCtrl,
                  decoration: const InputDecoration(
                      labelText: 'Lozinka *',
                      prefixIcon: Icon(Icons.lock_outline)),
                  obscureText: true,
                ),
                if (_error != null) ...[
                  const SizedBox(height: 8),
                  Text(_error!,
                      style: TextStyle(
                          color: Theme.of(context).colorScheme.error)),
                ],
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
                      : const Icon(Icons.check),
                  label: Text(_loading ? 'Registracija…' : 'Registruj se'),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

