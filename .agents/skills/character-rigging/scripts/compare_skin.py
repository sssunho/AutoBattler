import argparse
import json
import math
import sys
from pathlib import Path

import bpy


def numeric_comparison(before, after, tolerance):
    if len(before) != len(after):
        return {'preserved': False, 'maximum_absolute_error': None}
    maximum = 0.0
    for left, right in zip(before, after):
        if not math.isfinite(left) or not math.isfinite(right):
            return {'preserved': False, 'maximum_absolute_error': None}
        maximum = max(maximum, abs(left - right))
    return {'preserved': maximum <= tolerance, 'maximum_absolute_error': maximum}


def flat(values):
    return [float(value) for row in values for value in row]


def skin_data(obj, armature, maximum_influences, tolerance):
    names = {group.index: group.name for group in obj.vertex_groups}
    bones = armature.data.bones
    deform = {bone.name for bone in bones if bone.use_deform}
    weights = []
    missing = []
    non_normalized = []
    too_many = []
    invalid_entries = 0
    non_bone_entries = 0
    non_deforming_entries = 0
    maximum_sum_error = 0.0
    maximum_count = 0
    for vertex in obj.data.vertices:
        current = {}
        for entry in vertex.groups:
            name = names[entry.group]
            weight = float(entry.weight)
            if not math.isfinite(weight) or weight < 0.0 or weight > 1.0 + tolerance:
                invalid_entries += 1
                continue
            if weight <= 0.0:
                continue
            current[name] = weight
            if name not in bones:
                non_bone_entries += 1
            elif name not in deform:
                non_deforming_entries += 1
        skin = {name: weight for name, weight in current.items() if name in deform}
        count = len(skin)
        maximum_count = max(maximum_count, count)
        total = sum(skin.values())
        error = abs(total - 1.0)
        maximum_sum_error = max(maximum_sum_error, error)
        if not skin:
            missing.append(vertex.index)
        if error > tolerance:
            non_normalized.append(vertex.index)
        if count > maximum_influences:
            too_many.append(vertex.index)
        weights.append(current)
    quality = {
        'deform_weights_valid': not (missing or non_normalized or too_many or invalid_entries),
        'unweighted_vertices': missing,
        'non_normalized_vertices': non_normalized,
        'too_many_influences_vertices': too_many,
        'maximum_influences': maximum_count,
        'maximum_weight_sum_error': maximum_sum_error,
        'invalid_weight_entries': invalid_entries,
        'non_bone_group_weight_entries': non_bone_entries,
        'non_deforming_bone_weight_entries': non_deforming_entries,
        'extra_groups_require_review': bool(non_bone_entries or non_deforming_entries),
    }
    return weights, quality


