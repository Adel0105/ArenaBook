import 'package:arena_book_desktop/models/hall_models.dart';
import 'package:arena_book_desktop/models/paged_list.dart';
import 'package:arena_book_desktop/models/session_models.dart';
import 'package:arena_book_desktop/services/api_error.dart';
import 'package:arena_book_desktop/services/arena_book_api.dart';
import 'package:arena_book_desktop/widgets/coords_map_picker.dart';
import 'package:arena_book_desktop/widgets/hall_admin_media_panel.dart';
import 'package:arena_book_desktop/widgets/paged_footer.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

class HallsScreen extends StatefulWidget {
  const HallsScreen({super.key, required this.api});

  final ArenaBookApi api;

  @override
  State<HallsScreen> createState() => _HallsScreenState();
}

class _HallsScreenState extends State<HallsScreen> {
  final _searchCtrl = TextEditingController();
  PagedList<HallListItem>? _page;
  List<CityItem> _cities = [];
  int _listPage = 1;
  int? _filterCityId;
  bool? _filterActive;
  bool _loadingList = true;
  String? _listError;

  HallDetails? _selected;
  bool _isNew = false;
  bool _loadingDetail = false;
  bool _saving = false;
  String? _detailError;

  final _nameCtrl = TextEditingController();
  final _streetCtrl = TextEditingController();
  final _latCtrl = TextEditingController();
  final _lngCtrl = TextEditingController();
  final _capacityCtrl = TextEditingController();
  final _priceCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  int? _formCityId;
  bool _formActive = true;

