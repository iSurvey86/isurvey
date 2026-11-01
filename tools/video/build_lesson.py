"""Ghép một bài học: giọng máy, thẻ chữ, và các đoạn quay Civil."""

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
import time
from pathlib import Path
from typing import Optional

from PIL import Image, ImageDraw, ImageFont

REPO = Path(__file__).resolve().parents[2]
WIDTH = 1920
HEIGHT = 1080
BG = (28, 36, 48)
ACCENT = (61, 143, 110)
FG = (255, 255, 255)
MUTED = (213, 220, 227)


def tool_in_winget(name: str) -> Optional[str]:
    local = os.environ.get("LOCALAPPDATA")
    if not local:
        return None
    root = Path(local) / "Microsoft" / "WinGet" / "Packages"
    if not root.exists():
        return None
    hits = sorted(root.glob(f"Gyan.FFmpeg*/**/bin/{name}.exe"))
    return str(hits[-1]) if hits else None


def ffmpeg() -> str:
    found = shutil.which("ffmpeg") or tool_in_winget("ffmpeg")
    if not found:
        sys.exit("Chưa có ffmpeg. Chạy tools/video/setup.ps1.")
    return found


def ffprobe() -> str:
    found = shutil.which("ffprobe") or tool_in_winget("ffprobe")
    if not found:
        sys.exit("Chưa có ffprobe. Chạy tools/video/setup.ps1.")
    return found


def load_lesson(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def work_dir(lesson: dict) -> Path:
    folder = REPO / "docs" / "video" / "thanh-pham" / lesson["id"]
    for name in ("audio", "cards", "parts"):
        (folder / name).mkdir(parents=True, exist_ok=True)
    (folder / "raw").mkdir(parents=True, exist_ok=True)
    return folder


def duration_of(path: Path) -> float:
    out = subprocess.check_output(
        [
            ffprobe(),
            "-v",
            "error",
            "-show_entries",
            "format=duration",
            "-of",
            "csv=p=0",
            str(path),
        ],
        text=True,
    ).strip()
    return float(out)


def narrate(lesson: dict, folder: Path) -> None:
    voice = lesson.get("voice", "vi-VN-HoaiMyNeural")
    rate = lesson.get("rate", "-8%")
    for shot in lesson["shots"]:
        text = (shot.get("narration") or "").strip()
        if not text:
            sys.exit(f"Cảnh {shot['id']} chưa có câu thoại.")
        dest = folder / "audio" / f"{shot['id']}.mp3"
        if dest.exists() and dest.stat().st_size > 1000:
            print(f"co san {dest.name} {duration_of(dest):.1f}s", flush=True)
            continue
        last_error = None
        for attempt in range(3):
            try:
                subprocess.check_call(
                    [
                        sys.executable,
                        "-m",
                        "edge_tts",
                        "--voice",
                        voice,
                        f"--rate={rate}",
                        "--text",
                        text,
                        "--write-media",
                        str(dest),
                    ]
                )
                last_error = None
                break
            except subprocess.CalledProcessError as exc:
                last_error = exc
                time.sleep(2)
        if last_error is not None:
            raise last_error
        print(f"tieng {dest.name} {duration_of(dest):.1f}s", flush=True)


def font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    name = "segoeuib.ttf" if bold else "segoeui.ttf"
    path = Path(r"C:\Windows\Fonts") / name
    if not path.exists():
        sys.exit(f"Không thấy font {path}")
    return ImageFont.truetype(str(path), size)


def render_card(lines: list[str], dest: Path) -> None:
    image = Image.new("RGB", (WIDTH, HEIGHT), BG)
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 0, 14, HEIGHT), fill=ACCENT)
    sizes = [78] + [46] * (len(lines) - 1)
    faces = [font(sizes[i], bold=(i == 0)) for i in range(len(lines))]
    gap = 28
    heights = []
    for i, line in enumerate(lines):
        box = draw.textbbox((0, 0), line, font=faces[i])
        heights.append(box[3] - box[1])
    total = sum(heights) + gap * (len(lines) - 1)
    y = (HEIGHT - total) / 2
    for i, line in enumerate(lines):
        box = draw.textbbox((0, 0), line, font=faces[i])
        width = box[2] - box[0]
        color = FG if i == 0 else MUTED
        draw.text(((WIDTH - width) / 2, y), line, font=faces[i], fill=color)
        y += heights[i] + gap
    image.save(dest)


def encode(cmd: list[str]) -> None:
    subprocess.check_call(cmd)


