"""Read-only source verification plus fresh 7-Zip extraction of a public delta.

Standalone schema-1 reader for approved closeout.py SHA256 3c4b1837...e61b18.
No network, Git, credential scan or production action is performed implicitly.
Only the explicitly supplied NEW work directory is written. No payload/token
contents or filenames are printed; complete path/hash evidence stays private.
"""
from pathlib import Path
import argparse
import datetime as dt
import hashlib
import json
import os
import re
import stat
import subprocess
import sys

sys.dont_write_bytecode = True
SCHEMA_HELPER_SHA = "3c4b1837e10973b6abe8c88060cd91aa4d8d117f63f0f4a46125f694e1e61b18"
HISTORICAL_COMMIT = "d0c4566bc956706ab3430e25d4142b03f4431e9c"
ARCHIVE_BASE = "rungproof-final-delta.7z"
HEX64 = re.compile(r"[0-9a-f]{64}\Z")
COMMIT = re.compile(r"(?:[0-9a-f]{40}|[0-9a-f]{64})\Z")
METADATA_LIMIT = 128 * 1024 * 1024
RESTORE_TEXT = "Restore historical release + exact Git commit, then extract this delta above the RungProof directory; private D receipt remains authoritative for originals."
SECURITY_TEXT = "Private filename/root exclusions, known capability redaction and byte-pattern credential scan; not a universal secret detector."


class Rejected(Exception):
    def __init__(self, code):
        self.code = code
        super().__init__(code)


def require(value, code):
    if not value:
        raise Rejected(code)


def now():
    return dt.datetime.now(dt.timezone.utc).isoformat()


def duplicate_free(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, "DUPLICATE_JSON_KEY")
        result[key] = value
    return result


def json_bytes(raw):
    require(len(raw) <= METADATA_LIMIT, "METADATA_TOO_LARGE")
    return json.loads(raw.decode("utf-8-sig"), object_pairs_hook=duplicate_free)


def plain(path):
    path = Path(os.path.abspath(os.fspath(path)))
    try:
        for item in (path, *path.parents):
            info = item.lstat()
            require(not stat.S_ISLNK(info.st_mode) and not getattr(info, "st_file_attributes", 0) & 0x400,
                    "REPARSE_PATH")
    except OSError:
        raise Rejected("MISSING_OR_UNREADABLE_PATH")
    return path


def fingerprint(info):
    return info.st_size, info.st_mtime_ns, info.st_ino


def inspect(path):
    path = plain(path)
    before = path.lstat()
    require(stat.S_ISREG(before.st_mode), "NON_REGULAR_FILE")
    require(getattr(before, "st_nlink", 1) == 1, "MULTIPLY_LINKED_FILE")
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(4 * 1024 * 1024), b""):
            digest.update(block)
    after = path.lstat()
    require(fingerprint(before) == fingerprint(after), "FILE_CHANGED_DURING_HASH")
    return {"bytes": after.st_size, "sha256": digest.hexdigest()}


def safe_relative(value, listing=False):
    require(isinstance(value, str) and value and not any(ord(c) < 32 or ord(c) == 127 for c in value), "UNSAFE_RELATIVE_PATH")
    require(listing or "\\" not in value, "NON_CANONICAL_RELATIVE_PATH")
    value = value.replace("\\", "/")
    parts = value.split("/")
    require(all(part and part not in (".", "..") and not part.endswith((" ", "."))
                and not any(c in part for c in ':<>"|?*')
                and not re.fullmatch(r"(?i)(?:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\..*)?", part)
                for part in parts), "UNSAFE_RELATIVE_PATH")
    return "/".join(parts)


def validate_entries(entries):
    require(isinstance(entries, list), "INVALID_FILE_LIST")
    result, seen = {}, set()
    for row in entries:
        require(isinstance(row, dict) and set(row) == {"path", "source_sha256", "public_sha256", "public_bytes", "capability_replacements"}, "INVALID_FILE_ROW")
        path = safe_relative(row["path"])
        require(path.startswith("RungProof/") and path.casefold() not in seen, "DUPLICATE_OR_WRONG_PAYLOAD_ROOT")
        seen.add(path.casefold())
        require(type(row["public_bytes"]) is int and row["public_bytes"] >= 0
                and type(row["capability_replacements"]) is int and row["capability_replacements"] >= 0
                and isinstance(row["source_sha256"], str) and HEX64.fullmatch(row["source_sha256"])
                and isinstance(row["public_sha256"], str) and HEX64.fullmatch(row["public_sha256"]), "INVALID_FILE_HASH_OR_SIZE")
        result[path] = row
    # A file may not simultaneously be a parent directory of another file.
    folds = {path.casefold() for path in result}
    require(all("/".join(path.split("/")[:index]).casefold() not in folds
                for path in result for index in range(1, len(path.split("/")))), "FILE_DIRECTORY_COLLISION")
    return result