  @override
  void initState() {
    super.initState();
    _bootstrap();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    _nameCtrl.dispose();
    _streetCtrl.dispose();
    _latCtrl.dispose();
    _lngCtrl.dispose();
    _capacityCtrl.dispose();
    _priceCtrl.dispose();
    _phoneCtrl.dispose();
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
      final res = await widget.api.halls(
        page: p,
        pageSize: 15,
        q: _searchCtrl.text.trim().isEmpty ? null : _searchCtrl.text.trim(),
        cityId: _filterCityId,
        isActive: _filterActive,
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
        _listError = 'Greška pri učitavanju dvorana.';
        _loadingList = false;
      });
    }
  }

  void _startNew() {
    setState(() {
      _isNew = true;
      _selected = null;
      _detailError = null;
      _nameCtrl.clear();
      _streetCtrl.clear();
      _latCtrl.clear();
      _lngCtrl.clear();
      _capacityCtrl.text = '10';
      _priceCtrl.text = '50';
      _phoneCtrl.clear();
      _formCityId = _cities.isNotEmpty ? _cities.first.id : null;
      _formActive = true;
    });
  }

  Future<void> _selectHall(int id) async {
    setState(() {
      _isNew = false;
      _loadingDetail = true;
      _detailError = null;
    });
    try {
      final d = await widget.api.hallById(id);
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
        _detailError = 'Nije moguće učitati dvoranu.';
        _loadingDetail = false;
      });
    }
  }

  void _fillForm(HallDetails d) {
    _nameCtrl.text = d.name;
    _streetCtrl.text = d.streetAddress;
    _latCtrl.text = d.latitude?.toString() ?? '';
    _lngCtrl.text = d.longitude?.toString() ?? '';
    _capacityCtrl.text = '${d.capacityPeople}';
    _priceCtrl.text = d.pricePerHourCoins.toString();
    _phoneCtrl.text = d.contactPhone;
    _formCityId = d.cityId;
    _formActive = d.isActive;
  }

  Map<String, dynamic> _buildBody() {
    double? parseOpt(String s) {
      final t = s.trim();
      if (t.isEmpty) {
        return null;
      }
      return double.tryParse(t.replaceAll(',', '.'));
    }

    return {
      'name': _nameCtrl.text.trim(),
      'cityId': _formCityId,
      'streetAddress': _streetCtrl.text.trim(),
      'latitude': parseOpt(_latCtrl.text),
      'longitude': parseOpt(_lngCtrl.text),
      'capacityPeople': int.tryParse(_capacityCtrl.text.trim()) ?? 0,
      'pricePerHourCoins': double.tryParse(_priceCtrl.text.trim().replaceAll(',', '.')) ?? 0,
      'contactPhone': _phoneCtrl.text.trim(),
      'isActive': _formActive,
    };
  }

  Future<void> _save() async {
    if (_formCityId == null) {
      setState(() => _detailError = 'Odaberite grad.');
      return;
    }
    setState(() {
      _saving = true;
      _detailError = null;
    });
    try {
      final body = _buildBody();
      if (_isNew) {
        final created = await widget.api.createHall(body);
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
        final updated = await widget.api.updateHall(_selected!.id, body);
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
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Dvorana spremljena.')));
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

  Future<void> _delete() async {
    final id = _selected?.id;
    if (id == null) {
      return;
    }
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Brisanje dvorane'),
        content: const Text('Jeste li sigurni da želite obrisati ovu dvoranu?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Odustani')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Obriši')),
        ],
      ),
    );
    if (ok != true) {
      return;
    }
    setState(() => _saving = true);
    try {
      await widget.api.deleteHall(id);
      if (!mounted) {
        return;
      }
      setState(() {
        _selected = null;
        _isNew = false;
        _saving = false;
      });
      await _loadList(page: 1);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Dvorana obrisana.')));
      }
    } on ApiError catch (e) {
      if (!mounted) {
        return;
      }
      setState(() {
        _detailError = e.message;
        _saving = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SizedBox(
            width: 380,
            child: _buildListPanel(context),
          ),
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
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              children: [
                TextField(
                  controller: _searchCtrl,
                  decoration: InputDecoration(
                    hintText: 'Pretraži dvorane…',
                    prefixIcon: const Icon(Icons.search),
                    suffixIcon: IconButton(
                      icon: const Icon(Icons.clear),
                      onPressed: () {
                        _searchCtrl.clear();
                        _loadList(page: 1);
                      },
                    ),
                    isDense: true,
                    border: const OutlineInputBorder(),
                  ),
                  onSubmitted: (_) => _loadList(page: 1),
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Expanded(
                      child: DropdownButtonFormField<int?>(
                        isExpanded: true,
                        value: _filterCityId,
                        decoration: const InputDecoration(labelText: 'Grad', isDense: true, border: OutlineInputBorder()),
                        items: [
                          const DropdownMenuItem(value: null, child: Text('Svi gradovi')),
                          for (final c in _cities)
                            DropdownMenuItem(
                              value: c.id,
                              child: Text(c.name, overflow: TextOverflow.ellipsis),
                            ),
                        ],
                        onChanged: (v) {
                          setState(() => _filterCityId = v);
                          _loadList(page: 1);
                        },
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: DropdownButtonFormField<bool?>(
                        isExpanded: true,
                        value: _filterActive,
                        decoration: const InputDecoration(labelText: 'Status', isDense: true, border: OutlineInputBorder()),
                        items: const [
                          DropdownMenuItem(value: null, child: Text('Sve')),
                          DropdownMenuItem(value: true, child: Text('Aktivne')),
                          DropdownMenuItem(value: false, child: Text('Neaktivne')),
                        ],
                        onChanged: (v) {
                          setState(() => _filterActive = v);
                          _loadList(page: 1);
                        },
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                Align(
                  alignment: Alignment.centerRight,
                  child: FilledButton.icon(
                    onPressed: _startNew,
                    icon: const Icon(Icons.add, size: 18),
                    label: const Text('Nova dvorana'),
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
                      final selected = !_isNew && _selected?.id == item.id;
                      return ListTile(
                        selected: selected,
                        leading: _hallThumb(item.primaryImageUrl),
                        title: Text(item.name, maxLines: 1, overflow: TextOverflow.ellipsis),
                        subtitle: Text('${item.cityName} · ${item.capacityPeople} mjesta'),
                        trailing: item.isActive
                            ? null
                            : Chip(
                                label: const Text('Neaktivna', style: TextStyle(fontSize: 11)),
                                visualDensity: VisualDensity.compact,
                                padding: EdgeInsets.zero,
                              ),
                        onTap: () => _selectHall(item.id),
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
            'Odaberite dvoranu s popisa ili kreirajte novu.',
            style: theme.textTheme.bodyLarge?.copyWith(color: theme.colorScheme.onSurfaceVariant),
          ),
        ),
      );
    }

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
                  Text(
                    _isNew ? 'Nova dvorana' : 'Uredi dvoranu: ${_selected!.name}',
                    style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w600),
                  ),
                  if (_detailError != null) ...[
                    const SizedBox(height: 12),
                    Text(_detailError!, style: TextStyle(color: theme.colorScheme.error)),
                  ],
                  const SizedBox(height: 16),
                  TextField(controller: _nameCtrl, decoration: const InputDecoration(labelText: 'Naziv', border: OutlineInputBorder())),
                  const SizedBox(height: 12),
                  DropdownButtonFormField<int>(
                    isExpanded: true,
                    value: _formCityId,
                    decoration: const InputDecoration(labelText: 'Grad', border: OutlineInputBorder()),
                    items: [
                      for (final c in _cities)
                        DropdownMenuItem(
                          value: c.id,
                          child: Text(c.name, overflow: TextOverflow.ellipsis),
                        ),
                    ],
                    onChanged: (v) => setState(() => _formCityId = v),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _streetCtrl,
                    decoration: const InputDecoration(labelText: 'Adresa', border: OutlineInputBorder()),
                  ),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: _latCtrl,
                          decoration: const InputDecoration(labelText: 'Geografska širina', border: OutlineInputBorder()),
                          keyboardType: const TextInputType.numberWithOptions(decimal: true),
                          inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[\d.,\-]'))],
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: TextField(
                          controller: _lngCtrl,
                          decoration: const InputDecoration(labelText: 'Geografska dužina', border: OutlineInputBorder()),
                          keyboardType: const TextInputType.numberWithOptions(decimal: true),
                          inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[\d.,\-]'))],
                        ),
                      ),
                      const SizedBox(width: 8),
                      IconButton(
                        tooltip: 'Odaberi na karti',
                        onPressed: _pickCoordsOnMap,
                        icon: const Icon(Icons.map_outlined),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: _capacityCtrl,
                          decoration: const InputDecoration(labelText: 'Kapacitet', border: OutlineInputBorder()),
                          keyboardType: TextInputType.number,
                          inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: TextField(
                          controller: _priceCtrl,
                          decoration: const InputDecoration(labelText: 'Cijena / sat (kovanice)', border: OutlineInputBorder()),
                          keyboardType: const TextInputType.numberWithOptions(decimal: true),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _phoneCtrl,
                    decoration: const InputDecoration(labelText: 'Kontakt telefon', border: OutlineInputBorder()),
                  ),
                  const SizedBox(height: 8),
                  SwitchListTile(
                    contentPadding: EdgeInsets.zero,
                    title: const Text('Aktivna dvorana'),
                    value: _formActive,
                    onChanged: (v) => setState(() => _formActive = v),
                  ),
                  const SizedBox(height: 16),
                  if (!_isNew && _selected != null) ...[
                    const Divider(),
                    HallAdminMediaPanel(api: widget.api, hallId: _selected!.id),
                    const SizedBox(height: 16),
                  ],
                  Row(
                    children: [
                      FilledButton.icon(
                        onPressed: _saving ? null : _save,
                        icon: _saving
                            ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2))
                            : const Icon(Icons.save_outlined),
                        label: const Text('Spremi'),
                      ),
                      if (!_isNew && _selected != null) ...[
                        const SizedBox(width: 12),
                        OutlinedButton.icon(
                          onPressed: _saving ? null : _delete,
                          icon: const Icon(Icons.delete_outline),
                          label: const Text('Obriši'),
                        ),
                      ],
                    ],
                  ),
                ],
              ),
            ),
    );
  }

  Widget _hallThumb(String? url) {
    if (url == null || url.isEmpty) {
      return const CircleAvatar(child: Icon(Icons.stadium_outlined, size: 18));
    }
    return ClipRRect(
      borderRadius: BorderRadius.circular(8),
      child: Image.network(
        url,
        width: 44,
        height: 44,
        fit: BoxFit.cover,
        errorBuilder: (_, __, ___) => const CircleAvatar(child: Icon(Icons.broken_image_outlined, size: 18)),
      ),
    );
  }

  Future<void> _pickCoordsOnMap() async {
    final lat = double.tryParse(_latCtrl.text.replaceAll(',', '.'));
    final lng = double.tryParse(_lngCtrl.text.replaceAll(',', '.'));
    final picked = await CoordsMapPickerDialog.show(context, initialLat: lat, initialLng: lng);
    if (picked == null || !mounted) return;
    setState(() {
      _latCtrl.text = picked.latitude.toStringAsFixed(6);
      _lngCtrl.text = picked.longitude.toStringAsFixed(6);
    });
  }
}

