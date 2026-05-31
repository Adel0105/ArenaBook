import 'package:arena_book_desktop/models/paged_list.dart';
import 'package:arena_book_desktop/models/hall_models.dart';
import 'package:arena_book_desktop/models/reference_models.dart';
import 'package:arena_book_desktop/services/api_error.dart';
import 'package:arena_book_desktop/services/arena_book_api.dart';
import 'package:flutter/material.dart';

class HallAdminMediaPanel extends StatefulWidget {
  const HallAdminMediaPanel({super.key, required this.api, required this.hallId});

  final ArenaBookApi api;
  final int hallId;

  @override
  State<HallAdminMediaPanel> createState() => _HallAdminMediaPanelState();
}

class _HallAdminMediaPanelState extends State<HallAdminMediaPanel> {
  List<HallPhotoItem> _photos = [];
  List<HallEquipmentLink> _equipment = [];
  List<NamedReferenceItem> _equipmentTypes = [];
  bool _loading = true;
  String? _error;

  final _photoUrlCtrl = TextEditingController();
  final _photoSortCtrl = TextEditingController(text: '1');
  final _equipQtyCtrl = TextEditingController(text: '1');
  int? _equipTypeId;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _photoUrlCtrl.dispose();
    _photoSortCtrl.dispose();
    _equipQtyCtrl.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final results = await Future.wait([
        widget.api.hallPhotos(widget.hallId, pageSize: 100),
        widget.api.hallEquipment(widget.hallId, pageSize: 100),
        widget.api.equipmentTypes(pageSize: 200),
      ]);
      final photosPage = results[0] as PagedList<HallPhotoItem>;
      final equipmentPage = results[1] as PagedList<HallEquipmentLink>;
      final typesPage = results[2] as PagedList<NamedReferenceItem>;
      if (!mounted) return;
      setState(() {
        _photos = photosPage.items;
        _equipment = equipmentPage.items;
        _equipmentTypes = typesPage.items;
        _equipTypeId ??= _equipmentTypes.isNotEmpty ? _equipmentTypes.first.id : null;
        _loading = false;
      });
    } on ApiError catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.message;
        _loading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _error = 'Učitavanje medija nije uspjelo.';
        _loading = false;
      });
    }
  }

  Future<void> _addPhoto() async {
    final url = _photoUrlCtrl.text.trim();
    if (url.isEmpty) return;
    final sort = int.tryParse(_photoSortCtrl.text.trim()) ?? 1;
    try {
      await widget.api.createHallPhoto(widget.hallId, imageUrl: url, sortOrder: sort);
      _photoUrlCtrl.clear();
      await _load();
    } on ApiError catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
      }
    }
  }

  Future<void> _removePhoto(HallPhotoItem photo) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Ukloni fotografiju'),
        content: const Text('Jeste li sigurni?'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Odustani')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Ukloni')),
        ],
      ),
    );
    if (ok != true) return;
    await widget.api.deleteHallPhoto(widget.hallId, photo.id);
    await _load();
  }

  Future<void> _addEquipment() async {
    final typeId = _equipTypeId;
    if (typeId == null) return;
    final qty = int.tryParse(_equipQtyCtrl.text.trim()) ?? 1;
    try {
      await widget.api.createHallEquipment(widget.hallId, equipmentTypeId: typeId, quantity: qty);
      await _load();
    } on ApiError catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.message)));
      }
    }
  }

  Future<void> _removeEquipment(HallEquipmentLink link) async {
    await widget.api.deleteHallEquipment(widget.hallId, link.id);
    await _load();
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 16),
        child: Center(child: CircularProgressIndicator()),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (_error != null) Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
        Text('Fotografije', style: Theme.of(context).textTheme.titleMedium),
        const SizedBox(height: 8),
        if (_photos.isEmpty) const Text('Nema fotografija.'),
        ..._photos.map(
          (p) => ListTile(
            contentPadding: EdgeInsets.zero,
            leading: ClipRRect(
              borderRadius: BorderRadius.circular(8),
              child: Image.network(
                p.imageUrl,
                width: 56,
                height: 56,
                fit: BoxFit.cover,
                errorBuilder: (_, __, ___) => const Icon(Icons.broken_image_outlined),
              ),
            ),
            title: Text(p.imageUrl, maxLines: 1, overflow: TextOverflow.ellipsis),
            subtitle: Text('Redoslijed: ${p.sortOrder}'),
            trailing: IconButton(
              icon: const Icon(Icons.delete_outline),
              onPressed: () => _removePhoto(p),
            ),
          ),
        ),
        Row(
          children: [
            Expanded(
              flex: 3,
              child: TextField(
                controller: _photoUrlCtrl,
                decoration: const InputDecoration(
                  labelText: 'URL slike',
                  border: OutlineInputBorder(),
                  isDense: true,
                ),
              ),
            ),
            const SizedBox(width: 8),
            SizedBox(
              width: 72,
              child: TextField(
                controller: _photoSortCtrl,
                decoration: const InputDecoration(labelText: 'Red', border: OutlineInputBorder(), isDense: true),
                keyboardType: TextInputType.number,
              ),
            ),
            const SizedBox(width: 8),
            FilledButton(onPressed: _addPhoto, child: const Text('Dodaj')),
          ],
        ),
        const SizedBox(height: 20),
        Text('Oprema', style: Theme.of(context).textTheme.titleMedium),
        const SizedBox(height: 8),
        if (_equipment.isEmpty) const Text('Nema dodane opreme.'),
        ..._equipment.map(
          (e) => ListTile(
            contentPadding: EdgeInsets.zero,
            leading: const Icon(Icons.fitness_center_outlined),
            title: Text(e.equipmentTypeName),
            subtitle: Text('Količina: ${e.quantity}'),
            trailing: IconButton(
              icon: const Icon(Icons.delete_outline),
              onPressed: () => _removeEquipment(e),
            ),
          ),
        ),
        Row(
          children: [
            Expanded(
              child: DropdownButtonFormField<int>(
                value: _equipTypeId,
                decoration: const InputDecoration(labelText: 'Tip opreme', border: OutlineInputBorder(), isDense: true),
                items: [
                  for (final t in _equipmentTypes)
                    DropdownMenuItem(value: t.id, child: Text(t.name)),
                ],
                onChanged: (v) => setState(() => _equipTypeId = v),
              ),
            ),
            const SizedBox(width: 8),
            SizedBox(
              width: 72,
              child: TextField(
                controller: _equipQtyCtrl,
                decoration: const InputDecoration(labelText: 'Kom', border: OutlineInputBorder(), isDense: true),
                keyboardType: TextInputType.number,
              ),
            ),
            const SizedBox(width: 8),
            FilledButton(onPressed: _addEquipment, child: const Text('Dodaj')),
          ],
        ),
      ],
    );
  }
}
