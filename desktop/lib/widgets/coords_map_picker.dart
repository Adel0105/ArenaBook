import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

class CoordsMapPickerDialog extends StatefulWidget {
  const CoordsMapPickerDialog({
    super.key,
    this.initialLat,
    this.initialLng,
  });

  final double? initialLat;
  final double? initialLng;

  static Future<LatLng?> show(
    BuildContext context, {
    double? initialLat,
    double? initialLng,
  }) {
    return showDialog<LatLng>(
      context: context,
      builder: (_) => CoordsMapPickerDialog(initialLat: initialLat, initialLng: initialLng),
    );
  }

  @override
  State<CoordsMapPickerDialog> createState() => _CoordsMapPickerDialogState();
}

class _CoordsMapPickerDialogState extends State<CoordsMapPickerDialog> {
  late LatLng _point;

  @override
  void initState() {
    super.initState();
    _point = LatLng(widget.initialLat ?? 43.8563, widget.initialLng ?? 18.4131);
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Odabir lokacije na karti'),
      content: SizedBox(
        width: 520,
        height: 360,
        child: Column(
          children: [
            Expanded(
              child: FlutterMap(
                options: MapOptions(
                  initialCenter: _point,
                  initialZoom: 12,
                  onTap: (_, point) => setState(() => _point = point),
                ),
                children: [
                  TileLayer(
                    urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                    userAgentPackageName: 'arena_book_desktop',
                  ),
                  MarkerLayer(
                    markers: [
                      Marker(
                        point: _point,
                        width: 40,
                        height: 40,
                        child: const Icon(Icons.location_on, color: Colors.red, size: 36),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 8),
            Text(
              'Širina: ${_point.latitude.toStringAsFixed(5)} · Dužina: ${_point.longitude.toStringAsFixed(5)}',
              style: Theme.of(context).textTheme.bodySmall,
            ),
          ],
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Odustani')),
        FilledButton(onPressed: () => Navigator.pop(context, _point), child: const Text('Primijeni')),
      ],
    );
  }
}
