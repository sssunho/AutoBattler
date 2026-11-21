import argparse
import hashlib
import io
import json
import re
import stat
import struct
import sys
import uuid
import zipfile
from pathlib import Path, PurePosixPath

from PIL import Image


class PropImportError(Exception):
    pass


def require(condition, message):
    if not condition:
        raise PropImportError(message)


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def replace_once(pattern, replacement, text, label, flags=0):
    result, count = re.subn(pattern, replacement, text, flags=flags)
    require(count == 1, f"Unsupported template layout: {label} ({count} matches)")
    return result


def guid_of(text):
    found = re.findall(r"(?m)^guid: ([0-9a-f]{32})$", text)
    require(len(found) == 1, "Template must define exactly one GUID")
    return found[0]


def asset_name(archive, supplied):
    if supplied is not None:
        result = supplied
    else:
        stem = re.sub(r"[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}", " ", archive.stem)
        stem = re.sub(r"\b[0-9a-fA-F]{12,}\b", " ", stem)
        words = re.findall(r"[A-Za-z][A-Za-z0-9]*", stem)
        noise = {"tripo", "convert", "image", "mesh", "meshy", "model", "models", "fbx", "zip", "3d", "export", "download", "generated", "ai", "id"}
        words = [word for word in words if word.lower() not in noise and word.lower() != "d"]
        result = "".join(word[:1].upper() + word[1:] for word in words)
    require(bool(re.fullmatch(r"[A-Z][A-Za-z0-9]*", result or "")), "A meaningful PascalCase name is required; supply --name, for example OakBarrel")
    require(len(result) <= 100, "Asset name is too long")
    require(not re.fullmatch(r"CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9]", result, re.I), "Reserved Windows asset name")
    return result


def fbx_info(data):
    require(data.startswith(b"Kaydara FBX Binary  \x00\x1a\x00"), "Only binary FBX is supported")
    require(len(data) >= 27, "Truncated FBX header")
    version = struct.unpack_from("<I", data, 23)[0]
    require(version in (7400, 7500), f"Unsupported FBX version: {version}; use Unity to inspect this model")
    header = "<IIIB" if version == 7400 else "<QQQB"
    header_size = struct.calcsize(header)
    scalar = {"Y": "h", "C": "?", "I": "i", "F": "f", "D": "d", "L": "q"}
    array_sizes = {"f": 4, "d": 8, "l": 8, "i": 4, "b": 1, "c": 1}

    def properties(position, end, count):
        values = []
        for _ in range(count):
            require(position < end, "Truncated FBX property")
            kind = chr(data[position])
            position += 1
            if kind in scalar:
                fmt = "<" + scalar[kind]
                size = struct.calcsize(fmt)
                require(position + size <= end, "Truncated FBX scalar")
                value = struct.unpack_from(fmt, data, position)[0]
                position += size
            elif kind in ("S", "R"):
                require(position + 4 <= end, "Truncated FBX string")
                size = struct.unpack_from("<I", data, position)[0]
                position += 4
                require(position + size <= end, "Truncated FBX string payload")
                value = data[position:position + size].decode("utf-8", errors="strict") if kind == "S" else None
                position += size
            elif kind in array_sizes:
                require(position + 12 <= end, "Truncated FBX array")
                length, encoding, size = struct.unpack_from("<III", data, position)
                position += 12
                require(encoding in (0, 1) and position + size <= end, "Invalid FBX array")
                require(encoding != 0 or size == length * array_sizes[kind], "Invalid uncompressed FBX array size")
                position += size
                value = None
            else:
                raise PropImportError(f"Unsupported FBX property type: {kind}")
            values.append(value)
        require(position == end, "Invalid FBX property list length")
        return values

    objects = []
    connections = []

    def nodes(position, limit, parent="", depth=0):
        require(depth <= 64, "FBX hierarchy is too deep")
        while position + header_size <= limit:
            end, count, property_size, name_size = struct.unpack_from(header, data, position)
            if end == 0:
                require(data[position:position + header_size] == bytes(header_size), "Invalid FBX terminator")
                return position + header_size
            start = position + header_size
            require(start + name_size + property_size <= end <= limit, "Invalid FBX node bounds")
            node_name = data[start:start + name_size].decode("ascii")
            start += name_size
            props = properties(start, start + property_size, count)
            if parent == "Objects":
                objects.append((node_name, props))
            if parent == "Connections" and node_name == "C":
                connections.append(props)
            child_start = start + property_size
            if child_start < end:
                require(nodes(child_start, end, node_name, depth + 1) == end, "Invalid FBX child bounds")
            position = end
        require(position == limit, "Truncated FBX node header")
        return position

    nodes(27, len(data))
    materials = [props for kind, props in objects if kind == "Material"]
    require(len(materials) == 1, f"Expected one material, found {len(materials)}; inspect and map multiple materials manually")
    meshes = [props for kind, props in objects if kind == "Geometry" and len(props) >= 3 and props[2] == "Mesh"]
    require(bool(meshes), "FBX contains no mesh geometry")
    require(not any(kind.startswith("Animation") or kind in ("Deformer", "Pose") for kind, props in objects), "Animated or skinned FBX is outside this static-prop importer")
    mat = materials[0]
    require(len(mat) >= 2 and isinstance(mat[1], str), "Invalid FBX material object")
    name = mat[1].split("\x00", 1)[0].removeprefix("Material::")
    require(bool(name) and not any(ord(char) < 32 for char in name), "Invalid FBX material name")
    model_ids = {props[0] for kind, props in objects if kind == "Model" and props}
    require(any(len(c) >= 3 and c[0] == "OO" and c[1] == mat[0] and c[2] in model_ids for c in connections), "FBX material is not connected to a model")
    return {"version": version, "material": name, "mesh_count": len(meshes)}