def snapshot(path, maximum_influences, tolerance):
    if path.suffix.lower() == '.blend':
        bpy.ops.wm.open_mainfile(filepath=str(path), load_ui=False, use_scripts=False)
    else:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=str(path), use_anim=False)
    meshes = {}
    skeletons = {}
    for obj in sorted(bpy.context.scene.objects, key=lambda item: item.name):
        if obj.type == 'ARMATURE':
            skeletons[obj.name] = {
                'transform': flat(obj.matrix_world),
                'parent_binding': (obj.parent.name if obj.parent else None, obj.parent_type, obj.parent_bone),
                'bones': {bone.name: {
                    'parent': bone.parent.name if bone.parent else None,
                    'deform': bone.use_deform,
                    'rest_matrix_and_length': flat(bone.matrix_local) + [float(bone.length)],
                } for bone in obj.data.bones},
            }
        if obj.type != 'MESH':
            continue
        data = obj.data
        modifiers = [modifier for modifier in obj.modifiers if modifier.type == 'ARMATURE']
        if len(modifiers) > 1 or any(modifier.object is None for modifier in modifiers):
            raise ValueError('Mesh requires exactly one valid Armature modifier or none: ' + obj.name)
        if any(modifier.use_bone_envelopes or not modifier.use_vertex_groups for modifier in modifiers):
            raise ValueError('Only vertex-group skinning without bone envelopes is supported: ' + obj.name)
        armature = modifiers[0].object if modifiers else None
        bindings = {
            'parent': obj.parent.name if obj.parent else None,
            'parent_type': obj.parent_type,
            'parent_bone': obj.parent_bone,
            'armature_modifiers': [{
                'object': modifier.object.name,
                'use_vertex_groups': modifier.use_vertex_groups,
                'use_bone_envelopes': modifier.use_bone_envelopes,
                'preserve_volume': modifier.use_deform_preserve_volume,
                'mask_vertex_group': modifier.vertex_group,
                'invert_vertex_group': modifier.invert_vertex_group,
                'show_viewport': modifier.show_viewport,
                'show_render': modifier.show_render,
            } for modifier in modifiers],
            'materials': [(slot.material.name if slot.material else None, slot.link) for slot in obj.material_slots],
            'material_indices': [polygon.material_index for polygon in data.polygons],
            'smooth_polygons': [polygon.use_smooth for polygon in data.polygons],
            'uv_layer_order': [layer.name for layer in data.uv_layers],
            'active_uv_index': data.uv_layers.active_index,
            'uv_render_and_clone': [(layer.active_render, layer.active_clone) for layer in data.uv_layers],
        }
        weights, quality = skin_data(obj, armature, maximum_influences, tolerance) if armature else (None, None)
        meshes[obj.name] = {
            'topology': {
                'vertex_count': len(data.vertices),
                'edges': [tuple(edge.vertices) for edge in data.edges],
                'polygons': [tuple(polygon.vertices) for polygon in data.polygons],
                'loop_vertices': [loop.vertex_index for loop in data.loops],
            },
            'positions': flat(vertex.co for vertex in data.vertices),
            'transform': flat(obj.matrix_world),
            'normals': flat(normal.vector for normal in data.corner_normals),
            'uv': {layer.name: flat(value.uv for value in layer.data) for layer in data.uv_layers},
            'bindings': bindings,
            'weights': weights,
            'quality': quality,
        }
    if not meshes or not any(mesh['weights'] is not None for mesh in meshes.values()):
        raise ValueError('No skinned mesh was found in ' + str(path))
    return {'meshes': meshes, 'skeletons': skeletons}