def fit_screen(video: Path, audio: Path, dest: Path) -> None:
    video_s = duration_of(video)
    audio_s = duration_of(audio)
    length = max(video_s, audio_s)
    extra = max(0.0, length - video_s)
    encode(
        [
            ffmpeg(),
            "-y",
            "-i",
            str(video),
            "-i",
            str(audio),
            "-filter_complex",
            (
                "[0:v]fps=30,scale=1920:1080:force_original_aspect_ratio=decrease,"
                "pad=1920:1080:(ow-iw)/2:(oh-ih)/2:color=0x1C2430,setsar=1,"
                f"tpad=stop_mode=clone:stop_duration={extra:.3f}[v];"
                "[1:a]aformat=sample_rates=48000:channel_layouts=stereo,apad[a]"
            ),
            "-map",
            "[v]",
            "-map",
            "[a]",
            "-t",
            f"{length:.3f}",
            "-c:v",
            "libx264",
            "-preset",
            "veryfast",
            "-crf",
            "20",
            "-pix_fmt",
            "yuv420p",
            "-c:a",
            "aac",
            "-b:a",
            "160k",
            "-ar",
            "48000",
            "-ac",
            "2",
            str(dest),
        ]
    )


def fit_card(card: Path, audio: Path, dest: Path) -> None:
    length = duration_of(audio) + 0.45
    encode(
        [
            ffmpeg(),
            "-y",
            "-loop",
            "1",
            "-framerate",
            "30",
            "-i",
            str(card),
            "-i",
            str(audio),
            "-filter_complex",
            "[1:a]aformat=sample_rates=48000:channel_layouts=stereo,apad[a]",
            "-map",
            "0:v",
            "-map",
            "[a]",
            "-t",
            f"{length:.3f}",
            "-c:v",
            "libx264",
            "-preset",
            "veryfast",
            "-crf",
            "20",
            "-pix_fmt",
            "yuv420p",
            "-c:a",
            "aac",
            "-b:a",
            "160k",
            "-ar",
            "48000",
            "-ac",
            "2",
            "-r",
            "30",
            str(dest),
        ]
    )


def mux(lesson: dict, folder: Path) -> Path:
    missing = []
    for shot in lesson["shots"]:
        if shot["type"] == "screen":
            raw = folder / "raw" / f"{shot['id']}.mp4"
            if not raw.exists():
                missing.append(str(raw))
    if missing:
        print("Thiếu đoạn quay:", file=sys.stderr)
        for item in missing:
            print(item, file=sys.stderr)
        sys.exit(2)

    parts = []
    for shot in lesson["shots"]:
        audio = folder / "audio" / f"{shot['id']}.mp3"
        if not audio.exists():
            sys.exit(f"Chưa có tiếng {audio}. Chạy narrate trước.")
        dest = folder / "parts" / f"{shot['id']}.mp4"
        if shot["type"] == "card":
            card = folder / "cards" / f"{shot['id']}.png"
            render_card(shot.get("lines") or [lesson["title"]], card)
            fit_card(card, audio, dest)
        elif shot["type"] == "screen":
            fit_screen(folder / "raw" / f"{shot['id']}.mp4", audio, dest)
        else:
            sys.exit(f"Cảnh {shot['id']} có type lạ: {shot['type']}")
        parts.append(dest)
        print(f"canh {shot['id']} {duration_of(dest):.1f}s", flush=True)

    listing = folder / "parts" / "concat.txt"
    listing.write_text(
        "".join(f"file '{part.name}'\n" for part in parts),
        encoding="utf-8",
    )
    final = folder / f"{lesson['id']}.mp4"
    encode(
        [
            ffmpeg(),
            "-y",
            "-f",
            "concat",
            "-safe",
            "0",
            "-i",
            str(listing),
            "-c:v",
            "libx264",
            "-preset",
            "veryfast",
            "-crf",
            "20",
            "-pix_fmt",
            "yuv420p",
            "-c:a",
            "aac",
            "-b:a",
            "160k",
            "-ar",
            "48000",
            "-ac",
            "2",
            "-movflags",
            "+faststart",
            str(final),
        ]
    )
    print(f"xong {final} {duration_of(final):.1f}s", flush=True)
    return final


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("step", choices=("narrate", "mux", "all"))
    parser.add_argument("--lesson", required=True)
    args = parser.parse_args()
    lesson = load_lesson(Path(args.lesson))
    folder = work_dir(lesson)
    if args.step in ("narrate", "all"):
        narrate(lesson, folder)
    if args.step in ("mux", "all"):
        mux(lesson, folder)


if __name__ == "__main__":
    main()