def directory_set(file_paths):
    return {"/".join(path.split("/")[:index]) for path in file_paths for index in range(1, len(path.split("/")))}


def validate_parts(receipt_path, receipt):
    rows = receipt.get("parts")
    require(isinstance(rows, list) and rows, "NO_ARCHIVE_PARTS")
    indexed = {}
    for row in rows:
        require(isinstance(row, dict) and set(row) == {"name", "bytes", "sha256"}, "INVALID_PART_ROW")
        match = re.fullmatch(re.escape(ARCHIVE_BASE) + r"\.(\d{3,})", row.get("name", ""))
        require(match is not None, "INVALID_PART_NAME")
        index = int(match.group(1))
        require(index > 0 and row["name"] == ARCHIVE_BASE + "." + f"{index:03d}" and index not in indexed,
                "DUPLICATE_OR_NONCANONICAL_PART")
        require(type(row["bytes"]) is int and row["bytes"] > 0 and isinstance(row["sha256"], str)
                and HEX64.fullmatch(row["sha256"]), "INVALID_PART_HASH_OR_SIZE")
        indexed[index] = row
    require(set(indexed) == set(range(1, len(indexed) + 1)), "NONCONTIGUOUS_PARTS")
    observed = {path.name for path in receipt_path.parent.iterdir()
                if path.name == ARCHIVE_BASE or path.name.startswith(ARCHIVE_BASE + ".")}
    require(observed == {row["name"] for row in rows}, "UNPINNED_OR_MISSING_NEIGHBOR_PART")
    evidence = []
    for index in range(1, len(indexed) + 1):
        row = indexed[index]
        actual = inspect(receipt_path.parent / row["name"])
        require(actual["bytes"] == row["bytes"] and actual["sha256"] == row["sha256"], "PART_HASH_OR_SIZE_MISMATCH")
        evidence.append(dict(row))
    first = receipt_path.parent / indexed[1]["name"]
    with first.open("rb") as stream:
        require(stream.read(6) == b"7z\xbc\xaf\x27\x1c", "NOT_APPROVED_7Z_FORMAT")
    return first, evidence


def run_7z(executable, arguments, timeout, report, operation):
    completed = subprocess.run([str(executable), *arguments], stdin=subprocess.DEVNULL,
                               capture_output=True, timeout=timeout, check=False)
    report.setdefault("sevenzip_operations", []).append({
        "operation": operation, "exit_code": completed.returncode,
        "stdout_bytes": len(completed.stdout), "stdout_sha256": hashlib.sha256(completed.stdout).hexdigest(),
        "stderr_bytes": len(completed.stderr), "stderr_sha256": hashlib.sha256(completed.stderr).hexdigest()})
    require(completed.returncode == 0, "SEVENZIP_" + operation.upper() + "_FAILED")
    require(len(completed.stdout) <= METADATA_LIMIT and len(completed.stderr) <= METADATA_LIMIT, "SEVENZIP_OUTPUT_TOO_LARGE")
    return completed.stdout


def validate_listing(raw, entries):
    text = raw.decode("utf-8").replace("\r\r\n", "\n").replace("\r\n", "\n")
    require("\r" not in text, "UNSUPPORTED_LISTING_CONTROL")
    known = {"Path", "Size", "Packed Size", "Modified", "Created", "Accessed", "Attributes", "CRC", "Encrypted", "Method", "Block", "Host OS", "Characteristics", "Symbolic Link", "Hard Link", "Anti", "Alternate Stream", "NT Security ID"}
    records, current = [], {}
    for line in text.split("\n"):
        if not line:
            if current:
                records.append(current); current = {}
            continue
        require(" = " in line, "UNSUPPORTED_LISTING_RECORD")
        key, value = line.split(" = ", 1)
        require(key in known and key not in current, "UNSUPPORTED_OR_DUPLICATE_LISTING_FIELD")
        current[key] = value
    if current:
        records.append(current)
    expected = set(entries) | {"PUBLIC-MANIFEST.json"}
    directories = directory_set(expected)
    files, seen, manifest_size = set(), set(), None
    for record in records:
        path = safe_relative(record.get("Path"), listing=True)
        require(path.casefold() not in seen, "DUPLICATE_ARCHIVE_PATH")
        seen.add(path.casefold())
        require(not any(key in record for key in ("Symbolic Link", "Hard Link"))
                and record.get("Anti", "-") == "-" and record.get("Alternate Stream", "-") == "-"
                and record.get("Encrypted", "-") == "-"
                and "reparse" not in record.get("Characteristics", "").casefold(), "SPECIAL_OR_ENCRYPTED_ARCHIVE_ENTRY")
        attributes = record.get("Attributes", "")
        mode = re.search(r"(?:^|\s)([-dlcbps])[rwxStTs-]{9}(?:\s|$)", attributes)
        require(mode is None or mode.group(1) in "-d", "SPECIAL_ARCHIVE_MODE")
        is_directory = "D" in attributes.split(" ")[0] or mode is not None and mode.group(1) == "d"
        require(record.get("Size", "").isdigit(), "INVALID_LISTED_SIZE")
        size = int(record["Size"])
        if is_directory:
            require(path in directories and size == 0, "UNEXPECTED_ARCHIVE_DIRECTORY")
        else:
            require(path in expected, "UNMANIFESTED_ARCHIVE_PAYLOAD")
            if path == "PUBLIC-MANIFEST.json":
                require(size <= METADATA_LIMIT, "EMBEDDED_MANIFEST_TOO_LARGE")
                manifest_size = size
            else:
                require(size == entries[path]["public_bytes"], "LISTED_PAYLOAD_SIZE_MISMATCH")
            files.add(path)
    require(files == expected and manifest_size is not None, "ARCHIVE_FILE_SET_MISMATCH")
    return manifest_size


