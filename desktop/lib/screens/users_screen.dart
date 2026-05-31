import 'package:arena_book_desktop/models/admin_user_models.dart';
import 'package:arena_book_desktop/models/paged_list.dart';
import 'package:arena_book_desktop/models/session_models.dart';
import 'package:arena_book_desktop/services/api_error.dart';
import 'package:arena_book_desktop/services/arena_book_api.dart';
import 'package:arena_book_desktop/widgets/paged_footer.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

class UsersScreen extends StatefulWidget {
  const UsersScreen({super.key, required this.api});

  final ArenaBookApi api;

  @override
  State<UsersScreen> createState() => _UsersScreenState();
}

class _UsersScreenState extends State<UsersScreen> {
  final _searchCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  PagedList<AdminUserListItem>? _page;
  List<CityItem> _cities = [];
  int _listPage = 1;
  bool? _filterLocked;
  DateTime? _filterFrom;
  DateTime? _filterTo;
  bool _loadingList = true;
  String? _listError;

  AdminUserDetails? _selected;
  bool _isNew = false;
  bool _loadingDetail = false;
  bool _saving = false;
  String? _detailError;

  final _emailFormCtrl = TextEditingController();
  final _passwordCtrl = TextEditingController();
  final _firstNameCtrl = TextEditingController();
  final _lastNameCtrl = TextEditingController();
  final _profileUrlCtrl = TextEditingController();
  int? _formCityId;
  DateTime? _formDob;
  String _formRole = 'Member';

  static final _dateFmt = DateFormat('dd.MM.yyyy');
  static const _roles = ['Administrator', 'Organizer', 'Member'];

  @override
  void initState() {
    super.initState();
    _bootstrap();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    _emailCtrl.dispose();
    _emailFormCtrl.dispose();
    _passwordCtrl.dispose();
    _firstNameCtrl.dispose();
    _lastNameCtrl.dispose();
    _profileUrlCtrl.dispose();
    super.dispose();
  }

  Future<void> _bootstrap() async {
    await Future.wait([_loadCities(), _loadList()]);
  }

  Future<void> _loadCities() async {
    try {
      final res = await widget.api.cities(pageSize: 200);
      if (!mounted) {
        return;
      }
      setState(() => _cities = res.items);
    } catch (_) {}
  }