def archive_contents(archive, base_color):
    require(archive.is_file(), f"Archive not found: {archive}")
    archive_hash = sha256(archive.read_bytes())
    with zipfile.ZipFile(archive) as zipped:
        members = []
        seen = set()
        for item in zipped.infolist():
            normalized = item.filename.replace("\\", "/")
            parts = PurePosixPath(normalized).parts
            require(bool(parts) and not normalized.startswith("/") and ".." not in parts and ":" not in normalized, f"Unsafe ZIP path: {item.filename}")
            require(not stat.S_ISLNK(item.external_attr >> 16), f"ZIP symlinks are unsupported: {item.filename}")
            require(normalized.casefold() not in seen, f"Duplicate ZIP path: {item.filename}")
            seen.add(normalized.casefold())
            if not item.is_dir():
                members.append(item.filename)
        models = [name for name in members if name.lower().endswith(".fbx")]
        require(len(models) == 1, f"Expected one FBX in ZIP, found {len(models)}")
        images = [name for name in members if Path(name).suffix.lower() in (".jpg", ".jpeg", ".png")]
        if base_color:
            require(base_color in images, "--base-color must exactly name a JPG/PNG member inside the ZIP")
            selected = base_color
        else:
            excluded = r"normal|roughness|metallic|metalness|occlusion|height|displacement|thumbnail|preview"
            candidates = [name for name in images if not re.search(excluded, Path(name).stem, re.I)]
            labeled = [name for name in candidates if re.search(r"base[ _-]?colou?r|albedo|diffuse", Path(name).stem, re.I)]
            if len(labeled) == 1:
                selected = labeled[0]
            else:
                require(len(candidates) == 1, "Color texture is ambiguous or missing; inspect ZIP and supply --base-color")
                selected = candidates[0]
        model_info = zipped.getinfo(models[0])
        image_info = zipped.getinfo(selected)
        require(model_info.file_size <= 512 * 1024 * 1024 and image_info.file_size <= 128 * 1024 * 1024, "Selected asset is too large for this lightweight importer")
        model_data = zipped.read(models[0])
        image_data = zipped.read(selected)
    with Image.open(io.BytesIO(image_data)) as image:
        require(image.format in ("JPEG", "PNG"), "Selected texture is not JPEG/PNG")
        expected = "PNG" if Path(selected).suffix.lower() == ".png" else "JPEG"
        require(image.format == expected, "Texture extension does not match image encoding")
        image_size = image.size
        image.verify()
    with Image.open(io.BytesIO(image_data)) as image:
        image.load()
    return {"model": models[0], "base_color": selected, "model_bytes": model_data, "texture_bytes": image_data, "texture_size": image_size, "archive_sha256": archive_hash, "ignored_members": [name for name in members if name not in (models[0], selected)], **fbx_info(model_data)}