def compare(before, after, tolerance):
    result = {
        'mesh_names_preserved': before['meshes'].keys() == after['meshes'].keys(),
        'armature_names_preserved': before['skeletons'].keys() == after['skeletons'].keys(),
        'meshes': {},
        'armatures': {},
    }
    preserved = result['mesh_names_preserved'] and result['armature_names_preserved']
    quality_valid = True
    review_required = False
    for name, current in after['meshes'].items():
        quality = current['quality']
        if quality is not None:
            quality_valid = quality_valid and quality['deform_weights_valid']
            review_required = review_required or quality['extra_groups_require_review']
        if name not in before['meshes']:
            result['meshes'][name] = {'before_missing': True, 'after_weight_quality': quality}
            continue
        previous = before['meshes'][name]
        checks = {
            'topology_preserved': previous['topology'] == current['topology'],
            'bindings_preserved': previous['bindings'] == current['bindings'],
            'uv_layer_names_preserved': previous['uv'].keys() == current['uv'].keys(),
            'before_weight_quality': previous['quality'],
            'after_weight_quality': quality,
            'uv_layers': {},
        }
        for field in ('positions', 'transform', 'normals'):
            checks[field] = numeric_comparison(previous[field], current[field], tolerance)
            preserved = preserved and checks[field]['preserved']
        for layer in current['uv'].keys() & previous['uv'].keys():
            checks['uv_layers'][layer] = numeric_comparison(previous['uv'][layer], current['uv'][layer], tolerance)
            preserved = preserved and checks['uv_layers'][layer]['preserved']
        checks['index_correspondence_usable'] = checks['topology_preserved'] and checks['positions']['preserved']
        checks['changed_weight_vertices'] = None
        checks['changed_weight_vertex_count'] = None
        if checks['index_correspondence_usable'] and previous['weights'] is not None and current['weights'] is not None:
            changed = []
            for index, (left, right) in enumerate(zip(previous['weights'], current['weights'])):
                if any(abs(left.get(group, 0.0) - right.get(group, 0.0)) > tolerance for group in left.keys() | right.keys()):
                    changed.append(index)
            checks['changed_weight_vertices'] = changed
            checks['changed_weight_vertex_count'] = len(changed)
        preserved = preserved and checks['topology_preserved'] and checks['bindings_preserved'] and checks['uv_layer_names_preserved']
        result['meshes'][name] = checks
    for name in before['skeletons'].keys() & after['skeletons'].keys():
        previous = before['skeletons'][name]
        current = after['skeletons'][name]
        checks = {
            'transform': numeric_comparison(previous['transform'], current['transform'], tolerance),
            'parent_binding_preserved': previous['parent_binding'] == current['parent_binding'],
            'bone_names_preserved': previous['bones'].keys() == current['bones'].keys(),
            'changed_bones': [],
            'maximum_rest_error': 0.0,
        }
        for bone in previous['bones'].keys() & current['bones'].keys():
            left = previous['bones'][bone]
            right = current['bones'][bone]
            rest = numeric_comparison(left['rest_matrix_and_length'], right['rest_matrix_and_length'], tolerance)
            if not rest['preserved'] or left['parent'] != right['parent'] or left['deform'] != right['deform']:
                checks['changed_bones'].append(bone)
            if rest['maximum_absolute_error'] is not None:
                checks['maximum_rest_error'] = max(checks['maximum_rest_error'], rest['maximum_absolute_error'])
        preserved = preserved and checks['transform']['preserved'] and checks['parent_binding_preserved'] and checks['bone_names_preserved'] and not checks['changed_bones']
        result['armatures'][name] = checks
    result['checked_data_preserved'] = bool(preserved)
    result['after_deform_weights_valid'] = quality_valid
    result['extra_weight_groups_require_review'] = review_required
    result['ok'] = bool(preserved and quality_valid and not review_required)
    return result


def main():
    if not bpy.app.background:
        raise RuntimeError('Use a separate background Blender process; live Blender is not supported.')
    parser = argparse.ArgumentParser()
    parser.add_argument('--before', required=True, type=Path)
    parser.add_argument('--after', required=True, type=Path)
    parser.add_argument('--max-influences', type=int, default=4)
    parser.add_argument('--tolerance', type=float, default=1e-6)
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
    before_path = args.before.resolve(strict=True)
    after_path = args.after.resolve(strict=True)
    if before_path.suffix.lower() not in ('.blend', '.fbx') or before_path.suffix.lower() != after_path.suffix.lower():
        raise ValueError('Use two files of the same format: .blend or .fbx.')
    if args.max_influences < 1 or not math.isfinite(args.tolerance) or args.tolerance <= 0:
        raise ValueError('Maximum influences and tolerance must be positive and finite.')
    before = snapshot(before_path, args.max_influences, args.tolerance)
    after = snapshot(after_path, args.max_influences, args.tolerance)
    report = compare(before, after, args.tolerance)
    report.update({
        'report_type': 'character-rigging-skin-comparison',
        'before': str(before_path),
        'after': str(after_path),
        'tolerance': args.tolerance,
        'maximum_influences_limit': args.max_influences,
        'assumptions': ['Same object names, file format and vertex ordering.', 'Vertex-group skinning without bone envelopes; extra groups require manual review.', 'Base mesh and bone rest data; no evaluated modifiers or pose deformation.'],
        'not_checked': ['Shader and texture contents', 'Animation curves and constraints', 'Shape keys and non-Armature modifiers', 'Vertex color and custom attributes', 'Visual deformation quality', 'Unity import settings, GUIDs, Avatar validity and clip playback'],
    })
    print(json.dumps(report, ensure_ascii=False, allow_nan=False), flush=True)
    if not report['ok']:
        raise RuntimeError('Skin comparison found changed protected data, invalid weights or extra groups requiring review.')


if __name__ == '__main__':
    main()
