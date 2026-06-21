#!/usr/bin/env python3
"""Build, package, and optionally upload DataEditorX release zips."""

from __future__ import annotations

import argparse
import re
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "DataEditorX" / "DataEditorX.csproj"
UPDATE_INFO = ROOT / "DataEditorX" / "readme.txt"
ARTIFACTS = ROOT / "artifacts"
PUBLISH = ARTIFACTS / "publish"
DEFAULT_REPO = "ElderLich/DataEditorX"
DEFAULT_TAG = "MDPro3"
DEFAULT_FRAMEWORK = "net9.0-windows7.0"
NUGET_ORG = "https://api.nuget.org/v3/index.json"
VS_OFFLINE_PACKAGES = Path(r"C:\Program Files (x86)\Microsoft SDKs\NuGetPackages")


def run(args: list[str], *, cwd: Path = ROOT, check: bool = True) -> subprocess.CompletedProcess:
    print("+", " ".join(args))
    return subprocess.run(args, cwd=cwd, check=check)


def assembly_version(version: str) -> str:
    parts = version.split(".")
    if len(parts) == 3:
        return version + ".0"
    if len(parts) == 4:
        return version
    raise ValueError("Version must look like 1.0.0 or 1.0.0.0")


def update_text_file(path: Path, replacements: list[tuple[str, str]]) -> None:
    raw = path.read_bytes()
    has_bom = raw.startswith(b"\xef\xbb\xbf")
    original = raw.decode("utf-8-sig")
    text = original
    for pattern, replacement in replacements:
        text = re.sub(pattern, replacement, text)
    if text == original:
        return
    path.write_text(text, encoding="utf-8-sig" if has_bom else "utf-8")


def default_sources() -> list[str]:
    sources = [NUGET_ORG]
    if VS_OFFLINE_PACKAGES.exists():
        sources.append(str(VS_OFFLINE_PACKAGES))
    return sources


def source_args(sources: list[str]) -> list[str]:
    args = []
    for source in sources:
        args.extend(["--source", source])
    return args


def normalize_newlines(text: str) -> str:
    return text.replace("\r\n", "\n").replace("\r", "\n")


def update_version_files(version: str, repo: str, tag: str) -> None:
    asm = assembly_version(version)
    update_text_file(
        PROJECT,
        [
            (r"<Version>.*?</Version>", f"<Version>{version}</Version>"),
            (r"<AssemblyVersion>.*?</AssemblyVersion>", f"<AssemblyVersion>{asm}</AssemblyVersion>"),
            (r"<FileVersion>.*?</FileVersion>", f"<FileVersion>{asm}</FileVersion>"),
            (r"<InformationalVersion>.*?</InformationalVersion>", f"<InformationalVersion>{version}</InformationalVersion>"),
        ],
    )

    base = f"https://github.com/{repo}/releases/download/{tag}"
    update_info = "\n".join(
        [
            f"[DataEditorX]{version}[DataEditorX]",
            f"[URL]{base}/DataEditorX_win32.zip [URL]",
            f"[URL]{base}/DataEditorX_win64.zip [URL]",
            "",
        ]
    )
    raw = UPDATE_INFO.read_bytes() if UPDATE_INFO.exists() else b""
    if normalize_newlines(raw.decode("utf-8-sig")) != update_info:
        encoding = "utf-8-sig" if not raw or raw.startswith(b"\xef\xbb\xbf") else "utf-8"
        UPDATE_INFO.write_text(update_info, encoding=encoding)


def zip_dir(source: Path, destination: Path) -> None:
    if destination.exists():
        destination.unlink()
    with zipfile.ZipFile(destination, "w", zipfile.ZIP_DEFLATED) as zf:
        for file in sorted(source.rglob("*")):
            if file.is_file():
                zf.write(file, file.relative_to(source))


def strip_debug_symbols(output: Path) -> None:
    for file in output.rglob("*.pdb"):
        file.unlink()


def publish(
    runtime: str,
    output: Path,
    version: str,
    framework: str,
    self_contained: bool,
    include_symbols: bool,
    sources: list[str],
) -> None:
    if output.exists():
        shutil.rmtree(output)
    asm = assembly_version(version)
    run(
        [
            "dotnet",
            "publish",
            str(PROJECT),
            *source_args(sources),
            "-c",
            "Release",
            "-f",
            framework,
            "-r",
            runtime,
            "--self-contained",
            "true" if self_contained else "false",
            "-p:PublishSingleFile=false",
            f"-p:Version={version}",
            f"-p:AssemblyVersion={asm}",
            f"-p:FileVersion={asm}",
            f"-p:InformationalVersion={version}",
            "-o",
            str(output),
        ]
    )
    if not include_symbols:
        strip_debug_symbols(output)


def upload(repo: str, tag: str, version: str, assets: list[Path]) -> None:
    view = run(["gh", "release", "view", tag, "--repo", repo], check=False)
    if view.returncode != 0:
        run(["gh", "release", "create", tag, "--repo", repo, "--title", f"DataEditorX {version}", "--notes", f"DataEditorX {version}"])
    run(["gh", "release", "upload", tag, "--repo", repo, "--clobber", *[str(asset) for asset in assets]])


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Build DataEditorX release zips and optionally upload them to GitHub.")
    parser.add_argument("--version", required=True, help="Release version, for example 1.0.0.")
    parser.add_argument("--repo", default=DEFAULT_REPO, help=f"GitHub owner/repo. Default: {DEFAULT_REPO}.")
    parser.add_argument("--tag", default=DEFAULT_TAG, help=f"GitHub release tag. Default: {DEFAULT_TAG}.")
    parser.add_argument("--upload", action="store_true", help="Upload zips to the GitHub release using the gh CLI.")
    parser.add_argument("--skip-build", action="store_true", help="Only update version metadata and upload existing zips.")
    parser.add_argument("--self-contained", action="store_true", help="Publish self-contained builds instead of framework-dependent builds.")
    parser.add_argument("--include-symbols", action="store_true", help="Keep .pdb debug symbol files in the release zips.")
    parser.add_argument("--framework", default=DEFAULT_FRAMEWORK, help=f"Target framework to publish. Default: {DEFAULT_FRAMEWORK}.")
    parser.add_argument("--source", action="append", help=f"NuGet source for restore. Can be repeated. Default: {NUGET_ORG}.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    version = args.version.lstrip("v")
    sources = args.source or default_sources()
    update_version_files(version, args.repo, args.tag)

    win32_publish = PUBLISH / "win32"
    win64_publish = PUBLISH / "win64"
    win32_zip = ARTIFACTS / "DataEditorX_win32.zip"
    win64_zip = ARTIFACTS / "DataEditorX_win64.zip"

    ARTIFACTS.mkdir(exist_ok=True)
    if not args.skip_build:
        publish("win-x86", win32_publish, version, args.framework, args.self_contained, args.include_symbols, sources)
        publish("win-x64", win64_publish, version, args.framework, args.self_contained, args.include_symbols, sources)
        zip_dir(win32_publish, win32_zip)
        zip_dir(win64_publish, win64_zip)

    print(f"Created: {win32_zip}")
    print(f"Created: {win64_zip}")

    if args.upload:
        upload(args.repo, args.tag, version, [win32_zip, win64_zip])

    return 0


if __name__ == "__main__":
    sys.exit(main())