def prepare(args):
    root = Path(args.project_root).resolve()
    assets = (root / "Assets").resolve()
    require(assets.is_dir() and (root / "ProjectSettings").is_dir(), "--project-root must contain Assets and ProjectSettings")
    destination_arg = Path(args.destination)
    destination = (destination_arg if destination_arg.is_absolute() else root / destination_arg).resolve()
    require(destination != assets and destination.is_relative_to(assets), "Destination must be a folder inside this project's Assets")
    require(not destination.exists() or destination.is_dir(), "Destination is not a folder")
    archive = Path(args.archive).resolve()
    name = asset_name(archive, args.name)
    source = archive_contents(archive, args.base_color)
    template_root = assets / "Art/BG/Props"
    template_paths = {
        "model": template_root / "OakBarrel.fbx.meta",
        "texture": template_root / "Textures/T_OakBarrel_BaseColor.jpg.meta",
        "material": template_root / "Materials/M_OakBarrel.mat",
        "material_meta": template_root / "Materials/M_OakBarrel.mat.meta",
        "shader_meta": assets / "Art/PainterlyEnvironment/Character/PaintedCharacter.shader.meta",
        "shader": assets / "Art/PainterlyEnvironment/Character/PaintedCharacter.shader",
    }
    for path in template_paths.values():
        require(path.is_file() and path.resolve().is_relative_to(assets), f"Required project template missing or outside Assets: {path}")
    templates = {key:path.read_text(encoding="utf-8-sig") for key,path in template_paths.items()}
    require('Shader "RaggedBanner/Painted Character"' in templates["shader"], "Painted Character shader name changed")
    shader_guid = guid_of(templates["shader_meta"])
    require(re.search(r"m_Shader: \{fileID: 4800000, guid: " + shader_guid + r", type: 3\}", templates["material"]), "Material template does not reference the current Painted Character shader")
    ids = {key:uuid.uuid4().hex for key in ("model", "texture", "material")}
    model_meta = replace_once(r"(?m)^guid: [0-9a-f]{32}$", "guid: " + ids["model"], templates["model"], "model GUID")
    remap = '  externalObjects:\n  - first:\n      type: UnityEngine:Material\n      assembly: UnityEngine.CoreModule\n      name: ' + json.dumps(source["material"], ensure_ascii=False) + '\n    second: {fileID: 2100000, guid: ' + ids["material"] + ', type: 2}\n'
    model_meta = replace_once(r"  externalObjects:\n.*?(?=  materials:)", lambda match:remap, model_meta, "material remap", re.S)
    for key in ("importAnimation", "animationType", "importBlendShapes", "importPhysicalCameras", "importCameras", "importLights"):
        model_meta = replace_once(r"(?m)^(\s*" + key + r":) \d+$", lambda match:match[1] + " 0", model_meta, key)
    texture_meta = replace_once(r"(?m)^guid: [0-9a-f]{32}$", "guid: " + ids["texture"], templates["texture"], "texture GUID")
    require("    sRGBTexture: 1\n" in texture_meta and "  textureType: 0\n" in texture_meta, "Texture template must be an sRGB color texture")
    mat = replace_once(r"(?m)^  m_Name: .*$", "  m_Name: M_" + name, templates["material"], "material name")
    mat = replace_once(r"(    - _BaseMap:\n        m_Texture: )\{[^\n]+\}", lambda match:match[1] + "{fileID: 2800000, guid: " + ids["texture"] + ", type: 3}", mat, "BaseMap")
    mat = replace_once(r"(    - _BumpMap:\n        m_Texture: )\{[^\n]+\}", lambda match:match[1] + "{fileID: 0}", mat, "BumpMap")
    mat = replace_once(r"(?m)^    - _BaseColor: \{[^\n]+\}$", "    - _BaseColor: {r: 1, g: 1, b: 1, a: 1}", mat, "BaseColor")
    for key in ("_EnableNormalMap", "_BumpScale"):
        mat = replace_once(r"(?m)^    - " + key + r": [^\n]+$", "    - " + key + ": 0", mat, key)
    require("  m_ValidKeywords: []\n" in mat and "  m_InvalidKeywords: []\n" in mat, "Material template has shader keywords; review before importing")
    mat_meta = replace_once(r"(?m)^guid: [0-9a-f]{32}$", "guid: " + ids["material"], templates["material_meta"], "material GUID")
    model_path = destination / (name + ".fbx")
    texture_path = destination / "Textures" / ("T_" + name + "_BaseColor" + Path(source["base_color"]).suffix.lower())
    material_path = destination / "Materials" / ("M_" + name + ".mat")
    payloads = {}
    folders = set()
    for path in (model_path, texture_path, material_path):
        folder = path.parent
        while folder != assets:
            require(folder.is_relative_to(assets), "Output folder escaped Assets")
            require(not folder.exists() or folder.is_dir(), f"Output parent is not a directory: {folder}")
            folders.add(folder)
            folder = folder.parent
    folders = sorted(folders, key=lambda path:(len(path.parts), str(path)))
    for folder in folders:
        meta = Path(str(folder) + ".meta")
        if not meta.exists():
            payloads[meta] = ("fileFormatVersion: 2\nguid: " + uuid.uuid4().hex + "\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n").encode("utf-8")
        else:
            require(meta.is_file() and meta.resolve().is_relative_to(assets), f"Invalid folder metadata path: {meta}")
            existing_meta = meta.read_text(encoding="utf-8-sig")
            guid_of(existing_meta)
            require("folderAsset: yes" in existing_meta, f"Existing folder metadata is not a folder asset: {meta}")
    for path, meta_text, content in ((texture_path, texture_meta, source["texture_bytes"]), (material_path, mat_meta, mat.encode("utf-8")), (model_path, model_meta, source["model_bytes"])):
        payloads[Path(str(path) + ".meta")] = meta_text.encode("utf-8")
        payloads[path] = content
    conflicts = []
    for path in payloads:
        require(path.resolve().is_relative_to(assets), f"Output path escaped Assets: {path}")
        if path.exists() or path.is_symlink():
            conflicts.append(str(path.relative_to(root)))
    warnings = []
    if source["ignored_members"]:
        warnings.append("Only the selected FBX and base-color texture will be copied; other ZIP members are ignored.")
    if max(source["texture_size"]) > 2048:
        warnings.append("Original texture bytes are preserved; the project template currently imports textures at up to 2048 pixels.")
    plan = {"status":"plan_only", "archive":str(archive), "name":name, "destination":str(destination.relative_to(root)), "source_model":source["model"], "source_base_color":source["base_color"], "material_slot":source["material"], "fbx_version":source["version"], "mesh_count":source["mesh_count"], "texture_size":source["texture_size"], "shader":"RaggedBanner/Painted Character", "output_paths":[str(path.relative_to(root)) for path in payloads], "conflicts":conflicts, "warnings":warnings}
    return root, assets, archive, source, folders, payloads, ids, plan