  Future<void> _loadList({int? page}) async {
    final p = page ?? _listPage;
    setState(() {
      _loadingList = true;
      _listError = null;
    });
    try {
      final res = await widget.api.adminUsers(
        page: p,
        pageSize: 15,
        q: _searchCtrl.text.trim().isEmpty ? null : _searchCtrl.text.trim(),
        email: _emailCtrl.text.trim().isEmpty ? null : _emailCtrl.text.trim(),
        registeredFrom: _filterFrom,
        registeredTo: _filterTo,
        isLockedOut: _filterLocked,
      );
      if (!mounted) {
        return;
      }
      setState(() {
        _page = res;
        _listPage = p;
        _loadingList = false;
      });
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() {
        _listError = e.message;
        _loadingList = false;
      });
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() {
        _listError = 'Greška pri učitavanju korisnika.';
        _loadingList = false;
      });
    }
  }

  void _startNew() {
    setState(() {
      _isNew = true;
      _selected = null;
      _detailError = null;
      _emailFormCtrl.clear();
      _passwordCtrl.clear();
      _firstNameCtrl.clear();
      _lastNameCtrl.clear();
      _profileUrlCtrl.clear();
      _formCityId = null;
      _formDob = null;
      _formRole = 'Member';
    });
  }

  Future<void> _selectUser(String userId) async {
    setState(() {
      _isNew = false;
      _loadingDetail = true;
      _detailError = null;
    });
    try {
      final d = await widget.api.adminUserById(userId);
      if (!mounted) {
        return;
      }
      _fillForm(d);
      setState(() {
        _selected = d;
        _loadingDetail = false;
      });
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() {
        _detailError = e.message;
        _loadingDetail = false;
      });
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() {
        _detailError = 'Nije moguće učitati korisnika.';
        _loadingDetail = false;
      });
    }
  }

  void _fillForm(AdminUserDetails d) {
    _emailFormCtrl.text = d.email;
    _firstNameCtrl.text = d.firstName;
    _lastNameCtrl.text = d.lastName;
    _profileUrlCtrl.text = d.profileImageUrl ?? '';
    _formCityId = d.cityId;
    _formDob = d.dateOfBirth;
    _formRole = d.roles.isNotEmpty ? d.roles.first : 'Member';
  }

  Future<void> _pickDob() async {
    final initial = _formDob ?? DateTime(2000, 1, 1);
    final date = await showDatePicker(
      context: context,
      initialDate: initial,
      firstDate: DateTime(1950),
      lastDate: DateTime.now(),
    );
    if (date != null) {
      setState(() => _formDob = date);
    }
  }

  String? _dobIso() {
    final d = _formDob;
    if (d == null) {
      return null;
    }
    return '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';
  }

  Future<void> _save() async {
    if (_formDob == null) {
      setState(() => _detailError = 'Datum rođenja je obavezan.');
      return;
    }
    setState(() {
      _saving = true;
      _detailError = null;
    });
    try {
      if (_isNew) {
        final body = <String, dynamic>{
          'email': _emailFormCtrl.text.trim(),
          'password': _passwordCtrl.text,
          'firstName': _firstNameCtrl.text.trim(),
          'lastName': _lastNameCtrl.text.trim(),
          'roleName': _formRole,
          if (_formCityId != null) 'cityId': _formCityId,
          'dateOfBirth': _dobIso(),
        };
        final created = await widget.api.createAdminUser(body);
        if (!mounted) {
          return;
        }
        setState(() {
          _isNew = false;
          _selected = created;
          _saving = false;
        });
        _fillForm(created);
      } else if (_selected != null) {
        final body = <String, dynamic>{
          'firstName': _firstNameCtrl.text.trim(),
          'lastName': _lastNameCtrl.text.trim(),
          if (_formCityId != null) 'cityId': _formCityId,
          'profileImageUrl': _profileUrlCtrl.text.trim().isEmpty ? null : _profileUrlCtrl.text.trim(),
          'dateOfBirth': _dobIso(),
        };
        final updated = await widget.api.updateAdminUser(_selected!.userId, body);
        if (!mounted) {
          return;
        }
        setState(() {
          _selected = updated;
          _saving = false;
        });
        _fillForm(updated);
      }
      await _loadList(page: _listPage);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Korisnik spremljen.')));
      }
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() {
        _detailError = e.message;
        _saving = false;
      });
    } catch (_) {
      if (!mounted) {
        return;
      }
      setState(() {
        _detailError = 'Spremanje nije uspjelo.';
        _saving = false;
      });
    }
  }

  Future<void> _toggleLock() async {
    final d = _selected;
    if (d == null) {
      return;
    }
    final wasLocked = d.isLockedOut;
    setState(() => _saving = true);
    try {
      if (wasLocked) {
        await widget.api.unlockAdminUser(d.userId);
      } else {
        await widget.api.lockAdminUser(d.userId);
      }
      await _selectUser(d.userId);
      await _loadList(page: _listPage);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(wasLocked ? 'Korisnik otključan.' : 'Korisnik zaključan.')),
        );
      }
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() => _detailError = e.message);
    } finally {
      if (mounted) {
        setState(() => _saving = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SizedBox(width: 400, child: _buildListPanel(context)),
          const SizedBox(width: 16),
          Expanded(child: _buildDetailPanel(context)),
        ],
      ),
    );
  }

  Widget _buildListPanel(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: theme.colorScheme.outlineVariant),
      ),
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              children: [
                TextField(
                  controller: _searchCtrl,
                  decoration: const InputDecoration(
                    hintText: 'Ime, prezime…',
                    prefixIcon: Icon(Icons.search),
                    isDense: true,
                    border: OutlineInputBorder(),
                  ),
                  onSubmitted: (_) => _loadList(page: 1),
                ),
                const SizedBox(height: 8),
                TextField(
                  controller: _emailCtrl,
                  decoration: const InputDecoration(
                    labelText: 'E-mail',
                    isDense: true,
                    border: OutlineInputBorder(),
                  ),
                  onSubmitted: (_) => _loadList(page: 1),
                ),
                const SizedBox(height: 8),
                DropdownButtonFormField<bool?>(
                  isExpanded: true,
                  value: _filterLocked,
                  decoration: const InputDecoration(labelText: 'Zaključan', isDense: true, border: OutlineInputBorder()),
                  items: const [
                    DropdownMenuItem(value: null, child: Text('Svi')),
                    DropdownMenuItem(value: false, child: Text('Aktivni')),
                    DropdownMenuItem(value: true, child: Text('Zaključani')),
                  ],
                  onChanged: (v) {
                    setState(() => _filterLocked = v);
                    _loadList(page: 1);
                  },
                ),
                const SizedBox(height: 8),
                Align(
                  alignment: Alignment.centerRight,
                  child: FilledButton.icon(
                    onPressed: _startNew,
                    icon: const Icon(Icons.person_add_outlined, size: 18),
                    label: const Text('Novi korisnik'),
                  ),
                ),
              ],
            ),
          ),
          if (_listError != null)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 12),
              child: Text(_listError!, style: TextStyle(color: theme.colorScheme.error)),
            ),
          Expanded(
            child: _loadingList
                ? const Center(child: CircularProgressIndicator())
                : ListView.builder(
                    itemCount: _page?.items.length ?? 0,
                    itemBuilder: (context, i) {
                      final item = _page!.items[i];
                      final selected = !_isNew && _selected?.userId == item.userId;
                      return ListTile(
                        selected: selected,
                        title: Text(item.fullName.isEmpty ? item.email : item.fullName),
                        subtitle: Text(item.email),
                        trailing: item.isLockedOut
                            ? const Icon(Icons.lock, size: 18, color: Colors.orange)
                            : null,
                        onTap: () => _selectUser(item.userId),
                      );
                    },
                  ),
          ),
          if (_page != null)
            PagedFooter(
              page: _page!.page,
              totalPages: _page!.totalPages,
              totalCount: _page!.totalCount,
              isLoading: _loadingList,
              onPageChanged: (p) => _loadList(page: p),
            ),
        ],
      ),
    );
  }

  Widget _buildDetailPanel(BuildContext context) {
    final theme = Theme.of(context);
    if (!_isNew && _selected == null) {
      return Card(
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: BorderSide(color: theme.colorScheme.outlineVariant),
        ),
        child: Center(
          child: Text(
            'Odaberite korisnika s popisa ili kreirajte novog.',
            style: theme.textTheme.bodyLarge?.copyWith(color: theme.colorScheme.onSurfaceVariant),
          ),
        ),
      );
    }

    final d = _selected;

    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: theme.colorScheme.outlineVariant),
      ),
      child: _loadingDetail
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          _isNew ? 'Novi korisnik' : d!.fullName.isEmpty ? d.email : '${d.firstName} ${d.lastName}',
                          style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w600),
                        ),
                      ),
                      if (d != null && d.isLockedOut)
                        Chip(
                          avatar: const Icon(Icons.lock, size: 16),
                          label: const Text('Zaključan'),
                        ),
                    ],
                  ),
                  if (!_isNew && d != null) ...[
                    const SizedBox(height: 4),
                    Text(
                      'Uloge: ${d.roles.join(', ')} · Organizirano: ${d.sessionsOrganizedCount} · Sudjelovanja: ${d.sessionsParticipatedCount}',
                      style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                    ),
                  ],
                  if (_detailError != null) ...[
                    const SizedBox(height: 12),
                    Text(_detailError!, style: TextStyle(color: theme.colorScheme.error)),
                  ],
                  const SizedBox(height: 16),
                  TextField(
                    controller: _emailFormCtrl,
                    readOnly: !_isNew,
                    decoration: const InputDecoration(labelText: 'E-mail', border: OutlineInputBorder()),
                    keyboardType: TextInputType.emailAddress,
                  ),
                  if (_isNew) ...[
                    const SizedBox(height: 12),
                    TextField(
                      controller: _passwordCtrl,
                      obscureText: true,
                      decoration: const InputDecoration(labelText: 'Lozinka', border: OutlineInputBorder()),
                    ),
                    const SizedBox(height: 12),
                    DropdownButtonFormField<String>(
                      isExpanded: true,
                      value: _formRole,
                      decoration: const InputDecoration(labelText: 'Uloga', border: OutlineInputBorder()),
                      items: [for (final r in _roles) DropdownMenuItem(value: r, child: Text(r))],
                      onChanged: (v) {
                        if (v != null) {
                          setState(() => _formRole = v);
                        }
                      },
                    ),
                  ],
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: _firstNameCtrl,
                          decoration: const InputDecoration(labelText: 'Ime', border: OutlineInputBorder()),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: TextField(
                          controller: _lastNameCtrl,
                          decoration: const InputDecoration(labelText: 'Prezime', border: OutlineInputBorder()),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  OutlinedButton(
                    onPressed: _pickDob,
                    child: Text(_formDob == null ? 'Datum rođenja *' : _dateFmt.format(_formDob!)),
                  ),
                  const SizedBox(height: 12),
                  DropdownButtonFormField<int?>(
                    isExpanded: true,
                    value: _formCityId,
                    decoration: const InputDecoration(labelText: 'Grad', border: OutlineInputBorder()),
                    items: [
                      const DropdownMenuItem(value: null, child: Text('—')),
                      for (final c in _cities)
                        DropdownMenuItem(
                          value: c.id,
                          child: Text(c.name, overflow: TextOverflow.ellipsis),
                        ),
                    ],
                    onChanged: (v) => setState(() => _formCityId = v),
                  ),
                  if (!_isNew) ...[
                    const SizedBox(height: 12),
                    Center(
                      child: CircleAvatar(
                        radius: 36,
                        backgroundImage: _profileUrlCtrl.text.trim().isNotEmpty
                            ? NetworkImage(_profileUrlCtrl.text.trim())
                            : null,
                        child: _profileUrlCtrl.text.trim().isEmpty
                            ? const Icon(Icons.person_outline, size: 36)
                            : null,
                      ),
                    ),
                    const SizedBox(height: 12),
                    TextField(
                      controller: _profileUrlCtrl,
                      onChanged: (_) => setState(() {}),
                      decoration: const InputDecoration(labelText: 'URL profilne slike', border: OutlineInputBorder()),
                    ),
                  ],
                  const SizedBox(height: 16),
                  Wrap(
                    spacing: 12,
                    runSpacing: 8,
                    children: [
                      FilledButton.icon(
                        onPressed: _saving ? null : _save,
                        icon: const Icon(Icons.save_outlined),
                        label: const Text('Spremi'),
                      ),
                      if (!_isNew && d != null)
                        OutlinedButton.icon(
                          onPressed: _saving ? null : _toggleLock,
                          icon: Icon(d.isLockedOut ? Icons.lock_open : Icons.lock),
                          label: Text(d.isLockedOut ? 'Otključaj' : 'Zaključaj'),
                        ),
                    ],
                  ),
                ],
              ),
            ),
    );
  }
}

