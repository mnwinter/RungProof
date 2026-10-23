#!/usr/bin/env python3
"""Offline, dry-run-first incremental closeout. Never stages, pushes or uploads.

All output directories are new. The historical D receipt/archive set is read-only.
Public coverage requires exact bytes in HEAD or an extracted archive hash proof.
Python stdlib only; Windows reparse points are recorded and never traversed.
"""
import argparse
import datetime as dt
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import stat
import subprocess
import sys

SCHEMA = 1
BASE_COMMIT = "d0c4566bc956706ab3430e25d4142b03f4431e9c"
ROOT_THREAD = "01a161f0-5690-7783-a4ed-93dd4dee3195"
CAP = re.compile(rb"((?:https?|wss?)://(?:127\.0\.0\.1|localhost|\[::1\]):\d{2,5}/)([A-Za-z0-9_-]{24,})", re.I)
TOKEN = re.compile(rb"(?:bridge[_-]?token|capability[_-]?token|token)\s*[=:]\s*[\"']([A-Za-z0-9_-]{24,})[\"']", re.I)
SECRET = {
    "private_key": re.compile(rb"-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----"),
    "github_token": re.compile(rb"\b(?:gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{30,})\b"),
    "openai_key": re.compile(rb"\bsk-(?:proj-|svcacct-)?[A-Za-z0-9_-]{30,}\b"),
    "aws_access_key": re.compile(rb"\b(?:AKIA|ASIA)[A-Z0-9]{16}\b"),
    "credential_url": re.compile(rb"(?:https?|ftp)://[^\s/@:]+:[^\s/@]+@", re.I),
}
PRIVATE_NAMES = {"session-state.json", ".env", ".netrc", ".npmrc", ".pypirc", "credentials.json", "auth.json"}
CONTAINERS = {".zip", ".7z", ".gz", ".tgz", ".tar", ".nupkg", ".pfx", ".p12", ".key"}


def now():
    return dt.datetime.now(dt.timezone.utc).isoformat()