def apply(prepared):
    root, assets, archive, source, folders, payloads, ids, plan = prepared
    require(not plan["conflicts"], "Output already exists; nothing overwritten: " + ", ".join(plan["conflicts"]))
    require(sha256(archive.read_bytes()) == source["archive_sha256"], "Source ZIP changed during inspection")
    created_files = []
    created_folders = []
    try:
        for folder in folders:
            require(folder.resolve().is_relative_to(assets), f"Output folder escaped Assets: {folder}")
            if not folder.exists():
                folder.mkdir()
                created_folders.append(folder)
        for path, content in payloads.items():
            require(path.resolve().is_relative_to(assets), f"Output path escaped Assets: {path}")
            with path.open("xb") as output:
                created_files.append(path)
                output.write(content)
        for path, content in payloads.items():
            require(path.read_bytes() == content, f"Saved content changed: {path}")
        require(sha256(archive.read_bytes()) == source["archive_sha256"], "Source ZIP changed while saving")
    except BaseException:
        for path in reversed(created_files):
            try:
                content = path.read_bytes()
                if payloads[path].startswith(content):
                    path.unlink()
            except OSError:
                pass
        for folder in reversed(created_folders):
            try:
                folder.rmdir()
            except OSError:
                pass
        raise
    plan.update({"status":"files_saved_unity_import_unverified", "guids":ids, "verification":{"saved_bytes_match":True, "material_and_texture_references_match":True, "archive_sha256":source["archive_sha256"], "model_sha256":sha256(source["model_bytes"]), "texture_sha256":sha256(source["texture_bytes"])}, "unity_import_verified":False})
    return plan


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description="Inspect or import a single static FBX prop ZIP using RaggedBanner's Painted Character material.")
    parser.add_argument("archive")
    parser.add_argument("--project-root", default=".")
    parser.add_argument("--destination", default="Assets/Art/BG/Props")
    parser.add_argument("--name")
    parser.add_argument("--base-color")
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    try:
        prepared = prepare(args)
        result = apply(prepared) if args.apply else prepared[-1]
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0
    except (PropImportError, OSError, ValueError, UnicodeError, struct.error, zipfile.BadZipFile, Image.DecompressionBombError) as error:
        print(json.dumps({"status":"error", "message":str(error)}, ensure_ascii=False), file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
