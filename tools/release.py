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
    text = path.read_text(encoding="utf-8-sig")
    for pattern, replacement in replacements:
        text = re.sub(pattern, replacement, text)
    path.write_text(text, encoding="utf-8")


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
    UPDATE_INFO.write_text(
        "\n".join(
            [
                f"[DataEditorX]{version}[DataEditorX]",
                f"[URL]{base}/DataEditorX_win32.zip [URL]",
                f"[URL]{base}/DataEditorX_win64.zip [URL]",
                "",
            ]
        ),
        encoding="utf-8-sig",
    )


def zip_dir(source: Path, destination: Path) -> None:
    if destination.exists():
        destination.unlink()
    with zipfile.ZipFile(destination, "w", zipfile.ZIP_DEFLATED) as zf:
        for file in sorted(source.rglob("*")):
            if file.is_file():
                zf.write(file, file.relative_to(source))


def publish(runtime: str, output: Path, version: str, self_contained: bool) -> None:
    if output.exists():
        shutil.rmtree(output)
    asm = assembly_version(version)
    run(
        [
            "dotnet",
            "publish",
            str(PROJECT),
            "-c",
            "Release",
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
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    version = args.version.lstrip("v")
    update_version_files(version, args.repo, args.tag)

    win32_publish = PUBLISH / "win32"
    win64_publish = PUBLISH / "win64"
    win32_zip = ARTIFACTS / "DataEditorX_win32.zip"
    win64_zip = ARTIFACTS / "DataEditorX_win64.zip"

    ARTIFACTS.mkdir(exist_ok=True)
    if not args.skip_build:
        publish("win-x86", win32_publish, version, args.self_contained)
        publish("win-x64", win64_publish, version, args.self_contained)
        zip_dir(win32_publish, win32_zip)
        zip_dir(win64_publish, win64_zip)

    print(f"Created: {win32_zip}")
    print(f"Created: {win64_zip}")

    if args.upload:
        upload(args.repo, args.tag, version, [win32_zip, win64_zip])

    return 0


if __name__ == "__main__":
    sys.exit(main())