def actual_files(root, expected):
    files, todo, seen = {}, [root], set()
    allowed_directories = directory_set(expected)
    while todo:
        for entry in os.scandir(todo.pop()):
            path = Path(entry.path)
            relative = safe_relative(path.relative_to(root).as_posix())
            require(relative.casefold() not in seen, "EXTRACTED_CASE_COLLISION")
            seen.add(relative.casefold())
            info = path.lstat()
            require(not stat.S_ISLNK(info.st_mode) and not getattr(info, "st_file_attributes", 0) & 0x400,
                    "EXTRACTED_REPARSE_ENTRY")
            if stat.S_ISDIR(info.st_mode):
                require(relative in allowed_directories, "UNEXPECTED_EXTRACTED_DIRECTORY")
                todo.append(path)
            else:
                require(stat.S_ISREG(info.st_mode) and relative in expected, "UNMANIFESTED_OR_NONREGULAR_EXTRACTED_FILE")
                files[relative] = inspect(path)
    require(set(files) == expected, "EXTRACTED_FILE_SET_MISMATCH")
    return files


def verify(args):
    require(isinstance(args.receipt_sha256, str) and HEX64.fullmatch(args.receipt_sha256)
            and isinstance(args.sevenzip_sha256, str) and HEX64.fullmatch(args.sevenzip_sha256), "EXPLICIT_SHA256_REQUIRED")
    receipt_path = plain(args.receipt).resolve()
    executable = plain(args.sevenzip).resolve()
    work = Path(os.path.abspath(args.work))
    plain(work.parent)
    require(not work.exists(), "WORK_MUST_BE_NEW")
    require(work != receipt_path.parent and receipt_path.parent not in work.parents, "WORK_INSIDE_SOURCE_ARCHIVE_DIRECTORY")
    work.mkdir()
    report = {"schema": 1, "result": "INCOMPLETE", "kind": "PRIVATE_PUBLIC_DELTA_EXACT_READBACK",
              "created_utc": now(), "schema_helper_sha256": SCHEMA_HELPER_SHA,
              "verifier_sha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
              "receipt_sha256": args.receipt_sha256, "sevenzip_sha256": args.sevenzip_sha256,
              "credential_scan_performed": False, "remote_verified": False,
              "boundary": "Local pinned public-delta bytes and exact fresh extraction only; no original private-source, historical release, Git remote, complete project, credential or app/native acceptance proof. Source parts must have no writers. All failed/partial readback artifacts are retained."}
    try:
        require(receipt_path.stat().st_size <= METADATA_LIMIT, "METADATA_TOO_LARGE")
        raw = receipt_path.read_bytes()
        require(hashlib.sha256(raw).hexdigest() == args.receipt_sha256, "RECEIPT_PIN_MISMATCH")
        receipt = json_bytes(raw)
        require(isinstance(receipt, dict) and receipt.get("result") == "PASS"
                and set(receipt) == {"result", "created_utc", "git_head", "parts", "files", "remote_verified", "upload_performed"}
                and isinstance(receipt.get("created_utc"), str) and receipt["created_utc"]
                and receipt.get("remote_verified") is False and receipt.get("upload_performed") is False
                and isinstance(receipt.get("git_head"), str) and COMMIT.fullmatch(receipt["git_head"]), "INVALID_PUBLIC_RECEIPT")
        require(inspect(executable)["sha256"] == args.sevenzip_sha256, "SEVENZIP_PIN_MISMATCH")
        entries = validate_entries(receipt.get("files"))
        first, part_evidence = validate_parts(receipt_path, receipt)
        report["parts"] = part_evidence
        listing = run_7z(executable, ["l", "-slt", "-ba", "-sccUTF-8", str(first)], args.timeout_seconds, report, "list")
        manifest_size = validate_listing(listing, entries)
        extraction = work / "fresh-extraction"
        extraction.mkdir()
        selection = work / "EXPLICIT-SAFE-FILE-SELECTION.txt"
        with selection.open("x", encoding="utf-8", newline="\n") as stream:
            stream.write("\n".join(sorted(set(entries) | {"PUBLIC-MANIFEST.json"})) + "\n")
        run_7z(executable, ["x", "-y", "-bd", "-bb0", "-sccUTF-8", "-scsUTF-8", "-spd",
                           "-i@" + str(selection), "-o" + str(extraction), str(first)], args.timeout_seconds, report, "extract")
        observed = actual_files(extraction, set(entries) | {"PUBLIC-MANIFEST.json"})
        require(observed["PUBLIC-MANIFEST.json"]["bytes"] == manifest_size, "EMBEDDED_MANIFEST_SIZE_MISMATCH")
        manifest = json_bytes((extraction / "PUBLIC-MANIFEST.json").read_bytes())
        require(isinstance(manifest, dict) and type(manifest.get("schema")) is int and manifest["schema"] == 1
                and set(manifest) == {"schema", "created_utc", "required_git_commit", "historical_base_commit", "files", "coverage", "restore", "security_scope", "remote_verified", "upload_performed"}
                and isinstance(manifest.get("created_utc"), str) and manifest["created_utc"]
                and manifest.get("required_git_commit") == receipt["git_head"]
                and manifest.get("historical_base_commit") == HISTORICAL_COMMIT, "EMBEDDED_MANIFEST_IDENTITY_MISMATCH")
        require(manifest.get("files") == receipt["files"], "EMBEDDED_MANIFEST_FILES_MISMATCH")
        coverage = manifest.get("coverage")
        require(isinstance(coverage, dict) and set(coverage) == {"DELTA", "GIT_EXACT", "HISTORICAL_ARCHIVE_EXACT", "PRIVATE_ONLY"}
                and all(type(value) is int and value >= 0 for value in coverage.values())
                and coverage["DELTA"] == len(entries) and manifest.get("restore") == RESTORE_TEXT
                and manifest.get("security_scope") == SECURITY_TEXT
                and manifest.get("remote_verified") is False and manifest.get("upload_performed") is False,
                "EMBEDDED_MANIFEST_SCHEMA_MISMATCH")
        for path, row in entries.items():
            require(observed[path]["bytes"] == row["public_bytes"] and observed[path]["sha256"] == row["public_sha256"], "EXTRACTED_PAYLOAD_HASH_OR_SIZE_MISMATCH")
        _, final_parts = validate_parts(receipt_path, receipt)
        require(final_parts == part_evidence and hashlib.sha256(receipt_path.read_bytes()).hexdigest() == args.receipt_sha256
                and inspect(executable)["sha256"] == args.sevenzip_sha256, "PINNED_INPUT_CHANGED")
        report.update(result="PASS", archive_parts=len(part_evidence), public_payload_files=len(entries),
                      public_payload_bytes=sum(row["public_bytes"] for row in entries.values()),
                      extracted_files=observed, embedded_manifest_sha256=observed["PUBLIC-MANIFEST.json"]["sha256"],
                      required_git_commit=receipt["git_head"], coverage_source_assertions=coverage,
                      other_coverage_counts_independently_proven=False, exact_file_set=True, input_pins_rechecked=True)
        return report
    except Exception as error:
        report.update(result="FAIL", failure_code=error.code if isinstance(error, Rejected) else type(error).__name__)
        raise
    finally:
        report["completed_utc"] = now()
        with (work / "PRIVATE-PUBLIC-READBACK-REPORT.json").open("x", encoding="utf-8") as stream:
            json.dump(report, stream, indent=2)
            stream.write("\n")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("receipt", "receipt-sha256", "sevenzip", "sevenzip-sha256", "work"):
        parser.add_argument("--" + name, required=True)
    parser.add_argument("--timeout-seconds", type=int, default=900)
    args = parser.parse_args()
    try:
        require(1 <= args.timeout_seconds <= 3600, "INVALID_TIMEOUT")
        report = verify(args)
        print(json.dumps({"result": "PASS", "parts": report["archive_parts"], "payload_files": report["public_payload_files"],
                          "payload_bytes": report["public_payload_bytes"], "receipt_sha256": args.receipt_sha256,
                          "embedded_manifest_sha256": report["embedded_manifest_sha256"], "report": str(Path(args.work) / "PRIVATE-PUBLIC-READBACK-REPORT.json")}))
        return 0
    except Exception as error:
        print(json.dumps({"result": "FAIL", "failure_code": error.code if isinstance(error, Rejected) else type(error).__name__}))
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