def write_json(path, obj):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    # Reports inside a newly created work/receipt directory may be finalized.
    path.write_text(json.dumps(obj, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def load(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def relative(value):
    value = str(value).replace("\\", "/")
    p = PurePosixPath(value)
    if not value or value == "." or p.is_absolute() or ".." in p.parts or ":" in value or "\n" in value or "\r" in value:
        raise ValueError("Unsafe relative path")
    for part in p.parts:
        if part.endswith((" ", ".")) or re.fullmatch(r"(?i)(?:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\..*)?", part):
            raise ValueError("Unsafe Windows path component")
    return p.as_posix()


def reparse(path):
    s = Path(path).lstat()
    return stat.S_ISLNK(s.st_mode) or bool(getattr(s, "st_file_attributes", 0) & 0x400)


def walk(root):
    """Yield only ordinary files; separately yield excluded reparse paths/errors."""
    root = Path(root)
    if not root.exists():
        yield "error", root, "MISSING_ROOT"
        return
    if reparse(root):
        yield "excluded", root, "ROOT_REPARSE_POINT"
        return
    todo = [root]
    while todo:
        directory = todo.pop()
        try:
            entries = sorted(os.scandir(directory), key=lambda x: x.name.casefold())
        except OSError as e:
            yield "error", directory, type(e).__name__
            continue
        for entry in entries:
            p = Path(entry.path)
            if entry.name == ".git":
                yield "excluded", p, "GIT_METADATA_SEPARATE_BUNDLE"
                continue
            try:
                if reparse(p):
                    yield "excluded", p, "REPARSE_NOT_TRAVERSED"
                elif entry.is_dir(follow_symlinks=False):
                    todo.append(p)
                elif entry.is_file(follow_symlinks=False):
                    yield "file", p, None
                else:
                    yield "excluded", p, "NOT_REGULAR"
            except OSError as e:
                yield "error", p, type(e).__name__


def git(repo, *args):
    # Local read-only commands, except the explicitly requested private bundle.
    return subprocess.check_output(["git", "-C", str(repo), *args])


def fingerprint(s):
    return [s.st_size, s.st_mtime_ns, s.st_ino]


def inspect_file(path, algorithm="sha1", limit=None, scan=True):
    before = Path(path).stat()
    n = before.st_size if limit is None else limit
    sha = hashlib.sha256()
    blob = hashlib.new(algorithm)
    blob.update(b"blob " + str(n).encode("ascii") + b"\0")
    kinds, capabilities = set(), set()
    remaining, tail = n, b""
    with open(path, "rb") as f:
        while remaining:
            chunk = f.read(min(4 * 1024 * 1024, remaining))
            if not chunk:
                raise RuntimeError("SOURCE_TRUNCATED")
            sha.update(chunk)
            blob.update(chunk)
            remaining -= len(chunk)
            if scan:
                candidate = tail + chunk
                # Covers ordinary UTF-8 and ASCII text stored as UTF-16LE/BE.
                for view in (candidate, candidate.replace(b"\0", b"")):
                    for key, pattern in SECRET.items():
                        if pattern.search(view):
                            kinds.add(key)
                    capabilities.update(m.group(2) for m in CAP.finditer(view))
                    capabilities.update(m.group(1) for m in TOKEN.finditer(view))
                tail = candidate[-2048:]
    after = Path(path).stat()
    if limit is None and fingerprint(before) != fingerprint(after):
        raise RuntimeError("UNSTABLE_DURING_READ")
    if limit is not None and after.st_size < limit:
        raise RuntimeError("PREFIX_TRUNCATED")
    return {"bytes": n, "sha256": sha.hexdigest(), "git_blob": blob.hexdigest(),
            "source_fingerprint": fingerprint(after), "secret_kinds": sorted(kinds),
            "capability_count": len(capabilities)}, capabilities


def new_directory(path, forbidden=()):
    p = Path(path).resolve()
    for f in forbidden:
        if p == f or f in p.parents:
            raise ValueError("Output must be outside repository/historical backup")
    p.mkdir(parents=True, exist_ok=False)
    return p


def baseline_hashes(base):
    result = {}
    # inventory-files.jsonl has NO SHA256. Never use its path/mtime as proof.
    full = base / "verified-full-sha256.jsonl"
    if full.exists():
        with full.open(encoding="utf-8-sig") as f:
            for line in f:
                row = json.loads(line)
                if row.get("tree") == "RungProof" and "sha256" in row:
                    result[relative(row["path"])] = row["sha256"]
    final = base / "verified-final-delta-sha256.json"
    if final.exists():
        for row in load(final):
            if "sha256" in row and not row["path"].startswith((".git/", "installed-skill/")):
                result[relative(row["path"])] = row["sha256"]
    if not result:
        raise RuntimeError("No historical per-file hash receipt found")
    return result


def session_headers(session_roots, root_ids):
    """Read metadata first lines only for discovery; full unrelated logs untouched."""
    metadata, errors = {}, []
    for root in session_roots:
        if not root.exists():
            continue
        for kind, p, why in walk(root):
            if kind != "file" or p.suffix != ".jsonl":
                continue
            try:
                with p.open("rb") as f:
                    first = f.readline(2 * 1024 * 1024)
                row = json.loads(first)
                if row.get("type") != "session_meta":
                    continue
                m = row["payload"]
                source = m.get("source", {})
                spawn = source.get("subagent", {}).get("thread_spawn", {}) if isinstance(source, dict) else {}
                parent = m.get("parent_thread_id") or spawn.get("parent_thread_id")
                metadata[m["id"]] = {"id": m["id"], "parent": parent, "cwd": m.get("cwd"), "path": str(p)}
            except (OSError, ValueError, KeyError) as e:
                errors.append({"path": str(p), "kind": type(e).__name__})
    chosen = set(root_ids)
    while True:
        additions = {sid for sid, m in metadata.items() if m["parent"] in chosen} - chosen
        if not additions:
            break
        chosen.update(additions)
    absent = sorted(set(root_ids) - metadata.keys())
    if absent:
        raise RuntimeError("Requested root session metadata missing: " + ", ".join(absent))
    # CWD alone is intentionally NOT inclusion authority.
    return [metadata[sid] for sid in sorted(chosen) if sid in metadata], errors


def complete_prefix(path):
    """Snapshot a live JSONL only through its last currently complete record."""
    size = path.stat().st_size
    with path.open("rb") as f:
        start = max(0, size - 2 * 1024 * 1024)
        f.seek(start)
        tail = f.read(size - start)
    last = tail.rfind(b"\n")
    if last < 0:
        raise RuntimeError("No complete trailing JSONL record within bounded window")
    return start + last + 1, size


def inventory(args):
    repo, base = Path(args.repo).resolve(), Path(args.base).resolve()
    work = new_directory(args.work, (repo, base))
    head = git(repo, "rev-parse", "HEAD").decode().strip()
    object_format = git(repo, "rev-parse", "--show-object-format").decode().strip()
    if object_format not in ("sha1", "sha256"):
        raise RuntimeError("Unsupported Git object format")
    tracked = {}
    for row in git(repo, "ls-tree", "-rz", "--full-tree", "HEAD").split(b"\0"):
        if row:
            descriptor, path = row.split(b"\t", 1)
            mode, kind, oid = descriptor.split()
            if kind == b"blob":
                tracked[path.decode("utf-8")] = oid.decode("ascii")
    historic = baseline_hashes(base)
    proof = load(args.public_proof) if args.public_proof else {"files": {}}
    if args.public_proof and proof.get("result") != "PASS":
        raise RuntimeError("Public archive proof is not PASS")
    roots = [("RungProof", repo, False)]
    for item in args.extra:
        label, source = item.split("=", 1)
        roots.append((relative(label), Path(source).resolve(), True))
    selected, header_errors = session_headers([Path(x) for x in args.session_root], args.root_thread)
    rows, exclusions, errors = [], [], []

    def record(label, source, path, private, limit=None, context=None):
        rel = path.relative_to(source).as_posix() if context is None else relative(path.name)
        try:
            info, _ = inspect_file(path, object_format, limit)
            old_hash = historic.get(rel) if label == "RungProof" else None
            # Test the actual current D file, not merely its historical pathname.
            base_file = base / "RungProof" / rel if label == "RungProof" else None
            base_matches = False
            if base_file is not None and base_file.exists() and not reparse(base_file):
                old_info, _ = inspect_file(base_file, scan=False)
                base_matches = old_info["sha256"] == info["sha256"]
            reasons = list(info["secret_kinds"])
            if private or path.name.lower() in PRIVATE_NAMES or path.name.lower().startswith(".env."):
                reasons.append("PRIVATE_ROOT_OR_FILENAME")
            if path.suffix.lower() in CONTAINERS and info["sha256"] not in args.allow_container_hash:
                reasons.append("CONTAINER_OR_KEY_REQUIRES_EXPLICIT_HASH_REVIEW")
            exact_git = label == "RungProof" and tracked.get(rel) == info["git_blob"]
            exact_archive = label == "RungProof" and info["sha256"] in proof.get("files", {}).get(rel, [])
            public = "PRIVATE_ONLY" if reasons else "GIT_EXACT" if exact_git else "HISTORICAL_ARCHIVE_EXACT" if exact_archive else "DELTA"
            row = {"tree": label, "relative": relative(rel), "source": str(path), **info,
                   "baseline_sha256": old_hash,
                   "since_historical_base": "NEW" if old_hash is None else "UNCHANGED_BYTES" if old_hash == info["sha256"] else "MODIFIED_BYTES",
                   "private_storage": "BASE_EXACT" if base_matches else "NEW_RECEIPT_DELTA",
                   "base_file": str(base_file) if base_matches else None,
                   "public": public, "public_exclusion_reasons": reasons,
                   "exact_head_blob": exact_git, "exact_historical_archive_hash": exact_archive}
            if context:
                row["session"] = context
                row["prefix_snapshot"] = True
            rows.append(row)
        except (OSError, RuntimeError, ValueError) as e:
            errors.append({"source": str(path), "error": str(e)})

    for label, source, private in roots:
        for kind, path, why in walk(source):
            if kind == "file":
                record(label, source, path, private)
            else:
                (errors if kind == "error" else exclusions).append({"source": str(path), "reason": why})
    for m in selected:
        path = Path(m["path"])
        try:
            limit, original_size = complete_prefix(path)
            record("private-codex-context", path.parent, path, True, limit,
                   {**m, "snapshot_bytes": limit, "length_at_inventory": original_size,
                    "incomplete_tail_bytes_omitted": original_size - limit})
        except (OSError, RuntimeError) as e:
            errors.append({"source": str(path), "error": str(e)})
    current_repo_paths = {r["relative"] for r in rows if r["tree"] == "RungProof"}
    manifest = {"schema": SCHEMA, "kind": "DRY_RUN_MANIFEST_ONLY", "created_utc": now(),
                "repo": str(repo), "base": str(base), "historical_base_commit": BASE_COMMIT,
                "git_head": head, "git_object_format": object_format,
                "public_proof": str(Path(args.public_proof).resolve()) if args.public_proof else None,
                "public_proof_sha256": hashlib.sha256(Path(args.public_proof).read_bytes()).hexdigest() if args.public_proof else None,
                "roots": [{"tree": x, "source": str(y), "private": z} for x, y, z in roots],
                "sessions": selected, "session_header_errors": header_errors,
                "files": rows, "errors": errors, "excluded_reparse_or_git": exclusions,
                "deleted_since_base": sorted(set(historic) - current_repo_paths),
                "deletion_policy": "Record only; never delete historical backup files.",
                "result": "PASS" if not errors else "INCOMPLETE"}
    write_json(work / "PRIVATE-INVENTORY.json", manifest)
    summary = {"result": manifest["result"], "files": len(rows), "bytes": sum(r["bytes"] for r in rows),
               "delta_files": sum(r["private_storage"] == "NEW_RECEIPT_DELTA" for r in rows),
               "public_delta_files": sum(r["public"] == "DELTA" for r in rows),
               "private_only_files": sum(r["public"] == "PRIVATE_ONLY" for r in rows),
               "exact_git_files": sum(r["public"] == "GIT_EXACT" for r in rows),
               "exact_historical_archive_files": sum(r["public"] == "HISTORICAL_ARCHIVE_EXACT" for r in rows),
               "root_and_descendant_sessions": len(selected), "errors": len(errors),
               "private_or_sensitive_exact_head_paths": [r["relative"] for r in rows if r["exact_head_blob"] and r["public"] == "PRIVATE_ONLY"],
               "warning": "No public archive coverage inferred from path membership; no copies made."}
    write_json(work / "DRY-RUN-SUMMARY.json", summary)
    print(json.dumps(summary))
    return 0 if not errors else 2


def command(argv, **kwargs):
    completed = subprocess.run([str(x) for x in argv], stdout=subprocess.PIPE, stderr=subprocess.PIPE, **kwargs)
    if completed.returncode:
        raise RuntimeError("Local command failed: " + Path(str(argv[0])).name + " exit " + str(completed.returncode))
    return completed.stdout


def prove_public(args):
    """Build exact per-path hashes from the actual immutable historical archives."""
    repo, base = Path(args.repo).resolve(), Path(args.base).resolve()
    work = new_directory(args.work, (repo, base))
    seven = Path(args.sevenzip).resolve()
    index = load(base / "ARCHIVE-INDEX.json")
    verified = []
    for row in index["archives"]:
        path = base / "archives" / relative(row["name"])
        info, _ = inspect_file(path, scan=False)
        if info["sha256"] != row["sha256"] or info["bytes"] != row["bytes"]:
            raise RuntimeError("Historical archive digest mismatch: " + row["name"])
        verified.append(row)
    families = [r["name"] for r in verified if r["name"].endswith((".7z", ".7z.001"))]
    proof, locations = {}, {}
    for n, name in enumerate(families):
        archive = base / "archives" / name
        listing = command([seven, "l", "-slt", "-ba", "-sccUTF-8", archive]).decode("utf-8", errors="strict")
        # Reject archive link/reparse entries BEFORE invoking extraction: checking
        # afterwards alone would permit writes through a malicious link member.
        for line in listing.splitlines():
            key, sep, value = line.partition(" = ")
            if sep and ((key in ("Symbolic Link", "Hard Link", "Reparse Point") and value.strip())
                        or (key == "Attributes" and ("L" in value or "l" in value))):
                raise RuntimeError("Archive link/reparse member is not allowed")
        for line in listing.splitlines():
            if line.startswith("Path = "):
                candidate = line[7:]
                if candidate != str(archive):
                    relative(candidate)
        target = new_directory(work / ("family-" + str(n)))
        command([seven, "x", "-y", "-o" + str(target), archive])
        command([seven, "t", archive])
        extracted = list(walk(target))
        for kind, path, why in extracted:
            if kind == "error" or (kind == "excluded" and why != "GIT_METADATA_SEPARATE_BUNDLE"):
                raise RuntimeError("Unexpected archive extraction entry: " + str(path))
        toolmaps = [path for kind, path, why in extracted if kind == "file" and path.name == "toolchain-map.json"]
        blob_paths = set()
        for mapping in toolmaps:
            table = load(mapping)
            for row in table["files"]:
                digest = row["sha256"]
                if not re.fullmatch(r"[0-9a-f]{64}", digest):
                    raise RuntimeError("Invalid content-map digest")
                blob = mapping.parent / "blobs" / digest[:2] / (digest + ".blob")
                if not blob.exists():
                    blob = mapping.parent / "blobs" / (digest + ".blob")
                if blob not in blob_paths:
                    info, _ = inspect_file(blob, scan=False)
                    if info["sha256"] != digest or info["bytes"] != row["bytes"]:
                        raise RuntimeError("Historical content blob hash mismatch")
                    blob_paths.add(blob)
                rel = relative(row["path"])
                proof.setdefault(rel, set()).add(digest)
                locations.setdefault(rel, set()).add(name)
        for kind, path, why in extracted:
            if kind != "file":
                if kind == "error" or (kind == "excluded" and why != "GIT_METADATA_SEPARATE_BUNDLE"):
                    raise RuntimeError("Unexpected archive extraction entry: " + str(path))
                continue
            if path in blob_paths or path.name == "toolchain-map.json":
                continue
            info, _ = inspect_file(path, scan=False)
            rel = relative(path.relative_to(target).as_posix())
            proof.setdefault(rel, set()).add(info["sha256"])
            locations.setdefault(rel, set()).add(name)
    write_json(work / "HISTORICAL-PUBLIC-HASH-PROOF.json", {
        "schema": SCHEMA, "result": "PASS", "created_utc": now(),
        "method": "Actual7z extraction SHA256; content-addressed map entries verified against extracted blobs.",
        "historical_base_commit": BASE_COMMIT, "sevenzip_sha256": inspect_file(seven, scan=False)[0]["sha256"],
        "verified_archive_parts": verified,
        "files": {p: sorted(v) for p, v in proof.items()},
        "archive_locations": {p: sorted(v) for p, v in locations.items()}})
    print(json.dumps({"result": "PASS", "proven_paths": len(proof), "archive_parts": len(verified)}))
    return 0


def verify_current_set(plan):
    expected = {(r["tree"], r["relative"]) for r in plan["files"] if not r.get("prefix_snapshot")}
    observed, errors = set(), []
    for root in plan["roots"]:
        for kind, path, reason in walk(Path(root["source"])):
            if kind == "file":
                observed.add((root["tree"], path.relative_to(root["source"]).as_posix()))
            elif kind == "error":
                errors.append(str(path))
    if errors or expected != observed:
        raise RuntimeError("SOURCE_FILE_SET_CHANGED: refresh inventory; new/missing/read errors are not silently skipped")


def snapshot_storage(row, target, copy_all=False):
    """Choose storage without filesystem access; BASE_EXACT is inventory history.

    Copy-all never reuses that mutable base path. Keep the absolute verified path
    for existing archive consumers and identify receipt-owned copies portably.
    """
    copied = copy_all or row["private_storage"] != "BASE_EXACT"
    if copied:
        folder = "private-snapshot" if copy_all else "private-delta"
        dest = target / folder / relative(row["tree"]) / relative(row["relative"])
    else:
        dest = Path(row["base_file"])
    metadata = {
        "private_storage": "RECEIPT_COPY" if copy_all else row["private_storage"],
        "inventory_private_storage": row["private_storage"],
        "verified_private_path": str(dest),
        "verified_private_scope": "RECEIPT" if copied else "HISTORICAL_BASE",
        "verified_private_relative": dest.relative_to(target).as_posix() if copied else None,
    }
    return dest, copied, metadata


def snapshot(args):
    plan_path = Path(args.manifest).resolve()
    plan = load(plan_path)
    if plan["result"] != "PASS":
        raise RuntimeError("Cannot copy an incomplete/unstable inventory")
    repo, base = Path(plan["repo"]), Path(plan["base"])
    if git(repo, "rev-parse", "HEAD").decode().strip() != plan["git_head"]:
        raise RuntimeError("Git HEAD changed; refresh inventory after final commit")
    verify_current_set(plan)
    name = args.receipt_name or ("final-" + dt.datetime.now(dt.timezone.utc).strftime("%Y%m%dT%H%M%SZ"))
    if len(PurePosixPath(relative(name)).parts) != 1:
        raise ValueError("Receipt name must be one new directory name")
    target = new_directory(base / "incremental-2026-10-23" / name)
    copy_all = getattr(args, "copy_all", False)
    rows, errors = [], []
    for row in plan["files"]:
        try:
            source = Path(row["source"])
            if reparse(source):
                raise RuntimeError("SOURCE_BECAME_REPARSE_POINT")
            current, _ = inspect_file(source, plan["git_object_format"], row["bytes"] if row.get("prefix_snapshot") else None, False)
            if current["sha256"] != row["sha256"] or current["bytes"] != row["bytes"]:
                raise RuntimeError("SOURCE_CHANGED_SINCE_INVENTORY")
            dest, copied_file, storage = snapshot_storage(row, target, copy_all)
            if copied_file:
                dest.parent.mkdir(parents=True, exist_ok=True)
                with source.open("rb") as src, dest.open("xb") as dst:
                    remaining = row["bytes"]
                    while remaining:
                        part = src.read(min(4 * 1024 * 1024, remaining))
                        if not part:
                            raise RuntimeError("SOURCE_TRUNCATED_DURING_COPY")
                        dst.write(part)
                        remaining -= len(part)
            copied, _ = inspect_file(dest, scan=False)
            # This paired D hash also catches a source race during the copy.
            if copied["sha256"] != row["sha256"] or copied["bytes"] != row["bytes"]:
                raise RuntimeError("PRIVATE_D_HASH_MISMATCH")
            rows.append({**row, **storage, "paired_D_sha256": copied["sha256"]})
        except (OSError, RuntimeError, ValueError) as e:
            errors.append({"tree": row["tree"], "relative": row["relative"], "error": str(e)})
    try:
        verify_current_set(plan)
        if git(repo, "rev-parse", "HEAD").decode().strip() != plan["git_head"]:
            raise RuntimeError("HEAD_CHANGED_DURING_COPY")
        if args.bundle_git and not errors:
            bundle = target / "private-git-all-refs.bundle"
            command(["git", "-C", repo, "bundle", "create", bundle, "--all"])
            command(["git", "-C", repo, "bundle", "verify", bundle])
            bundle_hash = inspect_file(bundle, scan=False)[0]
        else:
            bundle_hash = None
    except (OSError, RuntimeError) as e:
        errors.append({"error": str(e)})
        bundle_hash = None
    receipt = {"schema": SCHEMA, "result": "PASS" if not errors else "INCOMPLETE", "created_utc": now(),
               "snapshot_mode": "COPY_ALL" if copy_all else "LINKED_DELTA",
               "copied_file_count": sum(r["verified_private_scope"] == "RECEIPT" for r in rows),
               "linked_base_file_count": sum(r["verified_private_scope"] == "HISTORICAL_BASE" for r in rows),
               "manifest_sha256": hashlib.sha256(plan_path.read_bytes()).hexdigest(),
               "git_head": plan["git_head"], "historical_base_commit": BASE_COMMIT,
               "base": plan["base"], "files": rows, "errors": errors,
               "private_git_bundle": bundle_hash,
               "private_git_bundle_relative": "private-git-all-refs.bundle" if bundle_hash else None,
               "public_proof": plan["public_proof"], "public_proof_sha256": plan["public_proof_sha256"],
               "deleted_since_base": plan["deleted_since_base"],
               "private_context_boundary": "Root+descendant JSONL complete-record prefixes at inventory time; later append is not claimed.",
               "remote_verified": False, "no_overwrite_of_historical_receipt": True}
    write_json(target / "PRIVATE-CLOSEOUT-RECEIPT.json", receipt)
    print(json.dumps({"result": receipt["result"], "receipt": str(target / "PRIVATE-CLOSEOUT-RECEIPT.json"), "verified_files": len(rows), "errors": len(errors)}))
    return 0 if not errors else 2


def scrub(raw, known):
    # Work only on a PUBLIC staging copy. Private D bytes are never modified.
    count = 0
    for token in sorted(known, key=len, reverse=True):
        for encoded in (token, token.decode("ascii").encode("utf-16-le"), token.decode("ascii").encode("utf-16-be")):
            n = raw.count(encoded)
            if n:
                count += n
                replacement = "REDACTED".encode("utf-16-le" if encoded == token.decode("ascii").encode("utf-16-le") else "utf-16-be") if encoded != token else b"REDACTED"
                raw = raw.replace(encoded, replacement)
    return raw, count


def archive(args):
    receipt_path = Path(args.receipt).resolve()
    receipt = load(receipt_path)
    if receipt["result"] != "PASS":
        raise RuntimeError("Private receipt must be PASS before public packaging")
    if receipt["public_proof"]:
        actual = hashlib.sha256(Path(receipt["public_proof"]).read_bytes()).hexdigest()
        if actual != receipt["public_proof_sha256"]:
            raise RuntimeError("Historical public proof changed")
    work = new_directory(args.work, (Path(args.repo).resolve(), Path(args.base).resolve()))
    stage = new_directory(work / "public-stage")
    known = set()
    # Collect capabilities from the secured D snapshot, including private files.
    # Never print/store the capability values in a public manifest.
    for row in receipt["files"]:
        info, caps = inspect_file(Path(row["verified_private_path"]))
        if info["sha256"] != row["paired_D_sha256"]:
            raise RuntimeError("Secured D file changed before packaging")
        known.update(caps)
    entries = []
    for row in receipt["files"]:
        if row["public"] != "DELTA":
            continue
        source = Path(row["verified_private_path"])
        raw = source.read_bytes()
        cleaned, replacements = scrub(raw, known)
        for view in (cleaned, cleaned.replace(b"\0", b"")):
            if CAP.search(view) or TOKEN.search(view) or any(p.search(view) for p in SECRET.values()):
                raise RuntimeError("Public scan rejected path: " + row["relative"])
        dest = stage / "RungProof" / relative(row["relative"])
        dest.parent.mkdir(parents=True, exist_ok=True)
        with dest.open("xb") as f:
            f.write(cleaned)
        entries.append({"path": dest.relative_to(stage).as_posix(), "source_sha256": row["sha256"],
                        "public_sha256": hashlib.sha256(cleaned).hexdigest(), "public_bytes": len(cleaned),
                        "capability_replacements": replacements})
    public = {"schema": SCHEMA, "created_utc": now(), "required_git_commit": receipt["git_head"],
              "historical_base_commit": BASE_COMMIT, "files": entries,
              "coverage": {k: sum(r["public"] == k for r in receipt["files"]) for k in ("DELTA", "GIT_EXACT", "HISTORICAL_ARCHIVE_EXACT", "PRIVATE_ONLY")},
              "restore": "Restore historical release + exact Git commit, then extract this delta above the RungProof directory; private D receipt remains authoritative for originals.",
              "security_scope": "Private filename/root exclusions, known capability redaction and byte-pattern credential scan; not a universal secret detector.",
              "remote_verified": False, "upload_performed": False}
    write_json(stage / "PUBLIC-MANIFEST.json", public)
    package = new_directory(receipt_path.parent / "public-delta")
    seven = Path(args.sevenzip).resolve()
    archive_path = package / "rungproof-final-delta.7z"
    command([seven, "a", "-t7z", "-mx=3", "-mmt=2", "-v700m", archive_path, "."], cwd=stage)
    command([seven, "t", Path(str(archive_path) + ".001")])
    for row in entries:
        info, _ = inspect_file(stage / row["path"], scan=False)
        if info["sha256"] != row["public_sha256"]:
            raise RuntimeError("Public stage changed during archive creation")
    parts = [{"name": p.name, **inspect_file(p, scan=False)[0]} for p in sorted(package.glob("*.7z.*"))]
    write_json(package / "PUBLIC-DELTA-RECEIPT.json", {"result": "PASS", "created_utc": now(), "git_head": receipt["git_head"],
               "parts": [{k: r[k] for k in ("name", "bytes", "sha256")} for r in parts],
               "files": entries, "remote_verified": False, "upload_performed": False})
    (package / "SHA256SUMS").write_text("".join(r["sha256"] + "  " + r["name"] + "\n" for r in parts), encoding="utf-8")
    print(json.dumps({"result": "PASS", "public_files": len(entries), "parts": len(parts), "package": str(package), "upload_performed": False}))
    return 0


def self_test():
    assert relative("rungproof-next/outputs/new.py") == "rungproof-next/outputs/new.py"
    for bad in ("../x", "C:/x", "/tmp/x", "a/../../x", "a\nx"):
        try:
            relative(bad)
        except ValueError:
            pass
        else:
            raise AssertionError("unsafe path accepted")
    token = b"Z" * 32
    raw = b"http://127.0.0.1:12345/" + token + b" bare=" + token
    cleaned, n = scrub(raw, {token})
    assert n == 2 and token not in cleaned and not CAP.search(cleaned)
    cleaned, n = scrub(raw.decode().encode("utf-16-le"), {token})
    assert n == 2 and token.decode().encode("utf-16-le") not in cleaned
    # A later flat-mirror update must not redirect copy-all back to BASE_EXACT.
    target = Path("new-receipt")
    base_row = {"tree": "RungProof", "relative": "docs/final.md",
                "private_storage": "BASE_EXACT", "base_file": "mutable-base/RungProof/docs/final.md"}
    delta_row = {**base_row, "private_storage": "NEW_RECEIPT_DELTA", "base_file": None}
    for row in (base_row, delta_row):
        dest, copied, metadata = snapshot_storage(row, target, True)
        assert copied and dest == target / "private-snapshot/RungProof/docs/final.md"
        assert metadata["private_storage"] == "RECEIPT_COPY"
        assert metadata["inventory_private_storage"] == row["private_storage"]
        assert metadata["verified_private_scope"] == "RECEIPT"
        assert metadata["verified_private_relative"] == "private-snapshot/RungProof/docs/final.md"
        assert Path(metadata["verified_private_path"]) == target / metadata["verified_private_relative"]
    dest, copied, metadata = snapshot_storage(base_row, target)
    assert not copied and dest == Path(base_row["base_file"])
    assert metadata["private_storage"] == "BASE_EXACT" and metadata["verified_private_relative"] is None
    assert metadata["verified_private_scope"] == "HISTORICAL_BASE"
    dest, copied, metadata = snapshot_storage(delta_row, target)
    assert copied and dest == target / "private-delta/RungProof/docs/final.md"
    assert metadata["private_storage"] == "NEW_RECEIPT_DELTA"
    extra = {**delta_row, "tree": "godot-userdata", "relative": "settings.cfg", "public": "PRIVATE_ONLY"}
    prefix = {**delta_row, "tree": "private-codex-context", "relative": "session.jsonl", "public": "PRIVATE_ONLY",
              "prefix_snapshot": True, "bytes": 17, "session": {"snapshot_bytes": 17}}
    for row in (extra, prefix):
        dest, copied, metadata = snapshot_storage(row, target, True)
        stored = {**row, **metadata}
        assert copied and dest == target / "private-snapshot" / row["tree"] / row["relative"]
        assert stored["public"] == "PRIVATE_ONLY"
        if row.get("prefix_snapshot"):
            assert stored["prefix_snapshot"] and stored["bytes"] == stored["session"]["snapshot_bytes"] == 17
    print("PASS: pure in-memory path/redaction and linked/copy-all storage metadata checks; no backup copy/archive/network activity")
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("inventory", "prove-public", "snapshot", "archive", "self-test"))
    parser.add_argument("--repo", default=str(Path(__file__).resolve().parents[3]))
    parser.add_argument("--base", default="D:/RungProof-Backup-2026-10-22")
    parser.add_argument("--work")
    parser.add_argument("--public-proof")
    parser.add_argument("--manifest")
    parser.add_argument("--receipt")
    parser.add_argument("--receipt-name")
    parser.add_argument("--sevenzip")
    parser.add_argument("--extra", action="append", default=[], metavar="PRIVATE_LABEL=SOURCE_DIRECTORY")
    parser.add_argument("--root-thread", action="append", default=None)
    parser.add_argument("--session-root", action="append", default=None)
    parser.add_argument("--allow-container-hash", action="append", default=[])
    parser.add_argument("--bundle-git", action="store_true")
    parser.add_argument("--copy-all", action="store_true",
                        help="snapshot: copy every planned regular file into the new receipt instead of linking matched base files")
    args = parser.parse_args()
    args.root_thread = args.root_thread or [ROOT_THREAD]
    home = Path.home() / ".codex"
    args.session_root = args.session_root or [str(home / "sessions"), str(home / "archived_sessions")]
    if args.mode in ("inventory", "prove-public", "archive") and not args.work:
        parser.error("--work must be a new directory outside repo and historical D backup")
    if args.mode in ("prove-public", "archive") and not args.sevenzip:
        parser.error("--sevenzip requires an explicit existing approved local7z.exe")
    if args.mode == "snapshot" and not args.manifest:
        parser.error("snapshot requires --manifest")
    if args.copy_all and args.mode != "snapshot":
        parser.error("--copy-all applies only to snapshot")
    if args.mode == "archive" and not args.receipt:
        parser.error("archive requires --receipt")
    return {"inventory": inventory, "prove-public": prove_public, "snapshot": snapshot, "archive": archive,
            "self-test": lambda unused: self_test()}[args.mode](args)


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, RuntimeError, ValueError, subprocess.SubprocessError) as error:
        # No raw file contents or credential values are printed.
        print(json.dumps({"result": "ERROR", "message": str(error)}), file=sys.stderr)
        raise SystemExit(2)
